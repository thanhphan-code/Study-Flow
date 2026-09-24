export interface RecentStudySet { id: string; subjectId: string; title: string; lastStudiedAt: string }
export interface Dashboard {
  dueCards: number
  studyTimeSecondsToday: number
  cardsReviewedToday: number
  questionsAnsweredToday: number
  accuracyToday: number
  recentStudySets: RecentStudySet[]
}
