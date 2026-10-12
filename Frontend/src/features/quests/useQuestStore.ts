import { create } from 'zustand';
import { QuestState, QuestDifficulty } from './types';
import type { Quest, CreateQuestPayload } from './types';
import { useAuthStore } from '../auth/useAuthStore';
import { useFeedStore } from '../parties/useFeedStore';

interface QuestStoreState {
  quests: Quest[];
  userXp: number;
  isLoading: boolean;
  error: string | null;
  tagMultipliers: Record<string, number>;
  
  fetchQuests: () => Promise<void>;
  createQuest: (payload: CreateQuestPayload) => Promise<void>;
  assignQuest: (id: string, assigneeId: string | null, assigneeName?: string | null) => Promise<void>;
  updateQuestState: (id: string, newState: QuestState, userId?: string, username?: string) => Promise<void>;
  setQuestStateLocal: (id: string, newState: QuestState) => void;
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

  fetchQuests: async () => {
    const token = useAuthStore.getState().token;
    const partyId = useAuthStore.getState().partyId;
    if (!partyId || partyId === 'party1') return;

    set({ isLoading: true, error: null });
    try {
      const headers: Record<string, string> = {};
      if (token) headers['Authorization'] = `Bearer ${token}`;
      if (partyId) headers['X-Party-Id'] = partyId;

      const response = await fetch(`/api/quests?partyId=${partyId}`, { headers });
      if (response.ok) {
        const data = await response.json();
        if (Array.isArray(data) && data.length > 0) {
          set({ quests: data, isLoading: false });
          return;
        }
      }
      set({ isLoading: false });
    } catch {
      set({ isLoading: false });
    }
  },

  updateTagMultipliers: async (multipliers: Record<string, number>) => {
    set({ tagMultipliers: multipliers });
  },

  setQuestStateLocal: (id: string, newState: QuestState) => {
    set(state => ({
      quests: state.quests.map(q => 
        (q.id.toLowerCase() === id.toLowerCase()) ? { ...q, state: newState } : q
      )
    }));
  },

  assignQuest: async (id: string, assigneeId: string | null, assigneeName?: string | null) => {
    set(state => ({
      quests: state.quests.map(q =>
        (q.id.toLowerCase() === id.toLowerCase() || q.id === id)
          ? { ...q, assigneeId, assigneeName: assigneeName ?? null }
          : q
      )
    }));

    try {
      const authState = useAuthStore.getState();
      const token = authState.token;
      const partyId = authState.partyId;
      const headers: Record<string, string> = { 'Content-Type': 'application/json' };
      if (token) headers['Authorization'] = `Bearer ${token}`;
      if (partyId) headers['X-Party-Id'] = partyId;

      const response = await fetch(`/api/quests/${id}/assign`, {
        method: 'PATCH',
        headers,
        body: JSON.stringify({ assigneeId })
      });

      if (!response.ok) throw new Error('Error al asignar la quest');
      const updatedQuest = await response.json();
      if (updatedQuest && updatedQuest.id) {
        set(state => ({
          quests: state.quests.map(q =>
            (q.id.toLowerCase() === id.toLowerCase() || q.id === id)
              ? { ...q, assigneeId: updatedQuest.assigneeId, assigneeName: updatedQuest.assigneeName }
              : q
          )
        }));
      }
    } catch {
      // Keep optimistic update for local test/offline
    }
  },

  updateQuestState: async (id: string, newState: QuestState, userId?: string, username?: string) => {
    const previousQuests = get().quests;
    // const previousXp = get().userXp;
    const quest = previousQuests.find(q => q.id.toLowerCase() === id.toLowerCase() || q.id === id);
    
    let xpChange = 0;
    const isMovingToCompleted = newState === QuestState.Completed && quest && quest.state !== QuestState.Completed;
    const isMovingFromCompleted = quest && quest.state === QuestState.Completed && newState !== QuestState.Completed;

    if (quest && (isMovingToCompleted || isMovingFromCompleted)) {
      let baseXP = quest.rewards?.reduce((acc, r) => acc + r.xpAmount, 0) || 0;
      let multiplier = 1;
      quest.tags?.forEach(tag => {
        if (get().tagMultipliers[tag]) {
          multiplier *= get().tagMultipliers[tag];
        }
      });
      const calculatedXp = Math.round(baseXP * multiplier);
      if (isMovingToCompleted) {
        xpChange = calculatedXp;
      } else if (isMovingFromCompleted) {
        xpChange = -calculatedXp;
      }
    }

    set(state => ({
      quests: state.quests.map(q => (q.id.toLowerCase() === id.toLowerCase() || q.id === id) ? { ...q, state: newState } : q),
      userXp: Math.max(0, state.userXp + xpChange)
    }));

    if (isMovingToCompleted && quest) {
      const heroName = quest.assigneeName || username || useAuthStore.getState().username || 'Tú';
      useFeedStore.getState().addActivity({
        id: `optimistic-${id}`,
        type: 'quest_completed',
        user: heroName,
        action: `¡${heroName} ha completado '${quest.title}'!`,
        time: 'Ahora'
      });
    }

    if (isMovingFromCompleted && quest) {
      const rollbackXp = Math.abs(xpChange);
      const heroName = quest.assigneeName || username || useAuthStore.getState().username || 'Tú';
      useFeedStore.getState().addActivity({
        id: `optimistic-rollback-${id}`,
        type: 'quest_rollback',
        user: heroName,
        action: `La Quest '${quest.title}' ha retornado (-${rollbackXp} XP)`,
        time: 'Ahora'
      });
    }
    
    try {
      const authState = useAuthStore.getState();
      const token = authState.token;
      const partyId = authState.partyId;
      const resolvedUserId = userId || authState.userId || '';
      const resolvedUsername = username || authState.username || '';
      const headers: Record<string, string> = { 'Content-Type': 'application/json' };
      if (token) headers['Authorization'] = `Bearer ${token}`;
      if (partyId) headers['X-Party-Id'] = partyId;

      const response = await fetch(`/api/quests/${id}/status?userId=${resolvedUserId}`, {
        method: 'PATCH',
        headers,
        body: JSON.stringify({ 
          state: newState,
          username: resolvedUsername,
          userId: resolvedUserId && resolvedUserId !== '11111111-1111-1111-1111-111111111111' ? resolvedUserId : undefined
        })
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
      const token = useAuthStore.getState().token;
      const partyId = useAuthStore.getState().partyId;
      const headers: Record<string, string> = { 'Content-Type': 'application/json' };
      if (token) headers['Authorization'] = `Bearer ${token}`;
      if (partyId) headers['X-Party-Id'] = partyId;

      const response = await fetch('/api/quests', {
        method: 'POST',
        headers,
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
