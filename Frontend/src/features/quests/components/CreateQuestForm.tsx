import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuestStore } from '../useQuestStore';
import { QuestDifficulty } from '../types';
import { Sparkles, CheckCircle2, AlertCircle, X } from 'lucide-react';

interface Props {
  onClose?: () => void;
  defaultStatus?: number;
}

export const CreateQuestForm = ({ onClose }: Props) => {
  const { t } = useTranslation();
  const createQuest = useQuestStore(state => state.createQuest);
  const isLoading = useQuestStore(state => state.isLoading);
  const storeError = useQuestStore(state => state.error);

  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');
  const [difficulty, setDifficulty] = useState<QuestDifficulty>(QuestDifficulty.D);
  const [xpReward, setXpReward] = useState(100);
  const [success, setSuccess] = useState(false);
  const [localError, setLocalError] = useState('');

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLocalError('');

    if (!title.trim()) {
      setLocalError('El nombre de la Quest es requerido.');
      return;
    }

    try {
      await createQuest({
        title: title.trim(),
        description: description.trim(),
        difficulty,
        rewards: [{ skillCategoryId: 'frontend', xpAmount: Number(xpReward) || 100 }]
      });

      setSuccess(true);
      if (onClose) {
        setTimeout(() => onClose(), 1200);
      }
    } catch (err: any) {
      setLocalError(err.message || t('quest.create.errorMsg', 'Fallo al forjar la quest.'));
    }
  };

  return (
    <div className="bg-gray-900 border border-gray-800 rounded-xl p-4 sm:p-6 w-[calc(100%-2rem)] md:w-full max-w-lg max-h-[90vh] overflow-y-auto shadow-2xl relative">
      {onClose && (
        <button 
          onClick={onClose} 
          className="absolute top-4 right-4 text-gray-400 hover:text-white transition-colors"
          aria-label="Cerrar modal"
        >
          <X size={20} />
        </button>
      )}

      <div className="flex items-center gap-2 mb-4">
        <Sparkles className="text-amber-400" size={22} />
        <h2 className="text-xl font-bold text-amber-500 tracking-wide">
          {t('quest.create.title', 'Forjar una Nueva Quest')}
        </h2>
      </div>

      {success && (
        <div className="bg-emerald-500/10 border border-emerald-500 text-emerald-400 p-3 rounded-lg mb-4 text-sm font-semibold flex items-center gap-2">
          <CheckCircle2 size={18} />
          <span>{t('quest.create.successMsg', '¡Quest forjada con éxito!')}</span>
        </div>
      )}

      {(localError || storeError) && (
        <div className="bg-red-500/10 border border-red-500 text-red-400 p-3 rounded-lg mb-4 text-sm flex items-center gap-2">
          <AlertCircle size={18} />
          <span>{localError || storeError}</span>
        </div>
      )}

      <form onSubmit={handleSubmit} aria-label="form-create-quest" className="space-y-4">
        <div>
          <label htmlFor="questName" className="block text-gray-300 text-sm font-bold mb-1">
            {t('quest.create.nameLabel', 'Nombre de la Quest')}
          </label>
          <input
            id="questName"
            type="text"
            required
            value={title}
            onChange={e => setTitle(e.target.value)}
            placeholder="Ej: Derrotar al Bug del Login"
            className="w-full bg-gray-950 border border-gray-700 text-white rounded-lg p-2.5 text-sm focus:outline-none focus:border-amber-500 transition-colors"
          />
        </div>

        <div>
          <label htmlFor="questDesc" className="block text-gray-300 text-sm font-bold mb-1">
            {t('quest.create.descLabel', 'Descripción')}
          </label>
          <textarea
            id="questDesc"
            rows={3}
            value={description}
            onChange={e => setDescription(e.target.value)}
            placeholder="Describe la misión y las condiciones de victoria..."
            className="w-full bg-gray-950 border border-gray-700 text-white rounded-lg p-2.5 text-sm focus:outline-none focus:border-amber-500 transition-colors"
          />
        </div>

        <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
          <div>
            <label className="block text-gray-300 text-sm font-bold mb-1">
              Dificultad
            </label>
            <select
              value={difficulty}
              onChange={e => setDifficulty(Number(e.target.value) as QuestDifficulty)}
              className="w-full bg-gray-950 border border-gray-700 text-gray-200 text-sm rounded-lg p-2.5 focus:outline-none focus:border-amber-500"
            >
              <option value={QuestDifficulty.F}>Rango F (Trivial)</option>
              <option value={QuestDifficulty.E}>Rango E (Fácil)</option>
              <option value={QuestDifficulty.D}>Rango D (Normal)</option>
              <option value={QuestDifficulty.C}>Rango C (Moderado)</option>
              <option value={QuestDifficulty.B}>Rango B (Difícil)</option>
              <option value={QuestDifficulty.A}>Rango A (Épico)</option>
              <option value={QuestDifficulty.S}>Rango S (Legendario)</option>
            </select>
          </div>

          <div>
            <label className="block text-gray-300 text-sm font-bold mb-1">
              Recompensa XP
            </label>
            <input
              type="number"
              min={10}
              step={10}
              value={xpReward}
              onChange={e => setXpReward(Number(e.target.value))}
              className="w-full bg-gray-950 border border-gray-700 text-white text-sm rounded-lg p-2.5 focus:outline-none focus:border-amber-500"
            />
          </div>
        </div>

        <div className="pt-2 flex justify-end gap-3">
          {onClose && (
            <button
              type="button"
              onClick={onClose}
              className="px-4 py-2 bg-gray-800 hover:bg-gray-700 text-gray-300 text-sm font-bold rounded-lg transition-colors"
            >
              Cancelar
            </button>
          )}
          <button
            type="submit"
            disabled={isLoading}
            className="px-5 py-2 bg-amber-500 hover:bg-amber-600 disabled:bg-amber-600/50 text-gray-900 text-sm font-bold rounded-lg transition-colors shadow-lg shadow-amber-500/20"
          >
            {isLoading ? 'Forjando...' : t('quest.create.submitBtn', 'Crear Quest')}
          </button>
        </div>
      </form>
    </div>
  );
};
