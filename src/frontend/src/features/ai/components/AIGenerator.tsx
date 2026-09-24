import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { aiApi } from '@/features/ai/api/aiApi'
import type { AIJob } from '@/features/ai/types/ai'
import { documentKeys, documentsApi } from '@/features/documents/api/documentsApi'
import { flashcardKeys } from '@/features/flashcards/api/flashcardsApi'
import { quizKeys } from '@/features/quizzes/api/quizzesApi'
import { useLanguage } from '@/i18n/LanguageProvider'

export function AIGenerator({ studySetId }: { studySetId: string }) {
  const [open, setOpen] = useState(false)
  const [source, setSource] = useState<'document' | 'paste'>('document')
  const [documentId, setDocumentId] = useState('')
  const [text, setText] = useState('')
  const [cards, setCards] = useState(true)
  const [quiz, setQuiz] = useState(true)
  const [cardCount, setCardCount] = useState(20)
  const [quizCount, setQuizCount] = useState(10)
  const [difficulty, setDifficulty] = useState('Mixed')
  const [jobs, setJobs] = useState<AIJob[]>([])
  const queryClient = useQueryClient()
  const {language}=useLanguage();const vi=language==='vi';const copy=vi?{open:'Tạo bằng AI',title:'Tạo tài liệu học',close:'Đóng',document:'Tài liệu',paste:'Dán văn bản',choose:'Chọn tài liệu đã xử lý',placeholder:'Dán nội dung nguồn…',quiz:'Trắc nghiệm',difficulty:'Độ khó',easy:'Dễ',medium:'Vừa',hard:'Khó',mixed:'Kết hợp',grounded:'AI chỉ dùng nội dung nguồn. Kết quả là bản nháp cho đến khi bạn kiểm tra và lưu.',generating:'Đang tạo…',generate:'Tạo bản nháp',quizDraft:'Bản nháp trắc nghiệm',cardDraft:'Bản nháp flashcard',saved:'Đã lưu',save:'Lưu bản nháp đã duyệt'}:{open:'Generate with AI',title:'Generate study material',close:'Close',document:'Document',paste:'Paste text',choose:'Choose a completed document',placeholder:'Paste source material…',quiz:'Quiz',difficulty:'Difficulty',easy:'Easy',medium:'Medium',hard:'Hard',mixed:'Mixed',grounded:'AI only uses the provided source. Content stays as a draft until you review and save it.',generating:'Generating…',generate:'Generate draft',quizDraft:'Quiz draft',cardDraft:'Flashcard draft',saved:'Saved',save:'Save reviewed draft'}
  const documents = useQuery({ queryKey: documentKeys.all, queryFn: documentsApi.list, enabled: open })
  const available = documents.data?.filter((item) => item.processingStatus === 'Ready' && (!item.studySetId || item.studySetId === studySetId)) ?? []
  const sourceName = (job:AIJob) => available.find(document=>document.id===job.documentId)?.originalFileName

  const generate = useMutation({
    mutationFn: () => aiApi.generate(studySetId, { documentId: source === 'document' ? documentId : null, pastedText: source === 'paste' ? text : null, generateFlashcards: cards, generateQuiz: quiz, flashcardCount: cardCount, quizQuestionCount: quizCount, difficulty, documentOnly: true }),
    onSuccess: setJobs,
  })
  const save = useMutation({
    mutationFn: (job: AIJob) => aiApi.save(job.id, job.draft!),
    onSuccess: async (_, savedJob) => {
      setJobs((current) => current.map((job) => job.id === savedJob.id ? { ...job, savedAt: new Date().toISOString() } : job))
      await Promise.all([queryClient.invalidateQueries({ queryKey: flashcardKeys.byStudySet(studySetId) }), queryClient.invalidateQueries({ queryKey: quizKeys.byStudySet(studySetId) })])
    },
  })

  const changeJob = (jobIndex: number, update: (job: AIJob) => AIJob) => setJobs((current) => current.map((job, index) => index === jobIndex ? update(job) : job))
  const updateCard = (jobIndex: number, cardIndex: number, field: 'frontText' | 'backText' | 'languageCode' | 'readingText' | 'romanization' | 'exampleText' | 'exampleTranslation' | 'memoryTip' | 'acceptedAnswers', value: string) => changeJob(jobIndex, (job) => ({ ...job, draft: { ...job.draft!, flashcards: job.draft!.flashcards.map((card, index) => index === cardIndex ? { ...card, [field]: field === 'frontText' || field === 'backText' ? value : value || null } : card) } }))
  const updateQuestion = (jobIndex: number, questionIndex: number, value: string) => changeJob(jobIndex, (job) => ({ ...job, draft: { ...job.draft!, questions: job.draft!.questions.map((question, index) => index === questionIndex ? { ...question, questionText: value } : question) } }))
  const updateOption = (jobIndex: number, questionIndex: number, optionIndex: number, value: string) => changeJob(jobIndex, (job) => ({ ...job, draft: { ...job.draft!, questions: job.draft!.questions.map((question, index) => index !== questionIndex ? question : { ...question, options: question.options.map((option, position) => position === optionIndex ? { ...option, text: value } : option) }) } }))

  if (!open) return <button onClick={() => setOpen(true)} className="sf-primary rounded-xl px-5 py-3 font-bold text-white">✨ {copy.open}</button>
  return <section className="sf-card mt-6 rounded-[1.7rem] p-6">
    <div className="flex items-center justify-between"><div><p className="text-sm font-semibold text-blue-600">Gemini 2.5 Flash-Lite</p><h2 className="mt-1 text-2xl font-bold">{copy.title}</h2></div><button onClick={() => setOpen(false)} className="text-slate-500">{copy.close}</button></div>
    {jobs.length === 0 ? <>
      <div className="mt-6 flex gap-2"><button onClick={() => setSource('document')} className={`rounded-xl px-4 py-2 text-sm font-bold ${source === 'document' ? 'bg-blue-600 text-white' : 'bg-slate-100'}`}>{copy.document}</button><button onClick={() => setSource('paste')} className={`rounded-xl px-4 py-2 text-sm font-bold ${source === 'paste' ? 'bg-blue-600 text-white' : 'bg-slate-100'}`}>{copy.paste}</button></div>
      {source === 'document' ? <select value={documentId} onChange={(event) => setDocumentId(event.target.value)} className="mt-4 w-full rounded-xl border border-slate-200 bg-white p-3"><option value="">{copy.choose}</option>{available.map((document) => <option key={document.id} value={document.id}>{document.originalFileName}</option>)}</select> : <textarea value={text} onChange={(event) => setText(event.target.value)} maxLength={60000} rows={7} placeholder={copy.placeholder} className="mt-4 w-full rounded-xl border border-slate-200 bg-white p-3" />}
      <div className="mt-5 grid gap-4 sm:grid-cols-3">
        <label className="rounded-xl bg-blue-50 p-4"><input type="checkbox" checked={cards} onChange={(event) => setCards(event.target.checked)} /><span className="ml-2 font-bold">Flashcards</span><input type="number" min="1" max="50" value={cardCount} onChange={(event) => setCardCount(Number(event.target.value))} className="mt-3 w-full rounded-lg bg-white p-2" /></label>
        <label className="rounded-xl bg-violet-50 p-4"><input type="checkbox" checked={quiz} onChange={(event) => setQuiz(event.target.checked)} /><span className="ml-2 font-bold">{copy.quiz}</span><input type="number" min="1" max="50" value={quizCount} onChange={(event) => setQuizCount(Number(event.target.value))} className="mt-3 w-full rounded-lg bg-white p-2" /></label>
        <label className="p-4 font-bold">{copy.difficulty}<select value={difficulty} onChange={(event) => setDifficulty(event.target.value)} className="mt-3 w-full rounded-lg border border-slate-200 bg-white p-2"><option value="Easy">{copy.easy}</option><option value="Medium">{copy.medium}</option><option value="Hard">{copy.hard}</option><option value="Mixed">{copy.mixed}</option></select></label>
      </div>
      <p className="mt-4 text-xs text-slate-500">{copy.grounded}</p>
      <button disabled={generate.isPending || (!cards && !quiz) || (source === 'document' ? !documentId : !text.trim())} onClick={() => generate.mutate()} className="sf-primary mt-5 rounded-xl px-6 py-3 font-bold text-white disabled:opacity-50">{generate.isPending ? copy.generating : copy.generate}</button>
      {generate.isError && <p className="mt-3 text-sm text-red-700">{generate.error.message}</p>}
    </> : <div className="mt-7 space-y-6">{jobs.map((job, jobIndex) => <article key={job.id} className="rounded-2xl bg-slate-50 p-5">
      <div className="flex justify-between"><h3 className="font-bold">{job.jobType === 'GenerateQuiz' ? copy.quizDraft : copy.cardDraft}</h3><span className={job.status === 'Completed' ? 'text-emerald-700' : 'text-red-700'}>{job.status}</span></div>
      {job.errorMessage && <p className="mt-3 text-sm text-red-700">{job.errorMessage}</p>}
      {job.draft?.flashcards.map((card, index) => <div key={index} className="mt-4 rounded-xl bg-white p-3"><div className="grid gap-2 sm:grid-cols-2"><textarea aria-label={vi?'Mặt trước':'Front'} value={card.frontText} onChange={(event) => updateCard(jobIndex, index, 'frontText', event.target.value)} className="rounded-xl border border-slate-200 bg-white p-3" /><textarea aria-label={vi?'Mặt sau':'Back'} value={card.backText} onChange={(event) => updateCard(jobIndex, index, 'backText', event.target.value)} className="rounded-xl border border-slate-200 bg-white p-3" /></div><div className="mt-2 grid gap-2 sm:grid-cols-3"><input aria-label={vi?'Ngôn ngữ':'Language'} value={card.languageCode??''} onChange={(event)=>updateCard(jobIndex,index,'languageCode',event.target.value)} placeholder="ja-JP" className="rounded-lg border border-blue-100 p-2 text-sm"/><input aria-label={vi?'Cách đọc':'Reading'} value={card.readingText??''} onChange={(event)=>updateCard(jobIndex,index,'readingText',event.target.value)} placeholder="Reading · たべる" className="rounded-lg border border-blue-100 p-2 text-sm"/><input aria-label={vi?'Phiên âm':'Romanization'} value={card.romanization??''} onChange={(event)=>updateCard(jobIndex,index,'romanization',event.target.value)} placeholder="Romanization · taberu" className="rounded-lg border border-blue-100 p-2 text-sm"/></div><div className="mt-2 grid gap-2 sm:grid-cols-2"><textarea aria-label={vi?'Ví dụ':'Example'} value={card.exampleText??''} onChange={event=>updateCard(jobIndex,index,'exampleText',event.target.value)} placeholder="Example" className="rounded-lg border border-amber-100 p-2 text-sm"/><textarea aria-label={vi?'Dịch ví dụ':'Example translation'} value={card.exampleTranslation??''} onChange={event=>updateCard(jobIndex,index,'exampleTranslation',event.target.value)} placeholder="Example translation" className="rounded-lg border border-amber-100 p-2 text-sm"/><textarea aria-label={vi?'Mẹo nhớ':'Memory tip'} value={card.memoryTip??''} onChange={event=>updateCard(jobIndex,index,'memoryTip',event.target.value)} placeholder="Memory tip" className="rounded-lg border border-amber-100 p-2 text-sm"/><textarea aria-label={vi?'Đáp án khác được chấp nhận':'Accepted answers'} value={card.acceptedAnswers??''} onChange={event=>updateCard(jobIndex,index,'acceptedAnswers',event.target.value)} placeholder="Accepted answers" className="rounded-lg border border-amber-100 p-2 text-sm"/></div>{card.sourceIds?.length?<div className="mt-3 flex flex-wrap gap-1">{card.sourceIds.map(sourceId=><span key={sourceId} className="rounded-lg bg-indigo-50 px-2 py-1 text-xs font-semibold text-indigo-700">{sourceName(job)??(vi?'Tài liệu đã chọn':'Selected document')} · {sourceId}</span>)}</div>:null}</div>)}
      {job.draft?.questions.map((question, questionIndex) => <div key={questionIndex} className="mt-4 rounded-xl bg-white p-3"><textarea aria-label={vi?'Nội dung câu hỏi':'Question text'} value={question.questionText} onChange={(event) => updateQuestion(jobIndex, questionIndex, event.target.value)} className="w-full resize-none font-bold outline-none" /><div className="mt-2 space-y-2">{question.options.map((option, optionIndex) => <label key={optionIndex} className="flex items-center gap-2 text-sm"><span aria-label={option.isCorrect?(vi?'Đáp án đúng':'Correct answer'):(vi?'Đáp án sai':'Incorrect answer')}>{option.isCorrect ? '✓' : '○'}</span><input aria-label={`${vi?'Lựa chọn':'Option'} ${optionIndex+1}`} value={option.text} onChange={(event) => updateOption(jobIndex, questionIndex, optionIndex, event.target.value)} className="w-full rounded-lg border border-slate-200 px-3 py-2" /></label>)}</div>{question.sourceIds?.length?<div className="mt-3 flex flex-wrap gap-1">{question.sourceIds.map(sourceId=><span key={sourceId} className="rounded-lg bg-indigo-50 px-2 py-1 text-xs font-semibold text-indigo-700">{sourceName(job)??(vi?'Tài liệu đã chọn':'Selected document')} · {sourceId}</span>)}</div>:null}</div>)}
      {job.status === 'Completed' && job.draft && <button disabled={save.isPending || Boolean(job.savedAt)} onClick={() => save.mutate(job)} className="mt-5 rounded-xl bg-blue-600 px-4 py-2.5 text-sm font-bold text-white disabled:opacity-50">{job.savedAt ? copy.saved : copy.save}</button>}
      {save.isError && <p className="mt-3 text-sm text-red-700">{save.error.message}</p>}
    </article>)}</div>}
  </section>
}
