import { apiRequest } from '@/api/httpClient'
import type { DocumentListItem, ImportedDocument } from '@/features/documents/types/document'

export const documentKeys = { all: ['documents'] as const, detail: (id: string) => ['documents', id] as const }
export const documentsApi = {
  list: () => apiRequest<DocumentListItem[]>('/documents'),
  get: (id: string) => apiRequest<ImportedDocument>(`/documents/${id}`),
  upload: (file: File, studySetId?: string) => { const body = new FormData(); body.append('file', file); return apiRequest<ImportedDocument>(studySetId ? `/study-sets/${studySetId}/documents` : '/documents', { method: 'POST', body }) },
  retry: (id: string) => apiRequest<ImportedDocument>(`/documents/${id}/retry`, { method: 'POST' }),
  remove: (id: string) => apiRequest<void>(`/documents/${id}`, { method: 'DELETE' }),
}
