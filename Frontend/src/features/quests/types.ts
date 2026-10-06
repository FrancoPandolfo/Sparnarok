export enum QuestState {
  Pending = 0,
  InProgress = 1,
  Completed = 2,
  Failed = 3
}

export enum QuestDifficulty {
  F = 0, E = 1, D = 2, C = 3, B = 4, A = 5, S = 6
}

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
  rewards: QuestReward[];
  createdAt: string;
}

export interface CreateQuestPayload {
  title: string;
  description: string;
  difficulty: QuestDifficulty;
  rewards: QuestReward[];
}
