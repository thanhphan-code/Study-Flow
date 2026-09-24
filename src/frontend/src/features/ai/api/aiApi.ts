import { apiRequest } from '@/api/httpClient'
import type { AIDraft,AIJob,AITextResult } from '@/features/ai/types/ai'
export const aiApi={
  generate:(studySetId:string,input:{documentId:string|null;pastedText:string|null;generateFlashcards:boolean;generateQuiz:boolean;flashcardCount:number;quizQuestionCount:number;difficulty:string;documentOnly:boolean})=>apiRequest<AIJob[]>(`/study-sets/${studySetId}/ai/generate`,{method:'POST',body:JSON.stringify(input)}),
  save:(jobId:string,draft:AIDraft,quizTitle='AI Quiz')=>apiRequest(`/ai/jobs/${jobId}/save`,{method:'POST',body:JSON.stringify({...draft,quizTitle})}),
  explain:(cardId:string)=>apiRequest<AITextResult>(`/flashcards/${cardId}/ai/explain`,{method:'POST'}),
  similar:(cardId:string)=>apiRequest<AITextResult>(`/flashcards/${cardId}/ai/similar-question`,{method:'POST'}),
  hint:(cardId:string,level:number)=>apiRequest<AITextResult>(`/flashcards/${cardId}/ai/hint`,{method:'POST',body:JSON.stringify({level})}),
}
