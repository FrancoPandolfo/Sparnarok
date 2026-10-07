import { describe, it, expect, beforeEach, vi } from 'vitest';
import { useQuestStore } from './useQuestStore';
import { QuestState } from './types';

describe('useQuestStore Optimistic Updates', () => {
  beforeEach(() => {
    useQuestStore.setState({ userXp: 0 });
    vi.clearAllMocks();
  });

  it('optimistically updates the state and adds XP immediately before API response', async () => {
    global.fetch = vi.fn().mockImplementation(() => 
      new Promise(resolve => setTimeout(() => resolve({ ok: true }), 100))
    );

    const store = useQuestStore.getState();
    const questId = '1';

    const updatePromise = store.updateQuestState(questId, QuestState.Completed, 'user123');

    const immediateState = useQuestStore.getState();
    const updatedQuest = immediateState.quests.find(q => q.id === questId);
    
    expect(updatedQuest?.state).toBe(QuestState.Completed);
    expect(immediateState.userXp).toBe(50);

    await updatePromise;
  });
});
