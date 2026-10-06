import { motion } from 'framer-motion';
import { X, Crown } from 'lucide-react';
import { useTranslation } from 'react-i18next';

export const PaywallModal = ({ onClose }: { onClose: () => void }) => {
  const { t } = useTranslation();

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 backdrop-blur-sm">
      <motion.div
        initial={{ opacity: 0, scale: 0.9, y: 20 }}
        animate={{ opacity: 1, scale: 1, y: 0 }}
        exit={{ opacity: 0, scale: 0.9, y: 20 }}
        className="bg-gray-900 border border-gray-700 rounded-xl p-6 max-w-md w-full shadow-[0_0_40px_rgba(245,158,11,0.2)] relative"
      >
        <button onClick={onClose} className="absolute top-4 right-4 text-gray-400 hover:text-white transition-colors">
          <X size={20} />
        </button>
        
        <div className="flex flex-col items-center text-center space-y-4 mt-2">
          <div className="w-16 h-16 bg-amber-500/20 rounded-full flex items-center justify-center">
            <Crown size={32} className="text-amber-400" />
          </div>
          <h2 className="text-2xl font-bold text-white">{t('paywall.title', 'Ascensión Pro Requerida')}</h2>
          <p className="text-gray-400">
            {t('paywall.description', 'Esta rama de habilidades pertenece a la capa de Arquitectura Avanzada. Solicita a tu Workspace Manager un upgrade al plan Pro para desbloquear el poder absoluto.')}
          </p>
          <button className="w-full py-3 mt-2 bg-amber-500 hover:bg-amber-600 text-gray-900 font-bold rounded-lg transition-colors shadow-[0_0_15px_rgba(245,158,11,0.5)]">
            {t('paywall.upgradeButton', 'Actualizar a Pro')}
          </button>
        </div>
      </motion.div>
    </div>
  );
};
