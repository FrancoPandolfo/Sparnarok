import { create } from 'zustand';
import { persist } from 'zustand/middleware';

function parseJwt(token: string): any {
  try {
    const base64Url = token.split('.')[1];
    if (!base64Url) return null;
    const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
    const jsonPayload = decodeURIComponent(
      atob(base64)
        .split('')
        .map(c => '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2))
        .join('')
    );
    return JSON.parse(jsonPayload);
  } catch {
    return null;
  }
}

interface AuthState {
  token: string | null;
  username: string | null;
  userId: string | null;
  partyId: string | null;
  partyName: string | null;
  setAuth: (token: string, username?: string, partyId?: string, partyName?: string) => void;
  setParty: (partyId: string, partyName: string) => void;
  logout: () => void;
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set, get) => ({
      token: null,
      username: null,
      userId: null,
      partyId: null,
      partyName: null,
      setAuth: (token: string, username?: string, partyId?: string, partyName?: string) => {
        const decoded = parseJwt(token);
        const resolvedUserId = decoded?.sub || decoded?.nameid || null;
        const resolvedUsername = username || decoded?.['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'] || decoded?.unique_name || decoded?.username || 'Héroe';
        const resolvedPartyId = partyId || decoded?.partyId || get().partyId || null;
        const resolvedPartyName = partyName || (resolvedPartyId ? `Gremio (${resolvedPartyId.slice(0, 6)})` : 'Gremio Principal');

        set({
          token,
          username: resolvedUsername,
          userId: resolvedUserId,
          partyId: resolvedPartyId,
          partyName: resolvedPartyName
        });
      },
      setParty: (partyId: string, partyName: string) => set({ partyId, partyName }),
      logout: () => set({ token: null, username: null, userId: null, partyId: null, partyName: null })
    }),
    { name: 'sparnarok-auth' }
  )
);
