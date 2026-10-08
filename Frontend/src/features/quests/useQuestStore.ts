import { create } from 'zustand';
import { QuestState, QuestDifficulty } from './types';
import type { Quest, CreateQuestPayload } from './types';

interface QuestStoreState {
  quests: Quest[];
  userXp: number;
  isLoading: boolean;
  error: string | null;
  tagMultipliers: Record<string, number>;
  
  createQuest: (payload: CreateQuestPayload) => Promise<void>;
  updateQuestState: (id: string, newState: QuestState, userId: string) => Promise<void>;
  updateTagMultipliers: (multipliers: Record<string, number>) => Promise<void>;
  clearError: () => void;
}

const initialMockQuests: Quest[] = [
  {
    id: '1',
    title: 'Derrotar al Bug del Login',
    description: 'El login devuelve 500 a veces.',
    difficulty: QuestDifficulty.D,
    state: QuestState.Pending,
    tags: ['Urgente', 'Bug'],
    rewards: [{ skillCategoryId: 'frontend', xpAmount: 50 }],
    createdAt: new Date().toISOString()
  },
  {
    id: '2',
    title: 'Refactorizar Auth a Middleware',
    description: 'Mejorar seguridad.',
    difficulty: QuestDifficulty.B,
    state: QuestState.InProgress,
    rewards: [{ skillCategoryId: 'backend', xpAmount: 120 }],
    createdAt: new Date().toISOString()
  }
];

export const useQuestStore = create<QuestStoreState>((set, get) => ({
  quests: initialMockQuests,
  userXp: 0,
  isLoading: false,
  error: null,
  tagMultipliers: { 'Urgente': 2.0 },

  clearError: () => set({ error: null }),

  updateTagMultipliers: async (multipliers: Record<string, number>) => {
    set({ tagMultipliers: multipliers });
    // Aquí iría el fetch PUT /api/party/{partyId}/settings/multipliers
  },

  updateQuestState: async (id: string, newState: QuestState, userId: string) => {
    const previousQuests = get().quests;
    // const previousXp = get().userXp;
    const quest = previousQuests.find(q => q.id === id);
    
    let xpGained = 0;
    if (newState === QuestState.Completed && quest && quest.state !== QuestState.Completed) {
      let baseXP = quest.rewards?.reduce((acc, r) => acc + r.xpAmount, 0) || 0;
      let multiplier = 1;
      quest.tags?.forEach(tag => {
        if (get().tagMultipliers[tag]) {
          multiplier *= get().tagMultipliers[tag];
        }
      });
      xpGained = Math.round(baseXP * multiplier);
    }

    set(state => ({
      quests: state.quests.map(q => q.id === id ? { ...q, state: newState } : q),
      userXp: state.userXp + xpGained
    }));
    
    try {
      const response = await fetch(`/api/quests/${id}/status?userId=${userId}`, {
        method: 'PATCH',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ state: newState })
      });
      if (!response.ok) throw new Error('API Error');
    } catch {
      // TEMP: Comentamos el rollback para que puedas jugar con la UI visualmente
      // sin necesidad de tener PostgreSQL ni la API corriendo.
      // set({ quests: previousQuests, userXp: previousXp });
    }
  },

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
