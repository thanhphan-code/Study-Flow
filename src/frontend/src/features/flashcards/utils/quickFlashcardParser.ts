import type { FlashcardInput } from '@/features/flashcards/types/flashcard'

export type QuickImportResult = { cards: FlashcardInput[]; errors: string[] }

export function parseQuickFlashcards(value: string, vietnamese = false): QuickImportResult {
  const cards: FlashcardInput[] = []
  const errors: string[] = []
  const lines = value.split(/\r?\n/)
  for (let index = 0; index < lines.length; index += 1) {
    const line = lines[index].trim()
    if (!line) continue
    const columns = line.includes('\t') ? line.split('\t') : line.split(/\s+\|\s+/)
    const frontText = columns[0]?.trim() ?? ''
    const backText = columns[1]?.trim() ?? ''
    const explanation = columns[2]?.trim() || null
    const languageCode = columns[3]?.trim() || null
    const readingText = columns[4]?.trim() || null
    const romanization = columns.slice(5).join(' | ').trim() || null
    if (columns.length < 2 || !frontText || !backText) {
      errors.push(vietnamese ? `Dòng ${index + 1}: cần có câu hỏi và đáp án.` : `Line ${index + 1}: question and answer are required.`)
      continue
    }
    if (frontText.length > 1000 || backText.length > 5000 || (explanation?.length ?? 0) > 5000) {
      errors.push(vietnamese ? `Dòng ${index + 1}: nội dung vượt giới hạn ký tự.` : `Line ${index + 1}: content exceeds the character limit.`)
      continue
    }
    if (languageCode && !/^[A-Za-z]{2,3}(?:-[A-Za-z0-9]{2,8})*$/.test(languageCode)) {
      errors.push(vietnamese ? `Dòng ${index + 1}: mã ngôn ngữ không hợp lệ.` : `Line ${index + 1}: invalid language code.`)
      continue
    }
    cards.push({ frontText, backText, explanation, languageCode, readingText, romanization })
  }
  if (cards.length > 100) errors.push(vietnamese ? 'Chỉ được nhập tối đa 100 thẻ mỗi lần.' : 'You can import at most 100 cards at a time.')
  return { cards: cards.slice(0, 100), errors }
}
