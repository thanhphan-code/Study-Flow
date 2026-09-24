import { apiRequest } from '@/api/httpClient'
import type { Flashcard, FlashcardInput } from '@/features/flashcards/types/flashcard'

export const flashcardKeys = { byStudySet: (studySetId: string) => ['flashcards', 'study-set', studySetId] as const }
export const flashcardsApi = {
  list: (studySetId: string) => apiRequest<Flashcard[]>(`/study-sets/${studySetId}/flashcards`),
  create: (studySetId: string, input: FlashcardInput) => apiRequest<Flashcard>(`/study-sets/${studySetId}/flashcards`, { method: 'POST', body: JSON.stringify(input) }),
  bulkCreate: (studySetId: string, cards: FlashcardInput[]) => apiRequest<Flashcard[]>(`/study-sets/${studySetId}/flashcards/bulk`, { method: 'POST', body: JSON.stringify({ cards }) }),
  update: (id: string, input: FlashcardInput) => apiRequest<Flashcard>(`/flashcards/${id}`, { method: 'PUT', body: JSON.stringify(input) }),
  remove: (id: string) => apiRequest<void>(`/flashcards/${id}`, { method: 'DELETE' }),
  uploadImage: (id: string, file: File) => { const body = new FormData(); body.append('file', file); return apiRequest<Flashcard>(`/flashcards/${id}/image`, { method: 'POST', body }) },
  removeImage: (id: string) => apiRequest<void>(`/flashcards/${id}/image`, { method: 'DELETE' }),
}
