import { describe, it, expect, beforeEach, vi } from 'vitest';
import { usePartyMembersStore } from './usePartyMembers';
import { useAuthStore } from '../auth/useAuthStore';

describe('usePartyMembersStore', () => {
  beforeEach(() => {
    usePartyMembersStore.setState({ members: [], isLoading: false, error: null });
    vi.clearAllMocks();
  });

  it('fetches and sets members from /api/parties/{partyId}/members', async () => {
    useAuthStore.setState({ partyId: 'party-100', token: 'auth-token' });

    const mockMembers = [
      { id: 'u1', username: 'Developer', role: 'Member' },
      { id: 'u2', username: 'QA', role: 'Tester' }
    ];

    globalThis.fetch = vi.fn().mockResolvedValue({
      ok: true,
      json: async () => mockMembers
    });

    await usePartyMembersStore.getState().fetchMembers('party-100');

    expect(usePartyMembersStore.getState().members).toEqual(mockMembers);
    expect(globalThis.fetch).toHaveBeenCalledWith(
      '/api/parties/party-100/members',
      expect.objectContaining({
        headers: expect.objectContaining({
          Authorization: 'Bearer auth-token',
          'X-Party-Id': 'party-100'
        })
      })
    );
  });
});
