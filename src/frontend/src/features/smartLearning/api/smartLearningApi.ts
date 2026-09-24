import { apiRequest } from '@/api/httpClient'
import type { LearningAttempt, LearningAttemptInput, SmartLearnSession, SmartLearnSummary } from '@/features/smartLearning/types/smartLearning'

export const smartLearningApi = {
  start: (studySetId: string, itemCount: number) => apiRequest<SmartLearnSession>(`/study-sets/${studySetId}/smart-learn`, { method: 'POST', body: JSON.stringify({ itemCount }) }),
  active: (studySetId: string) => apiRequest<SmartLearnSession>(`/study-sets/${studySetId}/smart-learn/active`),
  record: (sessionId: string, input: LearningAttemptInput) => apiRequest<LearningAttempt>(`/smart-learn/sessions/${sessionId}/attempts`, { method: 'POST', body: JSON.stringify(input) }),
  complete: (sessionId: string) => apiRequest<SmartLearnSummary>(`/smart-learn/sessions/${sessionId}/complete`, { method: 'POST' }),
}
