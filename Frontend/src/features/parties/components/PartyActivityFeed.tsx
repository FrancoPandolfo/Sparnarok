import { useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import { motion, AnimatePresence } from 'framer-motion';
import { Zap, ArrowUpCircle } from 'lucide-react';
import { useFeedStore, type FeedItem } from '../useFeedStore';

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
      const user = payload?.user || payload?.User || (message ? message.split(' ')[0] : 'Aliado');

      let actionText = message;
      if (actionText.startsWith(`${user} `)) {
        actionText = actionText.substring(user.length + 1);
      }
      if (xp > 0 && !actionText.includes(`+${xp} XP`)) {
        actionText = `${actionText} (+${xp} XP)`;
      }

      const newItem: FeedItem = {
        id,
        type: 'quest_completed',
        user,
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
