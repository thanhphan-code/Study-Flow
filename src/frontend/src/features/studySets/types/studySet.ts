export type StudySetSource = { id: string; title: string; orderIndex: number }
export type StudySet = { id: string; subjectId: string; title: string; description: string | null; type: 'Standard' | 'Combined'; sources: StudySetSource[]; totalCards: number; createdAt: string; updatedAt: string }
export type StudySetInput = { title: string; description: string | null }
