export type StudyMode = 'Flashcard' | 'Review' | 'Learn' | 'DailyStudy' | 'StudyTogether'

export interface StudySession {
  id: string
  studySetId: string
  mode: StudyMode | 'Quiz'
  startedAt: string
  endedAt: string | null
  cardsStudied: number
  questionsAnswered: number
  correctAnswers: number
  durationSeconds: number
}
