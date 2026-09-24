import type { FlashcardStatus } from '@/features/progress/types/progress'

export interface ProgressOverview { totalStudySets: number; totalCards: number; newCards: number; learningCards: number; reviewingCards: number; masteredCards: number; cardsReviewed: number; quizAttempts: number; quizAccuracy: number; weakCards: number; recommendedReviewCards: number }
export interface StudySetProgress { studySetId: string; title: string; totalCards: number; newCards: number; learningCards: number; reviewingCards: number; masteredCards: number; cardsReviewed: number; quizAttempts: number; quizAccuracy: number; weakCards: number }
export interface WeakCard { flashcardId: string; studySetId: string; studySetTitle: string; frontText: string; status: FlashcardStatus; correctCount: number; wrongCount: number; easeFactor: number; intervalDays: number; nextReviewAt: string | null; weaknessScore: number; isDue: boolean; insight: string }
export interface Streak { currentStreak: number; longestStreak: number; totalActiveDays: number; lastStudyDate: string | null }
export interface LearningHistoryDay { date: string; sessions: number; studyTimeSeconds: number; cardsReviewed: number; questionsAnswered: number; correctAnswers: number; accuracy: number }
export interface LearningHistory { from: string; to: string; activeDays: number; totalSessions: number; totalStudyTimeSeconds: number; days: LearningHistoryDay[] }
