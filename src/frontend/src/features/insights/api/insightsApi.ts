import { apiRequest } from '@/api/httpClient'
import type { LearningHistory, ProgressOverview, Streak, StudySetProgress, WeakCard } from '@/features/insights/types/insights'

export const insightKeys = { overview: ['progress', 'overview'] as const, weakCards: ['progress', 'weak-cards'] as const, streak: ['progress', 'streak'] as const, history: (days: number) => ['progress', 'history', days] as const, studySet: (id: string) => ['study-sets', id, 'progress'] as const }
export const insightsApi = {
  overview: () => apiRequest<ProgressOverview>('/progress/overview'),
  studySet: (id: string) => apiRequest<StudySetProgress>(`/study-sets/${id}/progress`),
  weakCards: () => apiRequest<WeakCard[]>('/progress/weak-cards'),
  streak: () => apiRequest<Streak>('/progress/streak'),
  history: (days = 30) => apiRequest<LearningHistory>(`/progress/history?days=${days}`),
}
