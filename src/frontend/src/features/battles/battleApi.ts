import { apiRequest } from "@/api/httpClient";

export type Option = { id: string; text: string };
export type Source = {
  name: string;
  page: number | null;
  slide: number | null;
  section: string | null;
  snippet: string;
};
export type BattleQuestion = {
  id: string;
  questionType: string;
  prompt: string;
  difficulty: string;
  number: number;
  timeLimitSeconds: number | null;
  startedAt: string;
  endedAt: string | null;
  options: Option[];
  leftItems: Option[];
};
export type Player = {
  userId: string;
  name: string;
  isHost: boolean;
  status: string;
  score: number | null;
  rank: number | null;
  correctCount: number | null;
  accuracy: number | null;
  averageResponseSeconds: number | null;
};
export type Review = {
  questionId: string;
  prompt: string;
  correctAnswer: string;
  explanation: string | null;
  evaluation: string;
  source: Source | null;
  roomAccuracy: number;
};
export type BattleRoom = {
  id: string;
  studySetId: string;
  hostUserId: string;
  name: string;
  joinCode: string;
  status: string;
  mode: string;
  difficulty: string;
  leaderboardMode: string;
  maxPlayers: number;
  questionCount: number;
  timeLimitSeconds: number | null;
  serverTime: string;
  playerCount: number;
  isMember: boolean;
  answeredCount: number;
  myScore: number;
  myCombo: number;
  players: Player[];
  question: BattleQuestion | null;
  myAnswer: {
    questionId: string;
    evaluation: string;
    scoreEarned: number;
    correctAnswer: string;
    explanation: string | null;
    source: Source | null;
  } | null;
  reviews: Review[];
  reviewStudySetId: string | null;
};
export type BattleSettings = {
  studySetId: string;
  name: string;
  questionCount: number;
  maxPlayers: number;
  timeLimitSeconds: number | null;
  questionTypes: string[];
  difficulty: string;
  mode: string;
  leaderboardMode: string;
};
export const battleApi = {
  create: (settings: BattleSettings) =>
    apiRequest<BattleRoom>("/battles", {
      method: "POST",
      body: JSON.stringify(settings),
    }),
  get: (id: string) => apiRequest<BattleRoom>(`/battles/${id}`),
  find: (code: string) =>
    apiRequest<BattleRoom>(`/battles/join/${encodeURIComponent(code)}`),
  act: (id: string, action: string) =>
    apiRequest<BattleRoom>(`/battles/${id}/${action}`, { method: "POST" }),
  answer: (id: string, questionId: string, answer: string) =>
    apiRequest<BattleRoom>(`/battles/${id}/questions/${questionId}/answer`, {
      method: "POST",
      body: JSON.stringify({ answer }),
    }),
};
export const questionLabels: Record<string, string> = {
  MultipleChoice: "Trắc nghiệm",
  TrueFalse: "Đúng / Sai",
  FillBlank: "Điền đáp án",
  Matching: "Ghép cặp",
  Ordering: "Sắp xếp",
  ShortAnswer: "Trả lời ngắn",
};
