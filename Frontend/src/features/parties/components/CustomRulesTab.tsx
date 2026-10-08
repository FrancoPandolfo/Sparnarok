import { useState } from 'react';
import { motion } from 'framer-motion';
import { Lock, Plus, Trash2, Save } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { useQuestStore } from '../../quests/useQuestStore';

interface Props {
  isPremium: boolean;
}

export const CustomRulesTab = ({ isPremium }: Props) => {
  const { t } = useTranslation();
  const storeMultipliers = useQuestStore(state => state.tagMultipliers);
  const updateTagMultipliers = useQuestStore(state => state.updateTagMultipliers);
  
  const [rules, setRules] = useState(
    Object.keys(storeMultipliers).length > 0 
      ? Object.entries(storeMultipliers).map(([k, v]) => ({ tag: k, multiplier: v }))
      : [{ tag: 'Bug', multiplier: 1.5 }]
  );

  const handleSave = () => {
    const record: Record<string, number> = {};
    rules.forEach(r => {
      if (r.tag.trim() !== '') record[r.tag] = r.multiplier;
    });
    updateTagMultipliers(record);
  };

  return (
    <div className="relative p-6 bg-gray-950 border border-gray-800 rounded-xl overflow-hidden mt-6">
      <div className={`transition-opacity duration-300 ${!isPremium ? 'opacity-50 pointer-events-none' : ''}`}>
        <h3 className="text-2xl font-bold text-amber-500 mb-4">{t('rules.title', 'Reglas Custom (Multiplicadores de XP)')}</h3>
        
        <div className="space-y-4 mb-6">
          {rules.map((rule, idx) => (
            <div key={idx} className="flex gap-4 items-center">
              <input 
                type="text" 
                disabled={!isPremium}
                value={rule.tag} 
                onChange={(e) => {
                  const newRules = [...rules];
                  newRules[idx].tag = e.target.value;
                  setRules(newRules);
                }}
                className="bg-gray-900 border border-gray-700 text-white rounded p-2 flex-1"
                placeholder={t('rules.tagPlaceholder', 'Ej: Bug Crítico')} 
              />
              <input 
                type="number" 
                disabled={!isPremium}
                step="0.1"
                value={rule.multiplier}
                onChange={(e) => {
                  const newRules = [...rules];
                  newRules[idx].multiplier = parseFloat(e.target.value) || 1;
                  setRules(newRules);
                }}
                className="bg-gray-900 border border-gray-700 text-amber-400 font-mono rounded p-2 w-24"
              />
              <button 
                disabled={!isPremium} 
                onClick={() => setRules(rules.filter((_, i) => i !== idx))}
                className="text-red-400 p-2 hover:bg-gray-800 rounded"
              >
                <Trash2 size={18} />
              </button>
            </div>
          ))}
        </div>

        <div className="flex gap-4">
          <button 
            disabled={!isPremium} 
            onClick={() => setRules([...rules, { tag: '', multiplier: 1 }])}
            className="flex items-center gap-2 text-indigo-400 font-bold px-4 py-2 hover:bg-gray-900 rounded"
          >
            <Plus size={16} /> {t('rules.addRule', 'Añadir Regla')}
          </button>
          <button 
            disabled={!isPremium} 
            onClick={handleSave}
            className="flex items-center gap-2 bg-amber-500 text-gray-900 font-bold px-4 py-2 hover:bg-amber-600 rounded ml-auto"
          >
            <Save size={16} /> {t('rules.saveRules', 'Guardar Reglas')}
          </button>
        </div>
      </div>

      {!isPremium && (
        <div className="absolute inset-0 z-50 flex items-center justify-center bg-background/80 backdrop-blur-sm">
          <motion.div 
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            className="bg-gray-900 border border-amber-500/30 p-8 rounded-2xl shadow-2xl max-w-lg text-center"
          >
            <div className="w-16 h-16 bg-amber-500/20 rounded-full flex items-center justify-center mx-auto mb-4">
              <Lock className="text-amber-500" size={32} />
            </div>
            <h2 className="text-2xl font-bold text-white mb-2">{t('paywall.title', 'Desbloquea las Reglas Custom')}</h2>
            <p className="text-gray-400 mb-6">{t('paywall.subtitle', 'Potencia el progreso de tu equipo personalizando cómo se otorga la experiencia. Exclusivo para planes Pro.')}</p>
            <button className="w-full py-3 bg-gradient-to-r from-amber-500 to-orange-500 text-gray-900 font-bold rounded-lg shadow-lg hover:shadow-amber-500/25 transition-all">
              {t('paywall.cta', 'Desbloquea Sparnarok Pro')}
            </button>
          </motion.div>
        </div>
      )}
    </div>
  );
};
