import { create } from 'zustand';
import { Quest, CreateQuestPayload } from './types';

interface QuestStoreState {
  quests: Quest[];
  isLoading: boolean;
  error: string | null;
  
  createQuest: (payload: CreateQuestPayload) => Promise<void>;
  clearError: () => void;
}

export const useQuestStore = create<QuestStoreState>((set, get) => ({
  quests: [],
  isLoading: false,
  error: null,

  clearError: () => set({ error: null }),

  createQuest: async (payload: CreateQuestPayload) => {
    set({ isLoading: true, error: null });
    try {
      const response = await fetch('/api/quests', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload)
      });

      if (!response.ok) {
        throw new Error('El gremio ha rechazado tu solicitud. Revisa los datos de la Quest.');
      }

      const newQuest: Quest = await response.json();
      
      set((state) => ({
        quests: [...state.quests, newQuest],
        isLoading: false
      }));
    } catch (error: any) {
      set({ 
        error: error.message || 'Interferencia mágica inesperada al crear la Quest.',
        isLoading: false 
      });
    }
  }
}));
