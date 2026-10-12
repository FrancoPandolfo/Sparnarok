import { useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import { motion, AnimatePresence } from 'framer-motion';
import { Zap, ArrowUpCircle } from 'lucide-react';
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

      const authState = useAuthStore.getState();
      const currentUserId = authState.userId;
      const currentUsername = authState.username;

      const isCurrentUser = Boolean(
        (payloadSourceUserId && currentUserId && payloadSourceUserId.toLowerCase() === currentUserId.toLowerCase()) ||
        (payloadUser && currentUsername && payloadUser.toLowerCase() === currentUsername.toLowerCase())
      );

      // If completed by current user, prevent duplicate activity item in their own panel
      if (isCurrentUser) {
        const existingFeed = useFeedStore.getState().feed;
        const alreadyExists = existingFeed.some(item => 
          (payloadQuestId && item.id.includes(payloadQuestId)) ||
          item.id === id
        );
        if (alreadyExists) {
          return;
        }

        const questTitle = message.includes("'") ? message.split("'")[1] : 'la tarea';
        addActivity({
          id,
          type: 'quest_completed',
          user: currentUsername || payloadUser || 'Tú',
          action: `ha completado la tarea '${questTitle}' (+${xp} XP)`,
          time: 'Ahora'
        });
        return;
      }

      // For ally (e.g. other party members):
      const heroName = payloadUser || (message && !message.startsWith('El aliado') && !message.startsWith('Un aliado') ? message.split(' ')[0] : 'un aliado');
      const allyUser = `El aliado ${heroName}`;

      let actionText = message;
      if (actionText.startsWith(`El aliado ${heroName} `)) {
        actionText = actionText.substring(`El aliado ${heroName} `.length);
      } else if (actionText.startsWith(`${heroName} `)) {
        actionText = actionText.substring(`${heroName} `.length);
      } else if (actionText.startsWith('Un aliado ')) {
        actionText = actionText.substring('Un aliado '.length);
      }

      if (xp > 0 && !actionText.includes(`+${xp} XP`)) {
        actionText = `${actionText} (+${xp} XP)`;
      }

      const newItem: FeedItem = {
        id,
        type: 'quest_completed',
        user: allyUser,
        action: actionText,
        time: 'Ahora'
      };

      addActivity(newItem);
    };

    connection.on('NewPartyActivity', onNewActivity);
    return () => { 
      connection.off('NewPartyActivity', onNewActivity); 
    };
  }, [connection, addActivity]);

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
                className="flex items-start gap-3 bg-gray-900 p-3 rounded-lg border border-gray-800"
              >
                <div className="mt-1">
                  {item.type === 'quest_completed' ? (
                    <Zap size={16} className="text-emerald-400" />
                  ) : (
                    <ArrowUpCircle size={16} className="text-blue-400" />
                  )}
                </div>
                <div className="flex-1">
                  <p className="text-sm text-gray-300">
                    <span className="font-bold text-white">{item.user}</span> {item.action}
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
