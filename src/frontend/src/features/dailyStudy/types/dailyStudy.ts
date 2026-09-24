export type DailyStudyFocus = 'Balanced' | 'Written' | 'Language'
export type DailyStudyActivity = 'Written' | 'Listening' | 'ReverseWritten'
export interface DailyStudyItem { flashcardId:string; studySetId:string; studySetTitle:string; frontText:string; backText:string; explanation:string|null; languageCode:string|null; readingText:string|null; romanization:string|null; exampleText:string|null; exampleTranslation:string|null; memoryTip:string|null; acceptedAnswers:string|null; imageUrl:string|null; activity:DailyStudyActivity; reason:'Due'|'Weak'|'New'|'Strengthen' }
export interface DailyStudyPlan { minutes:number; focus:DailyStudyFocus; dueCount:number; weakCount:number; newCount:number; items:DailyStudyItem[] }
