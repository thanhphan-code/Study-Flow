import { useState, type FormEvent } from 'react'
import type { ManualQuizInput, ManualQuizQuestionInput } from '@/features/quizzes/types/quiz'
import { useLanguage } from '@/i18n/LanguageProvider'

type DraftQuestion = ManualQuizQuestionInput & { key: number }
const createQuestion = (key: number): DraftQuestion => ({ key, questionText: '', explanation: null, options: [{ text: '', isCorrect: true }, { text: '', isCorrect: false }] })

export function ManualQuizEditor({ pending, onSave, onCancel }: { pending: boolean; onSave: (input: ManualQuizInput) => Promise<void>; onCancel: () => void }) {
  const { language } = useLanguage(); const vi = language === 'vi'
  const [title, setTitle] = useState('')
  const [questions, setQuestions] = useState<DraftQuestion[]>([createQuestion(1)])
  const [nextKey, setNextKey] = useState(2)
  const [error, setError] = useState('')
  const copy = vi ? { title: 'Tên bài trắc nghiệm', question: 'Câu hỏi', explanation: 'Giải thích sau khi chấm (không bắt buộc)', answer: 'Đáp án', correct: 'Đáp án đúng', addAnswer: '+ Thêm đáp án', addQuestion: '+ Thêm câu hỏi', remove: 'Xóa', cancel: 'Hủy', save: 'Tạo bài trắc nghiệm', saving: 'Đang tạo…', exact: 'Mỗi câu phải có từ 2–4 đáp án và đúng một đáp án được chọn.', failed: 'Không thể tạo bài trắc nghiệm.' } : { title: 'Quiz title', question: 'Question', explanation: 'Explanation shown after grading (optional)', answer: 'Answer', correct: 'Correct answer', addAnswer: '+ Add answer', addQuestion: '+ Add question', remove: 'Remove', cancel: 'Cancel', save: 'Create quiz', saving: 'Creating…', exact: 'Each question needs 2–4 answers with exactly one marked correct.', failed: 'Unable to create quiz.' }

  function updateQuestion(index: number, field: 'questionText' | 'explanation', value: string) { setQuestions((current) => current.map((question, position) => position === index ? { ...question, [field]: value || (field === 'explanation' ? null : '') } : question)) }
  function updateOption(questionIndex: number, optionIndex: number, value: string) { setQuestions((current) => current.map((question, position) => position !== questionIndex ? question : { ...question, options: question.options.map((option, index) => index === optionIndex ? { ...option, text: value } : option) })) }
  function markCorrect(questionIndex: number, optionIndex: number) { setQuestions((current) => current.map((question, position) => position !== questionIndex ? question : { ...question, options: question.options.map((option, index) => ({ ...option, isCorrect: index === optionIndex })) })) }
  function removeOption(questionIndex: number, optionIndex: number) { setQuestions((current) => current.map((question, position) => { if (position !== questionIndex) return question; const remaining = question.options.filter((_, index) => index !== optionIndex); return { ...question, options: remaining.some((option) => option.isCorrect) ? remaining : remaining.map((option, index) => ({ ...option, isCorrect: index === 0 })) } })) }
  async function submit(event: FormEvent) { event.preventDefault(); setError(''); try { await onSave({ title: title.trim(), questions: questions.map(({ questionText, explanation, options }) => ({ questionText: questionText.trim(), explanation: explanation?.trim() || null, options: options.map((option) => ({ ...option, text: option.text.trim() })) })) }) } catch { setError(copy.failed) } }

  return <form onSubmit={submit} className="space-y-5">
    <label className="block text-sm font-semibold text-slate-700">{copy.title}<input required maxLength={150} value={title} onChange={(event) => setTitle(event.target.value)} className="mt-2 w-full rounded-xl border border-slate-300 px-4 py-3 outline-none focus:border-blue-500 focus:ring-4 focus:ring-blue-100" /></label>
    <p className="rounded-xl bg-blue-50 px-4 py-3 text-sm text-blue-800">{copy.exact}</p>
    {questions.map((question, questionIndex) => <fieldset key={question.key} className="rounded-2xl border border-slate-200 bg-white p-5">
      <div className="flex items-center justify-between"><legend className="font-bold text-slate-900">{copy.question} {questionIndex + 1}</legend>{questions.length > 1 && <button type="button" onClick={() => setQuestions((current) => current.filter((_, index) => index !== questionIndex))} className="text-sm font-semibold text-red-600">{copy.remove}</button>}</div>
      <textarea required maxLength={2000} rows={3} value={question.questionText} onChange={(event) => updateQuestion(questionIndex, 'questionText', event.target.value)} className="mt-4 w-full rounded-xl border border-slate-300 p-3 text-base font-semibold outline-none focus:border-blue-500 focus:ring-4 focus:ring-blue-100" placeholder={copy.question} />
      <div className="mt-4 space-y-2">{question.options.map((option, optionIndex) => <div key={optionIndex} className="flex items-center gap-3"><input type="radio" name={`correct-${question.key}`} checked={option.isCorrect} onChange={() => markCorrect(questionIndex, optionIndex)} aria-label={`${copy.correct} ${optionIndex + 1}`} /><input required maxLength={1000} value={option.text} onChange={(event) => updateOption(questionIndex, optionIndex, event.target.value)} placeholder={`${copy.answer} ${optionIndex + 1}`} className="min-w-0 flex-1 rounded-xl border border-slate-300 px-4 py-2.5 outline-none focus:border-blue-500" />{question.options.length > 2 && <button type="button" onClick={() => removeOption(questionIndex, optionIndex)} className="text-sm text-red-600">{copy.remove}</button>}</div>)}</div>
      <div className="mt-3 flex items-center justify-between">{question.options.length < 4 ? <button type="button" onClick={() => setQuestions((current) => current.map((item, index) => index === questionIndex ? { ...item, options: [...item.options, { text: '', isCorrect: false }] } : item))} className="text-sm font-semibold text-blue-600">{copy.addAnswer}</button> : <span />}<span className="text-xs text-slate-400">{copy.correct}: {question.options.findIndex((option) => option.isCorrect) + 1}</span></div>
      <textarea maxLength={5000} rows={2} value={question.explanation ?? ''} onChange={(event) => updateQuestion(questionIndex, 'explanation', event.target.value)} className="mt-4 w-full rounded-xl border border-slate-200 p-3 text-sm outline-none focus:border-blue-500" placeholder={copy.explanation} />
    </fieldset>)}
    {questions.length < 50 && <button type="button" onClick={() => { setQuestions((current) => [...current, createQuestion(nextKey)]); setNextKey((value) => value + 1) }} className="rounded-xl border border-blue-200 px-4 py-2.5 font-semibold text-blue-700">{copy.addQuestion}</button>}
    {error && <p role="alert" className="rounded-xl bg-red-50 p-3 text-sm text-red-700">{error}</p>}
    <div className="flex justify-end gap-3"><button type="button" onClick={onCancel} className="px-4 py-3 font-semibold text-slate-500">{copy.cancel}</button><button disabled={pending} className="rounded-xl bg-blue-600 px-5 py-3 font-bold text-white disabled:opacity-50">{pending ? copy.saving : copy.save}</button></div>
  </form>
}
