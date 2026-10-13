import { useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import { motion, AnimatePresence } from 'framer-motion';
import { Zap, ArrowUpCircle, RotateCcw } from 'lucide-react';
import { useFeedStore, type FeedItem } from '../useFeedStore';
import { useAuthStore } from '../../auth/useAuthStore';

interface Props {
  connection?: any;
}

export const PartyActivityFeed = ({ connection }: Props) => {
  const { t } = useTranslation();
  const feed = useFeedStore(state => state.feed);
  const addActivity = useFeedStore(state => state.addActivity);

  useEffect(() => {
    if (!connection) return;

    const onNewActivity = (payload: any) => {
      const message = payload?.message || payload?.Message || 'completó una Quest';
      const xp = payload?.xp || payload?.Xp || 0;
      const id = payload?.id || payload?.Id || Date.now().toString();
      const payloadUser = payload?.username || payload?.Username;
      const payloadSourceUserId = payload?.sourceUserId || payload?.SourceUserId;
      const payloadQuestId = payload?.questId || payload?.QuestId;
      const isRollback = xp < 0;

      // Global check: if the feed already has an entry for this quest (optimistic or broadcast), drop duplicate
      const existingFeed = useFeedStore.getState().feed;
      const alreadyExists = existingFeed.some(item => 
        (payloadQuestId && (
          item.id === `optimistic-${payloadQuestId}` || 
          item.id === `optimistic-rollback-${payloadQuestId}` ||
          item.questId === payloadQuestId ||
          item.id.includes(payloadQuestId)
        )) ||
        (id && item.id === id)
      );
      if (alreadyExists) {
        return;
      }

      const authState = useAuthStore.getState();
      const currentUserId = authState.userId;
      const currentUsername = authState.username;

      const isCurrentUser = Boolean(
        (payloadSourceUserId && currentUserId && payloadSourceUserId.toLowerCase() === currentUserId.toLowerCase()) ||
        (payloadUser && currentUsername && payloadUser.toLowerCase() === currentUsername.toLowerCase())
      );

      // If completed or rolled back by current user
      if (isCurrentUser) {
        const questTitle = message.includes("'") ? message.split("'")[1] : 'la tarea';
        const heroName = currentUsername || payloadUser || 'Tú';
        addActivity({
          id,
          questId: payloadQuestId,
          type: isRollback ? 'quest_rollback' : 'quest_completed',
          user: heroName,
          action: isRollback
            ? `ha devuelto '${questTitle}' a desarrollo (${xp} XP)`
            : `¡${heroName} ha completado '${questTitle}'!`,
          time: 'Ahora'
        });
        return;
      }

      // For ally (e.g. other party members):
      const heroName = payloadUser || (message && !message.startsWith('El aliado') && !message.startsWith('Un aliado') ? message.split(' ')[0] : 'un aliado');
      const allyUser = isRollback ? heroName : `El aliado ${heroName}`;

      let actionText = message;
      if (actionText.startsWith(`El aliado ${heroName} `)) {
        actionText = actionText.substring(`El aliado ${heroName} `.length);
      } else if (actionText.startsWith(`${heroName} `)) {
        actionText = actionText.substring(`${heroName} `.length);
      } else if (actionText.startsWith('Un aliado ')) {
        actionText = actionText.substring('Un aliado '.length);
      }

      if (isRollback) {
        if (!actionText.includes(`${xp} XP`)) {
          actionText = `${actionText} (${xp} XP)`;
        }
      } else if (xp > 0 && !actionText.includes(`+${xp} XP`)) {
        actionText = `${actionText} (+${xp} XP)`;
      }

      const newItem: FeedItem = {
        id,
        questId: payloadQuestId,
        type: isRollback ? 'quest_rollback' : 'quest_completed',
        user: allyUser,
        action: actionText,
        time: 'Ahora'
      };

      addActivity(newItem);
    };

    if (typeof connection.off === 'function') {
      connection.off('NewPartyActivity');
    }
    connection.on('NewPartyActivity', onNewActivity);
    return () => { 
      if (typeof connection.off === 'function') {
        connection.off('NewPartyActivity', onNewActivity); 
      }
    };
  }, [connection]);

  return (
    <div className="bg-gray-950 border border-gray-800 rounded-xl p-4 flex flex-col h-full overflow-hidden">
      <h3 className="text-amber-500 font-bold mb-4 uppercase text-sm tracking-widest">{t('feed.title', 'Actividad de la Party')}</h3>
      <div className="flex-1 overflow-y-auto pr-2 space-y-3">
        <AnimatePresence>
          {!feed || feed.length === 0 ? (
            <p className="text-gray-500 text-sm text-center mt-4">Sin actividad reciente</p>
          ) : (
            feed.map(item => (
              <motion.div
                key={item.id}
                initial={{ opacity: 0, x: -20, height: 0 }}
                animate={{ opacity: 1, x: 0, height: 'auto' }}
                className={`flex items-start gap-3 p-3 rounded-lg border transition-colors ${
                  item.type === 'quest_rollback' 
                    ? 'bg-gray-900/40 border-gray-800/80' 
                    : 'bg-gray-900 border-gray-800'
                }`}
              >
                <div className="mt-1">
                  {item.type === 'quest_completed' ? (
                    <Zap size={16} className="text-emerald-400" />
                  ) : item.type === 'quest_rollback' ? (
                    <RotateCcw size={16} className="text-amber-500/80" />
                  ) : (
                    <ArrowUpCircle size={16} className="text-blue-400" />
                  )}
                </div>
                <div className="flex-1">
                  <p className="text-sm">
                    {item.user && !item.action.startsWith('¡') && (
                      <>
                        <span className={item.type === 'quest_rollback' ? 'font-medium text-amber-200/80' : 'font-bold text-white'}>
                          {item.user}
                        </span>{' '}
                      </>
                    )}
                    <span className={item.type === 'quest_rollback' ? 'text-gray-400' : 'text-gray-300'}>
                      {item.action}
                    </span>
                  </p>
                  <p className="text-xs text-gray-500 mt-1">{item.time}</p>
                </div>
              </motion.div>
            ))
          )}
        </AnimatePresence>
      </div>
    </div>
  );
};
