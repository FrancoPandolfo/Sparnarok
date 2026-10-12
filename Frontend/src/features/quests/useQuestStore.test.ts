import { describe, it, expect, beforeEach, vi } from 'vitest';
import { useQuestStore } from './useQuestStore';
import { useAuthStore } from '../auth/useAuthStore';
import { QuestState } from './types';

describe('useQuestStore Optimistic Updates', () => {
  beforeEach(() => {
    useQuestStore.setState({ userXp: 0 });
    vi.clearAllMocks();
  });

  it('optimistically updates the state and adds XP immediately before API response', async () => {
    globalThis.fetch = vi.fn().mockImplementation(() => 
      new Promise(resolve => setTimeout(() => resolve({ ok: true }), 100))
    );

    const store = useQuestStore.getState();
    const questId = '1';

    const updatePromise = store.updateQuestState(questId, QuestState.Completed, 'user123');

    const immediateState = useQuestStore.getState();
    const updatedQuest = immediateState.quests.find(q => q.id === questId);
    
    expect(updatedQuest?.state).toBe(QuestState.Completed);
    expect(immediateState.userXp).toBe(100);

    await updatePromise;
  });

  it('fetchQuests loads persisted quests from API', async () => {
    useAuthStore.setState({ partyId: 'valid-party-uuid', token: 'valid-token' });

    const mockDbQuests = [
      { id: 'guid-123', title: 'Persisted Quest', description: 'Persisted Description', state: QuestState.Completed, rewards: [], difficulty: 0, createdAt: '' }
    ];
    globalThis.fetch = vi.fn().mockResolvedValue({
      ok: true,
      json: async () => mockDbQuests
    });

    await useQuestStore.getState().fetchQuests();

    expect(useQuestStore.getState().quests).toEqual(mockDbQuests);
  });

  it('setQuestStateLocal updates quest state case-insensitively for SignalR events', () => {
    useQuestStore.setState({
      quests: [
        { id: 'A1B2-C3D4', title: 'Test Quest', description: 'Test Description', state: QuestState.Pending, rewards: [], difficulty: 0, createdAt: '' }
      ]
    });

    useQuestStore.getState().setQuestStateLocal('a1b2-c3d4', QuestState.Completed);

    const updated = useQuestStore.getState().quests.find(q => q.id === 'A1B2-C3D4');
    expect(updated?.state).toBe(QuestState.Completed);
  });

  it('optimistically rolls back XP and adds feed entry when moving out of Completed', async () => {
    globalThis.fetch = vi.fn().mockResolvedValue({ ok: true });

    useQuestStore.setState({
      userXp: 150,
      quests: [
        {
          id: 'quest-rollback-1',
          title: 'Rejected Task',
          description: 'Desc',
          state: QuestState.Completed,
          rewards: [{ skillCategoryId: 'cat-1', xpAmount: 100 }],
          difficulty: 0,
          createdAt: ''
        }
      ]
    });

    await useQuestStore.getState().updateQuestState('quest-rollback-1', QuestState.InProgress, 'user123', 'Tester');

    expect(useQuestStore.getState().userXp).toBe(50); // 150 - 100
    const feed = (await import('../parties/useFeedStore')).useFeedStore.getState().feed;
    const rollbackEntry = feed.find(f => f.type === 'quest_rollback');
    expect(rollbackEntry).toBeDefined();
    expect(rollbackEntry?.action).toContain("La Quest 'Rejected Task' ha retornado (-100 XP)");
  });
});
