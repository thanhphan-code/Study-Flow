import type { ClipboardEvent } from 'react'

const allowedTypes = new Set(['image/jpeg', 'image/png', 'image/webp'])
const maximumSize = 5 * 1024 * 1024

export function validateFlashcardImage(file: File, vi: boolean) {
  if (!allowedTypes.has(file.type)) return vi ? 'Chỉ hỗ trợ ảnh JPG, PNG hoặc WEBP.' : 'Only JPG, PNG, or WEBP images are supported.'
  if (file.size === 0) return vi ? 'Ảnh không được để trống.' : 'The image is empty.'
  if (file.size > maximumSize) return vi ? 'Ảnh không được vượt quá 5 MB.' : 'The image cannot exceed 5 MB.'
  return ''
}

export function imageFromClipboard(event: ClipboardEvent, vi: boolean) {
  const item = Array.from(event.clipboardData.items).find((value) => value.kind === 'file' && value.type.startsWith('image/'))
  const source = item?.getAsFile()
  if (!source) return { file: null, error: '' }
  const extension = source.type === 'image/jpeg' ? 'jpg' : source.type === 'image/png' ? 'png' : source.type === 'image/webp' ? 'webp' : 'image'
  const file = new File([source], source.name || `pasted-image-${Date.now()}.${extension}`, { type: source.type })
  const error = validateFlashcardImage(file, vi)
  return { file: error ? null : file, error }
}
