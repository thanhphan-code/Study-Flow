import { apiRequest } from '@/api/httpClient'
import type { ManualQuizInput, Quiz, QuizResult, QuizSession } from '@/features/quizzes/types/quiz'

export const quizKeys = { byStudySet: (id: string) => ['quizzes', 'study-set', id] as const }
export const quizzesApi = {
  list: (studySetId: string) => apiRequest<Quiz[]>(`/study-sets/${studySetId}/quizzes`),
  create: (studySetId: string, input: { title: string; questionCount: number }) => apiRequest<Quiz>(`/study-sets/${studySetId}/quizzes`, { method: 'POST', body: JSON.stringify(input) }),
  createManual: (studySetId: string, input: ManualQuizInput) => apiRequest<Quiz>(`/study-sets/${studySetId}/quizzes/manual`, { method: 'POST', body: JSON.stringify(input) }),
  start: (quizId: string) => apiRequest<QuizSession>(`/quizzes/${quizId}/attempts`, { method: 'POST' }),
  answer: (attemptId: string, questionId: string, answerOptionId: string) => apiRequest(`/quiz-attempts/${attemptId}/answers`, { method: 'POST', body: JSON.stringify({ questionId, answerOptionId }) }),
  complete: (attemptId: string) => apiRequest<QuizResult>(`/quiz-attempts/${attemptId}/complete`, { method: 'POST' }),
}
