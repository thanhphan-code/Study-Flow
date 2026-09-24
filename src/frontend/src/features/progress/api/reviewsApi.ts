import { apiRequest } from '@/api/httpClient'
import type { DueFlashcard, FlashcardProgress, ReviewRating } from '@/features/progress/types/progress'
import type { StudyMode } from '@/features/studySessions/types/studySession'

export const reviewKeys = { due: ['reviews', 'due'] as const }
type ReviewEvidence = { submittedAnswer: string; attemptType?: 'Recall' | 'MultipleChoice' | 'TrueFalse'; direction?: 'Forward' | 'Reverse'; confidence?: number; responseTimeMs?: number; hintUsed?: boolean; clientAttemptId?: string }
export const reviewsApi = {
  review: (flashcardId: string, rating: ReviewRating, studySessionId?: string, mode?: StudyMode, evidence?: ReviewEvidence) => apiRequest<FlashcardProgress>(`/flashcards/${flashcardId}/review`, { method: 'POST', body: JSON.stringify({ rating, studySessionId, mode, ...evidence }) }),
  due: () => apiRequest<DueFlashcard[]>('/reviews/due'),
}
