export const QuestState = {
  Pending: 0,
  InProgress: 1,
  Completed: 2,
  Failed: 3
} as const;

export type QuestState = typeof QuestState[keyof typeof QuestState];

export const QuestDifficulty = {
  F: 0, E: 1, D: 2, C: 3, B: 4, A: 5, S: 6
} as const;

export type QuestDifficulty = typeof QuestDifficulty[keyof typeof QuestDifficulty];

export interface QuestReward {
  skillCategoryId: string;
  xpAmount: number;
}

export interface Quest {
  id: string;
  title: string;
  description: string;
  difficulty: QuestDifficulty;
  state: QuestState;
  tags?: string[];
  rewards: QuestReward[];
  createdAt: string;
}

export interface CreateQuestPayload {
  title: string;
  description: string;
  difficulty: QuestDifficulty;
  rewards: QuestReward[];
}
