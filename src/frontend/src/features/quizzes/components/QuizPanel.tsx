import { useState, type FormEvent } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Link } from 'react-router-dom'
import { ApiRequestError } from '@/api/httpClient'
import { quizKeys, quizzesApi } from '@/features/quizzes/api/quizzesApi'
import { ManualQuizEditor } from '@/features/quizzes/components/ManualQuizEditor'
import type { ManualQuizInput } from '@/features/quizzes/types/quiz'
import { useLanguage } from '@/i18n/LanguageProvider'

export function QuizPanel({ studySetId, cardCount }: { studySetId: string; cardCount: number }) {
  const { language } = useLanguage(); const vi = language === 'vi'
  const [creating, setCreating] = useState(false); const [mode, setMode] = useState<'cards' | 'manual'>('manual'); const [error, setError] = useState(''); const queryClient = useQueryClient()
  const copy = vi ? { title: 'Bài trắc nghiệm', intro: 'Tự tạo câu hỏi và đáp án, hoặc sinh nhanh từ flashcard.', create: 'Tạo bài trắc nghiệm', manual: 'Tự nhập câu hỏi', cards: 'Sinh từ flashcard', quizTitle: 'Tên bài trắc nghiệm', count: 'Số câu', cancel: 'Hủy', generate: 'Tạo từ thẻ', creating: 'Đang tạo…', questions: 'câu hỏi', empty: 'Chưa có bài trắc nghiệm.', noCards: 'Cần có flashcard trước khi sinh tự động.', failed: 'Không thể tạo bài trắc nghiệm.' } : { title: 'Quizzes', intro: 'Write questions and answers yourself, or generate quickly from flashcards.', create: 'Create quiz', manual: 'Write questions', cards: 'Generate from cards', quizTitle: 'Quiz title', count: 'Questions', cancel: 'Cancel', generate: 'Generate from cards', creating: 'Creating…', questions: 'questions', empty: 'No quizzes yet.', noCards: 'Add flashcards before generating automatically.', failed: 'Unable to create quiz.' }
  const quizzes = useQuery({ queryKey: quizKeys.byStudySet(studySetId), queryFn: () => quizzesApi.list(studySetId) })
  const afterCreate = async () => { await queryClient.invalidateQueries({ queryKey: quizKeys.byStudySet(studySetId) }); setCreating(false); setError('') }
  const create = useMutation({ mutationFn: (input: { title: string; questionCount: number }) => quizzesApi.create(studySetId, input), onSuccess: afterCreate })
  const createManual = useMutation({ mutationFn: (input: ManualQuizInput) => quizzesApi.createManual(studySetId, input), onSuccess: afterCreate })
  async function submitGenerated(event: FormEvent<HTMLFormElement>) { event.preventDefault(); setError(''); const data = new FormData(event.currentTarget); try { await create.mutateAsync({ title: String(data.get('title')).trim(), questionCount: Number(data.get('questionCount')) }) } catch (reason) { setError(reason instanceof ApiRequestError ? reason.message : copy.failed) } }

  return <section className="mt-12 border-t border-slate-200 pt-10">
    <div className="flex flex-wrap items-end justify-between gap-4"><div><h2 className="text-2xl font-semibold text-slate-950">{copy.title}</h2><p className="mt-1 text-sm text-slate-500">{copy.intro}</p></div><button onClick={() => setCreating((value) => !value)} className="rounded-xl bg-slate-900 px-5 py-3 font-semibold text-white transition active:scale-[.98]">{copy.create}</button></div>
    {creating && <div className="mt-6 rounded-2xl bg-slate-50 p-2 sm:p-5">
      <div className="mb-5 inline-flex rounded-xl bg-slate-200/70 p-1"><button onClick={() => setMode('manual')} className={`rounded-lg px-4 py-2 text-sm font-bold ${mode === 'manual' ? 'bg-white text-blue-700 shadow-sm' : 'text-slate-600'}`}>{copy.manual}</button><button onClick={() => setMode('cards')} className={`rounded-lg px-4 py-2 text-sm font-bold ${mode === 'cards' ? 'bg-white text-blue-700 shadow-sm' : 'text-slate-600'}`}>{copy.cards}</button></div>
      {mode === 'manual' ? <ManualQuizEditor pending={createManual.isPending} onCancel={() => setCreating(false)} onSave={(input) => createManual.mutateAsync(input).then(() => undefined)} /> : <form onSubmit={submitGenerated} className="grid gap-4 rounded-2xl bg-white p-5 sm:grid-cols-[1fr_9rem_auto]"><label className="text-sm font-semibold text-slate-600">{copy.quizTitle}<input required maxLength={150} name="title" className="mt-2 w-full rounded-xl border border-slate-300 px-4 py-3" /></label><label className="text-sm font-semibold text-slate-600">{copy.count}<input required min={1} max={Math.min(50, cardCount)} defaultValue={Math.min(10, cardCount)} type="number" name="questionCount" className="mt-2 w-full rounded-xl border border-slate-300 px-4 py-3" /></label><div className="flex items-end gap-2"><button type="button" onClick={() => setCreating(false)} className="px-3 py-3 font-medium text-slate-500">{copy.cancel}</button><button disabled={create.isPending || !cardCount} className="rounded-xl bg-indigo-600 px-5 py-3 font-semibold text-white disabled:opacity-40">{create.isPending ? copy.creating : copy.generate}</button></div>{!cardCount && <p className="text-sm text-amber-700 sm:col-span-3">{copy.noCards}</p>}{error && <p role="alert" className="text-sm text-red-600 sm:col-span-3">{error}</p>}</form>}
    </div>}
    <div className="mt-6 grid gap-3 sm:grid-cols-2">{quizzes.data?.map((quiz) => <Link key={quiz.id} to={`/quizzes/${quiz.id}`} className="rounded-2xl border border-slate-200 bg-white p-5 transition hover:-translate-y-0.5 hover:border-indigo-300"><h3 className="font-semibold text-slate-950">{quiz.title}</h3><p className="mt-2 text-sm text-slate-500">{quiz.questionCount} {copy.questions}</p></Link>)}</div>
    {!quizzes.isPending && !quizzes.data?.length && <p className="mt-6 rounded-2xl border border-dashed border-slate-300 py-10 text-center text-slate-500">{copy.empty}</p>}
  </section>
}
