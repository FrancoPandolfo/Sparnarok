import { create } from 'zustand';
import { useAuthStore } from '../auth/useAuthStore';
import type { PartyMember } from '../quests/types';

interface PartyMembersState {
  members: PartyMember[];
  isLoading: boolean;
  error: string | null;
  fetchMembers: (partyIdParam?: string) => Promise<void>;
  setMembers: (members: PartyMember[]) => void;
}

export const usePartyMembersStore = create<PartyMembersState>((set, get) => ({
  members: [],
  isLoading: false,
  error: null,

  setMembers: (members: PartyMember[]) => set({ members }),

  fetchMembers: async (partyIdParam?: string) => {
    const authState = useAuthStore.getState();
    const partyId = partyIdParam || authState.partyId;
    const token = authState.token;

    if (!partyId || partyId === 'party1') {
      if (get().members.length === 0) {
        set({
          members: [
            { id: authState.userId || 'hero-current', username: authState.username || 'Héroe', role: 'Leader' }
          ]
        });
      }
      return;
    }

    set({ isLoading: true, error: null });
    try {
      const headers: Record<string, string> = {};
      if (token) headers['Authorization'] = `Bearer ${token}`;
      if (partyId) headers['X-Party-Id'] = partyId;

      const response = await fetch(`/api/parties/${partyId}/members`, { headers });
      if (response.ok) {
        const data = await response.json();
        if (Array.isArray(data)) {
          set({ members: data, isLoading: false });
          return;
        }
      }
      set({ isLoading: false });
    } catch (err: any) {
      set({ 
        error: err?.message || 'Error al obtener miembros de la party', 
        isLoading: false 
      });
    }
  }
}));

export const usePartyMembers = () => {
  const { members, isLoading, error, fetchMembers } = usePartyMembersStore();
  return { members, isLoading, error, fetchMembers };
};
