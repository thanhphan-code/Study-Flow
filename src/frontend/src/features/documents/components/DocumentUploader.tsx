import { useRef, useState } from 'react'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { documentKeys, documentsApi } from '@/features/documents/api/documentsApi'
import { useLanguage } from '@/i18n/LanguageProvider'

export function DocumentUploader({ studySetId }: { studySetId?: string }) {
  const [file, setFile] = useState<File | null>(null); const input = useRef<HTMLInputElement>(null); const queryClient = useQueryClient()
  const {language,t}=useLanguage();const vi=language==='vi'
  const upload = useMutation({ mutationFn: () => documentsApi.upload(file!, studySetId), onSuccess: () => { setFile(null); if (input.current) input.current.value = ''; queryClient.invalidateQueries({ queryKey: documentKeys.all }) } })
  return <div className="rounded-2xl border border-dashed border-slate-300 bg-slate-50 p-5"><label className="block text-sm font-semibold text-slate-800" htmlFor={`document-${studySetId ?? 'global'}`}>{t('importDocument')}</label><p className="mt-1 text-xs text-slate-500">PDF, DOCX, PPTX {vi?'hoặc':'or'} UTF-8 TXT · {vi?'tối đa':'maximum'} 20 MB</p><div className="mt-4 flex flex-wrap items-center gap-3"><input ref={input} id={`document-${studySetId ?? 'global'}`} type="file" accept=".pdf,.docx,.pptx,.txt,application/pdf,text/plain" onChange={event => setFile(event.target.files?.[0] ?? null)} className="max-w-full text-sm text-slate-600 file:mr-3 file:rounded-lg file:border-0 file:bg-white file:px-3 file:py-2 file:font-semibold"/><button disabled={!file || upload.isPending} onClick={() => upload.mutate()} className="rounded-xl bg-indigo-600 px-4 py-2.5 text-sm font-semibold text-white disabled:opacity-50">{upload.isPending ? (vi?'Đang tải lên…':'Uploading…') : t('upload')}</button></div>{upload.isError && <p role="alert" className="mt-3 text-sm text-red-700">{upload.error.message}</p>}{upload.isSuccess && <p className="mt-3 text-sm text-amber-700">{vi?'Đã tải lên. Tài liệu đang được xử lý nền.':'Uploaded. The document is being processed in the background.'}</p>}</div>
}
