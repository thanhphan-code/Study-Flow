export type DocumentFileType = 'Pdf' | 'Docx' | 'Pptx' | 'Txt'
export type DocumentProcessingStatus = 'Uploaded' | 'Queued' | 'Processing' | 'Ready' | 'Failed'
export interface DocumentListItem { id: string; subjectId: string | null; studySetId: string | null; originalFileName: string; mimeType: string; fileType: DocumentFileType; fileSize: number; processingStatus: DocumentProcessingStatus; retryCount: number; canRetry: boolean; processingRevision: string; createdAt: string }
export interface ImportedDocument extends DocumentListItem { extractedText: string | null; failureCode: string | null; processingError: string | null; queuedAt: string | null; processingStartedAt: string | null; processingCompletedAt: string | null; failedAt: string | null; chunkCount: number }
