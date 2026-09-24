export type SourceReference = {
  id: string
  documentId: string | null
  documentName: string
  documentChunkId: string | null
  pageNumber: number | null
  slideNumber: number | null
  sectionTitle: string | null
  snippet: string
  startOffset: number
  endOffset: number
  documentAvailable: boolean
}

