import { apiRequest } from '@/api/httpClient'
import type { SourceReference } from '@/features/grounding/types/source'

export const sourcesApi = {
  flashcard: (id: string) => apiRequest<SourceReference[]>(`/flashcards/${id}/sources`),
  question: (id: string) => apiRequest<SourceReference[]>(`/quiz-questions/${id}/sources`),
  job: (id: string) => apiRequest<SourceReference[]>(`/ai/jobs/${id}/sources`),
}

