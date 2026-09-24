import { useMemo, useState, type FormEvent } from 'react'
import { ApiRequestError } from '@/api/httpClient'
import type { FlashcardInput } from '@/features/flashcards/types/flashcard'
import { useLanguage } from '@/i18n/LanguageProvider'
import { parseQuickFlashcards } from '@/features/flashcards/utils/quickFlashcardParser'
import { FlashcardImagePicker } from '@/features/flashcards/components/FlashcardImagePicker'
import { imageFromClipboard } from '@/features/flashcards/utils/flashcardImageUtils'

export type FlashcardCreateDraft = { input: FlashcardInput; imageFile: File | null }
type Draft = FlashcardInput & { key: number; imageFile: File | null; imageError: string }
const emptyDraft = (key: number): Draft => ({ key, frontText: '', backText: '', explanation: null, languageCode: null, readingText: null, romanization: null, exampleText: null, exampleTranslation: null, memoryTip: null, acceptedAnswers: null, enableReverseRecall: false, imageFile: null, imageError: '' })

export function FlashcardEditor({ pending, onSave, onCancel }: { pending: boolean; onSave: (cards: FlashcardCreateDraft[]) => Promise<void>; onCancel: () => void }) {
  const { language } = useLanguage()
  const vi = language === 'vi'
  const [mode, setMode] = useState<'manual' | 'quick'>('manual')
  const [rows, setRows] = useState<Draft[]>([emptyDraft(1)])
  const [nextKey, setNextKey] = useState(2)
  const [quickText, setQuickText] = useState('')
  const [error, setError] = useState('')
  const parsed = useMemo(() => parseQuickFlashcards(quickText, vi), [quickText, vi])
  const labels = vi ? {
    manual: 'Từng câu', quick: 'Nhập nhanh', card: 'Thẻ', remove: 'Xóa', front: 'Câu hỏi / Mặt trước', back: 'Đáp án / Mặt sau', explanation: 'Giải thích', optional: 'không bắt buộc', language: 'Phát âm ngoại ngữ', context: 'Ngữ cảnh và cách ghi nhớ', languageCode: 'Mã ngôn ngữ', reading: 'Cách đọc', romanization: 'Phiên âm Latin', example: 'Câu ví dụ', exampleTranslation: 'Dịch câu ví dụ', memoryTip: 'Mẹo ghi nhớ', accepted: 'Đáp án khác được chấp nhận (mỗi dòng một đáp án)', add: '+ Thêm câu khác', cancel: 'Hủy', saving: 'Đang lưu…', save: 'Lưu', cards: 'thẻ', quickTitle: 'Dán tất cả câu hỏi', quickHelp: 'Mỗi dòng: câu hỏi | đáp án | giải thích | mã ngôn ngữ | cách đọc | phiên âm. Thêm ngữ cảnh bằng chế độ Từng câu.', placeholder: '食べる | to eat | động từ | ja-JP | たべる | taberu\n学校 | school | | ja-JP | がっこう | gakkou', valid: 'thẻ hợp lệ', preview: 'Xem trước', empty: 'Hãy nhập ít nhất một câu hỏi hợp lệ.', failed: 'Không thể lưu flashcard.',
  } : {
    manual: 'One by one', quick: 'Quick entry', card: 'Card', remove: 'Remove', front: 'Question / Front', back: 'Answer / Back', explanation: 'Explanation', optional: 'optional', language: 'Language pronunciation', context: 'Context and memory', languageCode: 'Language code', reading: 'Reading', romanization: 'Romanization', example: 'Example sentence', exampleTranslation: 'Example translation', memoryTip: 'Memory tip', accepted: 'Other accepted answers (one per line)', add: '+ Add another', cancel: 'Cancel', saving: 'Saving…', save: 'Save', cards: 'cards', quickTitle: 'Paste all questions', quickHelp: 'One line per card: question | answer | explanation | language code | reading | romanization. Add context in One by one mode.', placeholder: '食べる | to eat | verb | ja-JP | たべる | taberu\n学校 | school | | ja-JP | がっこう | gakkou', valid: 'valid cards', preview: 'Preview', empty: 'Enter at least one valid question.', failed: 'Unable to save flashcards.',
  }

  function change(key: number, field: keyof FlashcardInput, value: string | boolean) {
    setRows((current) => current.map((row) => row.key === key ? { ...row, [field]: value || (field === 'explanation' ? null : '') } : row))
  }
  function changeImage(key: number, file: File | null) { setRows((current) => current.map((row) => row.key === key ? { ...row, imageFile: file } : row)) }
  function changeImageError(key: number, imageError: string) { setRows((current) => current.map((row) => row.key === key ? { ...row, imageError } : row)) }
  async function submit(event: FormEvent) {
    event.preventDefault()
    setError('')
    const values: FlashcardCreateDraft[] = mode === 'manual'
      ? rows.map(({ frontText, backText, explanation, languageCode, readingText, romanization, exampleText, exampleTranslation, memoryTip, acceptedAnswers, enableReverseRecall, imageFile }) => ({ input: { frontText: frontText.trim(), backText: backText.trim(), explanation: explanation?.trim() || null, languageCode: languageCode?.trim() || null, readingText: readingText?.trim() || null, romanization: romanization?.trim() || null, exampleText: exampleText?.trim() || null, exampleTranslation: exampleTranslation?.trim() || null, memoryTip: memoryTip?.trim() || null, acceptedAnswers: acceptedAnswers?.trim() || null, enableReverseRecall }, imageFile }))
      : parsed.cards.map((input) => ({ input, imageFile: null }))
    if (!values.length || (mode === 'quick' && parsed.errors.length)) { setError(parsed.errors[0] ?? labels.empty); return }
    try { await onSave(values) } catch (reason) { setError(reason instanceof ApiRequestError ? reason.message : labels.failed) }
  }

  return <form onSubmit={submit} className="space-y-4">
    <div className="inline-flex rounded-xl bg-slate-100 p-1" role="tablist">
      <button type="button" role="tab" aria-selected={mode === 'manual'} onClick={() => { setMode('manual'); setError('') }} className={`rounded-lg px-4 py-2 text-sm font-bold ${mode === 'manual' ? 'bg-white text-blue-700 shadow-sm' : 'text-slate-600'}`}>{labels.manual}</button>
      <button type="button" role="tab" aria-selected={mode === 'quick'} onClick={() => { setMode('quick'); setError('') }} className={`rounded-lg px-4 py-2 text-sm font-bold ${mode === 'quick' ? 'bg-white text-blue-700 shadow-sm' : 'text-slate-600'}`}>{labels.quick}</button>
    </div>

    {mode === 'manual' ? <>
      {rows.map((row, index) => <fieldset key={row.key} onPaste={(event) => { const pasted = imageFromClipboard(event, vi); if (pasted.file || pasted.error) { event.preventDefault(); changeImageError(row.key, pasted.error); if (pasted.file) changeImage(row.key, pasted.file) } }} className="rounded-2xl border border-slate-200 bg-white/70 p-5 transition-shadow duration-200 focus-within:border-indigo-300 focus-within:shadow-[0_14px_35px_rgba(53,100,223,.08)]">
        <div className="mb-4 flex items-center justify-between"><legend className="font-semibold text-slate-900">{labels.card} {index + 1}</legend>{rows.length > 1 && <button type="button" onClick={() => setRows((current) => current.filter((item) => item.key !== row.key))} className="text-sm font-medium text-red-600">{labels.remove}</button>}</div>
        <div className="grid gap-4 sm:grid-cols-2"><label className="text-sm font-medium text-slate-700">{labels.front}<textarea required maxLength={1000} rows={4} value={row.frontText} onChange={(event) => change(row.key, 'frontText', event.target.value)} className="mt-2 w-full resize-none rounded-xl border border-slate-300 p-3 outline-none focus:border-indigo-500 focus:ring-4 focus:ring-indigo-100" /></label><label className="text-sm font-medium text-slate-700">{labels.back}<textarea required maxLength={5000} rows={4} value={row.backText} onChange={(event) => change(row.key, 'backText', event.target.value)} className="mt-2 w-full resize-none rounded-xl border border-slate-300 p-3 outline-none focus:border-indigo-500 focus:ring-4 focus:ring-indigo-100" /></label></div>
        <label className="mt-4 block text-sm font-medium text-slate-700">{labels.explanation} <span className="font-normal text-slate-400">({labels.optional})</span><textarea maxLength={5000} rows={2} value={row.explanation ?? ''} onChange={(event) => change(row.key, 'explanation', event.target.value)} className="mt-2 w-full resize-none rounded-xl border border-slate-300 p-3 outline-none focus:border-indigo-500 focus:ring-4 focus:ring-indigo-100" /></label>
        <FlashcardImagePicker file={row.imageFile} onChange={(file) => changeImage(row.key, file)} error={row.imageError} setError={(value) => changeImageError(row.key, value)} vi={vi} />
        <details className="mt-4 rounded-xl bg-blue-50 p-4"><summary className="cursor-pointer text-sm font-bold text-blue-800">🔊 {labels.language}</summary><div className="mt-4 grid gap-3 sm:grid-cols-3"><label className="text-xs font-semibold text-slate-600">{labels.languageCode}<input maxLength={35} placeholder="ja-JP" value={row.languageCode ?? ''} onChange={(event) => change(row.key, 'languageCode', event.target.value)} className="mt-1 w-full rounded-lg border border-blue-100 bg-white p-2.5" /></label><label className="text-xs font-semibold text-slate-600">{labels.reading}<input maxLength={500} placeholder="たべる" value={row.readingText ?? ''} onChange={(event) => change(row.key, 'readingText', event.target.value)} className="mt-1 w-full rounded-lg border border-blue-100 bg-white p-2.5" /></label><label className="text-xs font-semibold text-slate-600">{labels.romanization}<input maxLength={500} placeholder="taberu" value={row.romanization ?? ''} onChange={(event) => change(row.key, 'romanization', event.target.value)} className="mt-1 w-full rounded-lg border border-blue-100 bg-white p-2.5" /></label></div></details>
        <details className="mt-3 rounded-xl bg-amber-50 p-4"><summary className="cursor-pointer text-sm font-bold text-amber-900">💡 {labels.context}</summary><div className="mt-4 grid gap-3 sm:grid-cols-2"><label className="text-xs font-semibold text-slate-600">{labels.example}<textarea maxLength={2000} rows={2} value={row.exampleText ?? ''} onChange={(event) => change(row.key, 'exampleText', event.target.value)} className="mt-1 w-full rounded-lg border border-amber-100 bg-white p-2.5" /></label><label className="text-xs font-semibold text-slate-600">{labels.exampleTranslation}<textarea maxLength={2000} rows={2} value={row.exampleTranslation ?? ''} onChange={(event) => change(row.key, 'exampleTranslation', event.target.value)} className="mt-1 w-full rounded-lg border border-amber-100 bg-white p-2.5" /></label><label className="text-xs font-semibold text-slate-600">{labels.memoryTip}<textarea maxLength={2000} rows={2} value={row.memoryTip ?? ''} onChange={(event) => change(row.key, 'memoryTip', event.target.value)} className="mt-1 w-full rounded-lg border border-amber-100 bg-white p-2.5" /></label><label className="text-xs font-semibold text-slate-600">{labels.accepted}<textarea maxLength={2000} rows={2} value={row.acceptedAnswers ?? ''} onChange={(event) => change(row.key, 'acceptedAnswers', event.target.value)} className="mt-1 w-full rounded-lg border border-amber-100 bg-white p-2.5" /></label></div></details>
      </fieldset>)}
      <button type="button" disabled={rows.length >= 100} onClick={() => { setRows((current) => [...current, emptyDraft(nextKey)]); setNextKey((value) => value + 1) }} className="rounded-xl border border-slate-300 px-4 py-2.5 font-medium hover:bg-slate-50 disabled:opacity-50">{labels.add}</button>
    </> : <section className="rounded-2xl border border-blue-100 bg-blue-50/60 p-5">
      <h3 className="font-bold text-slate-900">{labels.quickTitle}</h3><p className="mt-1 text-sm leading-6 text-slate-600">{labels.quickHelp}</p>
      <textarea autoFocus value={quickText} onChange={(event) => setQuickText(event.target.value)} rows={10} placeholder={labels.placeholder} className="mt-4 w-full rounded-xl border border-slate-300 bg-white p-4 font-mono text-sm leading-6 outline-none focus:border-blue-500 focus:ring-4 focus:ring-blue-100" />
      <div className="mt-3 flex items-center justify-between text-sm"><span className="font-semibold text-blue-700">{parsed.cards.length}/100 {labels.valid}</span>{parsed.errors.length > 0 && <span className="text-red-700">{parsed.errors[0]}</span>}</div>
      {parsed.cards.length > 0 && <div className="mt-5 overflow-hidden rounded-xl border border-slate-200 bg-white"><p className="border-b border-slate-200 px-4 py-3 text-xs font-bold uppercase tracking-wider text-slate-500">{labels.preview}</p>{parsed.cards.slice(0, 5).map((card, index) => <div key={index} className="grid gap-1 border-b border-slate-100 px-4 py-3 text-sm last:border-0 sm:grid-cols-2"><strong>{card.frontText}</strong><span className="text-slate-600">{card.backText}</span></div>)}</div>}
    </section>}

    {error && <p role="alert" className="rounded-xl bg-red-50 p-3 text-sm text-red-700">{error}</p>}
    <div className="sticky bottom-3 z-20 flex justify-end gap-3 rounded-2xl border border-slate-200 bg-white/90 p-3 shadow-[0_14px_35px_rgba(38,56,112,.12)] backdrop-blur-xl"><button type="button" onClick={onCancel} className="rounded-xl px-4 py-2.5 font-medium text-slate-600 hover:bg-slate-100">{labels.cancel}</button><button disabled={pending || (mode === 'quick' && (!parsed.cards.length || parsed.errors.length > 0))} className="sf-primary rounded-xl px-5 py-2.5 font-semibold text-white disabled:opacity-60">{pending ? labels.saving : `${labels.save} ${mode === 'manual' ? rows.length : parsed.cards.length} ${labels.cards}`}</button></div>
  </form>
}
