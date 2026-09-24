import { useState } from 'react'
import { speakText } from '@/features/flashcards/utils/speech'
import { useLanguage } from '@/i18n/LanguageProvider'

export function PronunciationControls({ text, languageCode }: { text: string; languageCode: string }) {
  const { language } = useLanguage(); const vi = language === 'vi'; const [unsupported, setUnsupported] = useState(false)
  function speak(speed: 'slow' | 'normal') { if (!speakText(text, languageCode, speed)) setUnsupported(true) }
  return <div className="flex flex-wrap items-center justify-center gap-2"><button type="button" onClick={(event) => { event.stopPropagation(); speak('normal') }} className="rounded-xl bg-blue-50 px-4 py-2 text-sm font-bold text-blue-700">🔊 {vi ? 'Nghe' : 'Listen'}</button><button type="button" onClick={(event) => { event.stopPropagation(); speak('slow') }} className="rounded-xl bg-amber-50 px-4 py-2 text-sm font-bold text-amber-700">🐢 {vi ? 'Chậm' : 'Slow'}</button>{unsupported && <span className="w-full text-center text-xs text-red-600">{vi ? 'Trình duyệt này không hỗ trợ đọc văn bản.' : 'Text-to-speech is not supported by this browser.'}</span>}</div>
}
