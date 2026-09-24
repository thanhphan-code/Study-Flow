import { useEffect, useMemo, useRef, type DragEvent } from 'react'
import { validateFlashcardImage } from '@/features/flashcards/utils/flashcardImageUtils'

export function FlashcardImagePicker({ file, onChange, error, setError, vi }: { file: File | null; onChange: (file: File | null) => void; error: string; setError: (value: string) => void; vi: boolean }) {
  const input = useRef<HTMLInputElement>(null)
  const preview = useMemo(() => file ? URL.createObjectURL(file) : null, [file])

  useEffect(() => {
    return () => { if (preview) URL.revokeObjectURL(preview) }
  }, [preview])

  function accept(candidate: File | null) {
    if (!candidate) return
    const validationError = validateFlashcardImage(candidate, vi)
    setError(validationError)
    if (!validationError) onChange(candidate)
  }

  function drop(event: DragEvent<HTMLDivElement>) {
    event.preventDefault()
    accept(event.dataTransfer.files[0] ?? null)
  }

  return <div className="mt-4">
    <p className="text-sm font-medium text-slate-700">{vi ? 'Hình ảnh' : 'Image'} <span className="font-normal text-slate-400">({vi ? 'không bắt buộc' : 'optional'})</span></p>
    <div
      role="button"
      tabIndex={0}
      onClick={() => input.current?.click()}
      onKeyDown={(event) => { if (event.key === 'Enter' || event.key === ' ') input.current?.click() }}
      onDragOver={(event) => event.preventDefault()}
      onDrop={drop}
      className="mt-2 cursor-pointer rounded-xl border-2 border-dashed border-indigo-200 bg-indigo-50/50 p-4 outline-none transition hover:border-indigo-400 focus:ring-4 focus:ring-indigo-100"
    >
      <input ref={input} type="file" accept="image/jpeg,image/png,image/webp" className="sr-only" onChange={(event) => { accept(event.target.files?.[0] ?? null); event.currentTarget.value = '' }} />
      {preview ? <div className="flex flex-col gap-4 sm:flex-row sm:items-center">
        <img src={preview} alt={vi ? 'Ảnh xem trước' : 'Image preview'} className="max-h-44 max-w-full rounded-lg bg-white object-contain sm:w-56" />
        <div><p className="font-semibold text-slate-800">{file?.name}</p><p className="mt-1 text-sm text-slate-500">{vi ? 'Nhấp để thay ảnh hoặc dán một ảnh khác.' : 'Click to replace or paste another image.'}</p><button type="button" onClick={(event) => { event.stopPropagation(); onChange(null); setError('') }} className="mt-3 text-sm font-bold text-red-600">{vi ? 'Xóa ảnh' : 'Remove image'}</button></div>
      </div> : <div className="py-3 text-center"><p className="font-bold text-indigo-700">🖼 {vi ? 'Chọn, kéo thả hoặc dán ảnh' : 'Choose, drop, or paste an image'}</p><p className="mt-1 text-xs text-slate-500">JPG, PNG, WEBP · {vi ? 'tối đa' : 'maximum'} 5 MB · Ctrl+V</p></div>}
    </div>
    {error && <p className="mt-2 text-sm text-red-700">{error}</p>}
  </div>
}
