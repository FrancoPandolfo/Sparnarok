import { motion } from 'framer-motion';
import { Sparkles, Trophy, CheckCircle, Zap } from 'lucide-react';
import { useTranslation } from 'react-i18next';

interface UnlockedNode {
  skillCategoryId: string;
  categoryName: string;
  newLevel: number;
  currentXp: number;
}

interface Props {
  xpDistributed: number;
  unlockedNodes: UnlockedNode[];
  onClose: () => void;
}

export const MultiLevelUpModal = ({ xpDistributed, unlockedNodes, onClose }: Props) => {
  const { t } = useTranslation();

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/75 backdrop-blur-md p-4">
      {/* Background glow animation */}
      <div className="absolute w-96 h-96 bg-amber-500/20 rounded-full blur-3xl pointer-events-none animate-pulse" />

      <motion.div
        initial={{ opacity: 0, scale: 0.8, y: 30 }}
        animate={{ opacity: 1, scale: 1, y: 0 }}
        exit={{ opacity: 0, scale: 0.8, y: -20 }}
        transition={{ type: 'spring', damping: 20, stiffness: 300 }}
        className="bg-gray-900 border-2 border-amber-500/60 rounded-2xl p-4 sm:p-7 w-[calc(100%-2rem)] md:w-full max-w-lg max-h-[90vh] overflow-y-auto shadow-[0_0_60px_rgba(245,158,11,0.35)] relative"
      >
        {/* Particle sparkles in header */}
        <div className="flex flex-col items-center text-center space-y-3">
          <motion.div
            initial={{ rotate: -20, scale: 0 }}
            animate={{ rotate: 0, scale: 1 }}
            transition={{ delay: 0.1, type: 'spring' }}
            className="w-20 h-20 bg-gradient-to-tr from-amber-600 via-amber-400 to-yellow-200 rounded-full flex items-center justify-center shadow-lg shadow-amber-500/40 relative"
          >
            <Trophy size={42} className="text-gray-950" />
            <Sparkles size={20} className="text-white absolute -top-2 -right-2 animate-bounce" />
          </motion.div>

          <motion.h2 
            initial={{ opacity: 0, y: 10 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ delay: 0.2 }}
            className="text-3xl font-black text-transparent bg-clip-text bg-gradient-to-r from-amber-400 via-yellow-300 to-amber-500 tracking-wider uppercase"
          >
            {t('profile.levelUpCelebration', '¡LEVEL UP MÚLTIPLE!')}
          </motion.h2>

          <motion.p 
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            transition={{ delay: 0.3 }}
            className="text-gray-300 text-sm max-w-sm"
          >
            Has forjado tu conocimiento y distribuido exitosamente{' '}
            <span className="font-bold text-amber-400">+{xpDistributed} XP</span> acumulada en la constelación.
          </motion.p>
        </div>

        {/* List of leveled up nodes */}
        <div className="mt-6 space-y-2.5 max-h-60 overflow-y-auto pr-1">
          {unlockedNodes.map((node, i) => (
            <motion.div
              key={node.skillCategoryId || i}
              initial={{ opacity: 0, x: -25 }}
              animate={{ opacity: 1, x: 0 }}
              transition={{ delay: 0.3 + i * 0.1 }}
              className="flex items-center justify-between bg-gray-950/80 border border-gray-800 p-3 rounded-xl hover:border-amber-500/40 transition-colors"
            >
              <div className="flex items-center gap-3">
                <div className="p-1.5 bg-emerald-500/20 text-emerald-400 rounded-lg">
                  <CheckCircle size={16} />
                </div>
                <div>
                  <div className="text-sm font-bold text-white">{node.categoryName}</div>
                  <div className="text-[11px] text-gray-400 flex items-center gap-1">
                    <Zap size={11} className="text-amber-400" />
                    <span>{node.currentXp} XP invertida</span>
                  </div>
                </div>
              </div>
              <div className="bg-amber-500/10 border border-amber-500/30 text-amber-400 px-2.5 py-1 rounded-full text-xs font-black">
                NIVEL {node.newLevel}
              </div>
            </motion.div>
          ))}
        </div>

        {/* Close / Action button */}
        <div className="mt-7">
          <button
            onClick={onClose}
            className="w-full py-3 bg-gradient-to-r from-amber-500 to-amber-400 hover:from-amber-400 hover:to-amber-500 text-gray-950 font-black rounded-xl transition-all shadow-[0_0_20px_rgba(245,158,11,0.4)] text-sm tracking-wide uppercase"
          >
            Reclamar Ascensión
          </button>
        </div>
      </motion.div>
    </div>
  );
};
