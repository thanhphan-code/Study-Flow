export type SpeechSpeed = 'slow' | 'normal'

export function canSpeak() { return typeof window !== 'undefined' && 'speechSynthesis' in window && 'SpeechSynthesisUtterance' in window }

export function speakText(text: string, languageCode: string, speed: SpeechSpeed = 'normal') {
  if (!canSpeak()) return false
  window.speechSynthesis.cancel()
  const utterance = new SpeechSynthesisUtterance(text)
  utterance.lang = languageCode
  utterance.rate = speed === 'slow' ? 0.65 : 0.9
  utterance.pitch = 1
  const normalized = languageCode.toLowerCase()
  const voice = window.speechSynthesis.getVoices().find((item) => item.lang.toLowerCase() === normalized)
    ?? window.speechSynthesis.getVoices().find((item) => item.lang.toLowerCase().startsWith(normalized.split('-')[0]))
  if (voice) utterance.voice = voice
  window.speechSynthesis.speak(utterance)
  return true
}
