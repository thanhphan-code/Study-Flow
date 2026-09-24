import { apiRequest } from '@/api/httpClient'
import type { StudySet, StudySetInput, StudySetSource } from '@/features/studySets/types/studySet'
import type { Flashcard } from '@/features/flashcards/types/flashcard'

export const studySetKeys = {
  bySubject: (subjectId: string) => ['study-sets', 'subject', subjectId] as const,
  detail: (id: string) => ['study-sets', id] as const,
}

export const studySetsApi = {
  list: (subjectId: string) => apiRequest<StudySet[]>(`/subjects/${subjectId}/study-sets`),
  get: (id: string) => apiRequest<StudySet>(`/study-sets/${id}`),
  create: (subjectId: string, input: StudySetInput) => apiRequest<StudySet>(`/subjects/${subjectId}/study-sets`, { method: 'POST', body: JSON.stringify(input) }),
  update: (id: string, input: StudySetInput) => apiRequest<StudySet>(`/study-sets/${id}`, { method: 'PUT', body: JSON.stringify(input) }),
  remove: (id: string) => apiRequest<void>(`/study-sets/${id}`, { method: 'DELETE' }),
  createCombined: (subjectId: string, input: StudySetInput & { sourceStudySetIds: string[] }) => apiRequest<StudySet>(`/subjects/${subjectId}/combined-study-sets`, { method: 'POST', body: JSON.stringify(input) }),
  updateSources: (id: string, sourceStudySetIds: string[]) => apiRequest<StudySet>(`/study-sets/${id}/sources`, { method: 'PUT', body: JSON.stringify({ sourceStudySetIds }) }),
  studyTogether: (sourceStudySetIds: string[]) => apiRequest<{ sources: StudySetSource[]; flashcards: Flashcard[] }>('/study-together', { method: 'POST', body: JSON.stringify({ sourceStudySetIds }) }),
}
