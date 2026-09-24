import { useState, type FormEvent } from 'react'
import { ApiRequestError } from '@/api/httpClient'
import type { StudySetInput } from '@/features/studySets/types/studySet'
import { useLanguage } from '@/i18n/LanguageProvider'

export function StudySetForm({ initial, pending, onSubmit, onCancel }: { initial?: StudySetInput; pending: boolean; onSubmit: (input: StudySetInput) => Promise<void>; onCancel: () => void }) {
  const [error, setError] = useState('')
  const { language } = useLanguage(); const vi = language === 'vi'; const copy = vi ? { title: 'Tên bộ học', example: 'Ví dụ: Chương 3 — Lực', description: 'Mô tả', optional: 'không bắt buộc', cancel: 'Hủy', saving: 'Đang lưu…', save: 'Lưu bộ học', failed: 'Không thể lưu bộ học.' } : { title: 'Title', example: 'e.g. Chapter 3 — Forces', description: 'Description', optional: 'optional', cancel: 'Cancel', saving: 'Saving…', save: 'Save study set', failed: 'Unable to save this study set.' }
  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); setError(''); const data = new FormData(event.currentTarget)
    try { await onSubmit({ title: String(data.get('title')).trim(), description: String(data.get('description')).trim() || null }) }
    catch (reason) { setError(reason instanceof ApiRequestError ? reason.message : copy.failed) }
  }
  return <form onSubmit={submit} className="space-y-5"><label className="block text-sm font-medium text-slate-700">{copy.title}<input autoFocus required maxLength={150} name="title" defaultValue={initial?.title} placeholder={copy.example} className="mt-2 w-full rounded-xl border border-slate-300 px-4 py-3 outline-none focus:border-indigo-500 focus:ring-4 focus:ring-indigo-100" /></label><label className="block text-sm font-medium text-slate-700">{copy.description} <span className="font-normal text-slate-400">({copy.optional})</span><textarea name="description" rows={4} maxLength={2000} defaultValue={initial?.description ?? ''} className="mt-2 w-full resize-none rounded-xl border border-slate-300 px-4 py-3 outline-none focus:border-indigo-500 focus:ring-4 focus:ring-indigo-100" /></label>{error && <p role="alert" className="rounded-xl bg-red-50 p-3 text-sm text-red-700">{error}</p>}<div className="flex justify-end gap-3"><button type="button" onClick={onCancel} className="rounded-xl px-4 py-2.5 font-medium text-slate-600 hover:bg-slate-100">{copy.cancel}</button><button disabled={pending} className="rounded-xl bg-indigo-600 px-5 py-2.5 font-semibold text-white hover:bg-indigo-700 disabled:opacity-60">{pending ? copy.saving : copy.save}</button></div></form>
}
