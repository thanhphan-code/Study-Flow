import { apiRequest } from '@/api/httpClient'
import type { StudyMode, StudySession } from '@/features/studySessions/types/studySession'

export const studySessionsApi = {
  start: (studySetId: string, mode: StudyMode) => apiRequest<StudySession>('/study-sessions', { method: 'POST', body: JSON.stringify({ studySetId, mode }) }),
  complete: (id: string) => apiRequest<StudySession>(`/study-sessions/${id}/complete`, { method: 'POST' }),
}
