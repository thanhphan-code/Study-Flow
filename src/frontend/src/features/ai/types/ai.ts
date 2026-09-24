import type { SourceReference } from '@/features/grounding/types/source'
export interface GeneratedFlashcard { frontText:string; backText:string; explanation:string|null; languageCode:string|null; readingText:string|null; romanization:string|null; exampleText:string|null; exampleTranslation:string|null; memoryTip:string|null; acceptedAnswers:string|null; sourceIds?:string[] }
export interface GeneratedOption { text:string; isCorrect:boolean }
export interface GeneratedQuestion { type:'MultipleChoice'|'TrueFalse'; questionText:string; explanation:string|null; options:GeneratedOption[]; sourceIds?:string[] }
export interface AIDraft { flashcards:GeneratedFlashcard[]; questions:GeneratedQuestion[] }
export interface AIJob { id:string; studySetId:string; documentId:string|null; jobType:'GenerateFlashcards'|'GenerateQuiz'|'Explain'|'GenerateSimilarQuestion'|'TutorHint'; status:'Pending'|'Processing'|'Completed'|'Failed'; errorMessage:string|null; draft:AIDraft|null; savedAt:string|null }
export interface AITextResult { jobId:string; text:string; sources:SourceReference[] }
