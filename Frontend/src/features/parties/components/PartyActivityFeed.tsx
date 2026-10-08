import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { motion, AnimatePresence } from 'framer-motion';
import { Zap, ArrowUpCircle } from 'lucide-react';

interface FeedItem {
  id: string;
  type: 'quest_completed' | 'level_up';
  user: string;
  action: string;
  time: string;
}

interface Props {
  connection?: any;
}

export const PartyActivityFeed = ({ connection }: Props) => {
  const { t } = useTranslation();
  const [feed, setFeed] = useState<FeedItem[]>([
    { id: '1', type: 'quest_completed', user: 'Mery', action: t('feed.questAction', "completó 'Migrar DB' (+100 XP)"), time: 'Hace 5m' },
    { id: '2', type: 'level_up', user: 'Alex', action: t('feed.levelAction', 'subió a Rango 3 en Backend'), time: 'Hace 12m' }
  ]);

  useEffect(() => {
    if (!connection) return;

    const onNewActivity = (payload: any) => {
      setFeed(prev => [
        { 
          id: payload.id || Date.now().toString(), 
          type: 'quest_completed', 
          user: 'Party Member', 
          action: payload.message || 'completó una Quest', 
          time: 'Ahora' 
        },
        ...prev.slice(0, 4)
      ]);
    };

    connection.on('NewPartyActivity', onNewActivity);
    return () => { connection.off('NewPartyActivity', onNewActivity); };
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
