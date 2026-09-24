import { apiRequest } from '@/api/httpClient'
import type { Subject, SubjectInput } from '@/features/subjects/types/subject'

export const subjectKeys = {
  all: ['subjects'] as const,
  detail: (id: string) => ['subjects', id] as const,
}

export const subjectsApi = {
  list: () => apiRequest<Subject[]>('/subjects'),
  get: (id: string) => apiRequest<Subject>(`/subjects/${id}`),
  create: (input: SubjectInput) => apiRequest<Subject>('/subjects', { method: 'POST', body: JSON.stringify(input) }),
  update: (id: string, input: SubjectInput) => apiRequest<Subject>(`/subjects/${id}`, { method: 'PUT', body: JSON.stringify(input) }),
  remove: (id: string) => apiRequest<void>(`/subjects/${id}`, { method: 'DELETE' }),
}
