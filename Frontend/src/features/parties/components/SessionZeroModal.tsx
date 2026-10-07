import { useState } from 'react';
import { motion } from 'framer-motion';
import { X, SlidersHorizontal } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { SENIORITY_PRESETS } from '../../../constants/SeniorityMapper';

interface Props {
  userId: string;
  onClose: () => void;
  onSave: (xpMap: Record<string, number>) => void;
}

export const SessionZeroModal = ({ userId: _userId, onClose, onSave }: Props) => {
  const { t } = useTranslation();
  const [seniority, setSeniority] = useState<keyof typeof SENIORITY_PRESETS | ''>('');
  const [xpMap, setXpMap] = useState<Record<string, number>>({
    Backend: 0,
    Frontend: 0,
    Arquitectura: 0,
    DevOps: 0
  });

  const handleSeniorityChange = (e: React.ChangeEvent<HTMLSelectElement>) => {
    const val = e.target.value as keyof typeof SENIORITY_PRESETS;
    setSeniority(val);
    if (val && SENIORITY_PRESETS[val]) {
      setXpMap({ ...SENIORITY_PRESETS[val] });
    }
  };

  const handleXpChange = (category: string, value: string) => {
    const num = parseInt(value, 10);
    setXpMap(prev => ({ ...prev, [category]: isNaN(num) ? 0 : num }));
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/70 backdrop-blur-md">
      <motion.div
        initial={{ opacity: 0, scale: 0.9, y: 20 }}
        animate={{ opacity: 1, scale: 1, y: 0 }}
        exit={{ opacity: 0, scale: 0.9, y: 20 }}
        className="bg-bg-gray-900 border border-gray-700 rounded-xl p-6 max-w-md w-full shadow-[0_0_40px_rgba(245,158,11,0.15)] relative bg-gray-900"
      >
        <button onClick={onClose} className="absolute top-4 right-4 text-gray-400 hover:text-white transition-colors">
          <X size={20} />
        </button>
        
        <div className="flex flex-col space-y-6">
          <div className="flex items-center gap-3">
            <div className="w-12 h-12 bg-amber-500/20 rounded-full flex items-center justify-center">
              <SlidersHorizontal size={24} className="text-amber-400" />
            </div>
            <div>
              <h2 className="text-2xl font-bold text-white">{t('sessionZero.title', 'Sesión Cero')}</h2>
              <p className="text-gray-400 text-sm">{t('sessionZero.subtitle', 'Calibra la XP inicial del miembro')}</p>
            </div>
          </div>

          <div className="space-y-4">
            <div>
              <label className="block text-sm font-bold text-gray-300 mb-1">{t('sessionZero.seniorityLabel', 'Seniority Corporativo')}</label>
              <select 
                data-testid="seniority-select"
                value={seniority} 
                onChange={handleSeniorityChange}
                className="w-full bg-gray-950 border border-gray-700 text-white rounded-lg p-3 focus:ring-amber-500 focus:border-amber-500"
              >
                <option value="">{t('sessionZero.selectPlaceholder', 'Seleccionar Nivel...')}</option>
                <option value="Trainee">Trainee</option>
                <option value="Junior">Junior</option>
                <option value="SemiSenior">Semi-Senior</option>
                <option value="Senior">Senior</option>
                <option value="Staff">Staff</option>
              </select>
            </div>

            <div className="grid grid-cols-2 gap-4">
              {Object.keys(xpMap).map(category => (
                <div key={category}>
                  <label className="block text-xs font-bold text-gray-400 mb-1">{category} XP</label>
                  <input 
                    type="number"
                    data-testid={`xp-input-${category}`}
                    value={xpMap[category]}
                    onChange={(e) => handleXpChange(category, e.target.value)}
                    className="w-full bg-gray-950 border border-gray-700 text-amber-400 font-mono rounded-lg p-2 focus:ring-amber-500"
                  />
                </div>
              ))}
            </div>
          </div>

          <button 
            onClick={() => onSave(xpMap)}
            className="w-full py-3 bg-amber-500 hover:bg-amber-600 text-gray-900 font-bold rounded-lg transition-colors shadow-[0_0_15px_rgba(245,158,11,0.5)]"
          >
            {t('sessionZero.saveButton', 'Guardar Calibración')}
          </button>
        </div>
      </motion.div>
    </div>
  );
};
