import { useState, type FormEvent } from 'react'
import { ApiRequestError } from '@/api/httpClient'
import type { SubjectInput } from '@/features/subjects/types/subject'
import { useLanguage } from '@/i18n/LanguageProvider'

type SubjectFormProps = {
  initial?: SubjectInput
  pending: boolean
  submitLabel: string
  onSubmit: (input: SubjectInput) => Promise<void>
  onCancel?: () => void
}

export function SubjectForm({ initial, pending, submitLabel, onSubmit, onCancel }: SubjectFormProps) {
  const [error, setError] = useState('')
  const { language } = useLanguage(); const vi = language === 'vi'; const copy = vi ? { name: 'Tên môn học', example: 'Ví dụ: Vật lý', description: 'Mô tả', optional: 'không bắt buộc', prompt: 'Bạn đang học nội dung gì?', cancel: 'Hủy', saving: 'Đang lưu…', failed: 'Không thể lưu môn học.' } : { name: 'Subject name', example: 'e.g. Physics', description: 'Description', optional: 'optional', prompt: 'What are you learning?', cancel: 'Cancel', saving: 'Saving…', failed: 'Unable to save this subject.' }
  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); setError('')
    const data = new FormData(event.currentTarget)
    try {
      await onSubmit({ name: String(data.get('name')).trim(), description: String(data.get('description')).trim() || null })
    } catch (reason) {
      setError(reason instanceof ApiRequestError ? reason.message : copy.failed)
    }
  }

  return <form className="space-y-5" onSubmit={submit}><label className="block text-sm font-medium text-slate-700">{copy.name}<input autoFocus required maxLength={100} name="name" defaultValue={initial?.name} placeholder={copy.example} className="mt-2 w-full rounded-xl border border-slate-300 px-4 py-3 outline-none focus:border-indigo-500 focus:ring-4 focus:ring-indigo-100" /></label><label className="block text-sm font-medium text-slate-700">{copy.description} <span className="font-normal text-slate-400">({copy.optional})</span><textarea maxLength={1000} rows={4} name="description" defaultValue={initial?.description ?? ''} placeholder={copy.prompt} className="mt-2 w-full resize-none rounded-xl border border-slate-300 px-4 py-3 outline-none focus:border-indigo-500 focus:ring-4 focus:ring-indigo-100" /></label>{error && <p role="alert" className="rounded-xl bg-red-50 px-4 py-3 text-sm text-red-700">{error}</p>}<div className="flex justify-end gap-3">{onCancel && <button type="button" onClick={onCancel} className="rounded-xl px-4 py-2.5 font-medium text-slate-600 hover:bg-slate-100">{copy.cancel}</button>}<button disabled={pending} className="rounded-xl bg-indigo-600 px-5 py-2.5 font-semibold text-white hover:bg-indigo-700 disabled:opacity-60">{pending ? copy.saving : submitLabel}</button></div></form>
}
