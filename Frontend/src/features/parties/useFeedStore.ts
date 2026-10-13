import { create } from 'zustand';

export interface FeedItem {
  id: string;
  type: 'quest_completed' | 'level_up' | 'quest_rollback';
  user: string;
  action: string;
  time: string;
  questId?: string;
}

interface FeedState {
  feed: FeedItem[];
  addActivity: (item: FeedItem) => void;
  setFeed: (feed: FeedItem[]) => void;
}

export const useFeedStore = create<FeedState>((set) => ({
  feed: [
    { id: '1', type: 'quest_completed', user: 'Mery', action: "completó 'Migrar DB' (+100 XP)", time: 'Hace 5m' },
    { id: '2', type: 'level_up', user: 'Alex', action: 'subió a Rango 3 en Backend', time: 'Hace 12m' }
  ],
  addActivity: (item: FeedItem) => set((state) => {
    const isDuplicate = state.feed.some(existing => {
      if (existing.id === item.id) return true;
      if (item.questId) {
        const matchesExistingQuestId = existing.questId === item.questId;
        const matchesExistingId = existing.id.includes(item.questId);
        if ((matchesExistingQuestId || matchesExistingId) && existing.type === item.type) {
          return true;
        }
      }
      return false;
    });

    if (isDuplicate) {
      return state;
    }

    return { feed: [item, ...state.feed.slice(0, 19)] };
  }),
  setFeed: (feed: FeedItem[]) => set({ feed })
}));
