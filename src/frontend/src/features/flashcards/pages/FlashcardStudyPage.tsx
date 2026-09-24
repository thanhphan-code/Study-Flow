import { useEffect, useEffectEvent, useMemo, useRef, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { flashcardKeys, flashcardsApi } from '@/features/flashcards/api/flashcardsApi'
import type { Flashcard } from '@/features/flashcards/types/flashcard'
import { studySetKeys, studySetsApi } from '@/features/studySets/api/studySetsApi'
import { RatingButtons } from '@/features/progress/components/RatingButtons'
import { reviewKeys, reviewsApi } from '@/features/progress/api/reviewsApi'
import { studySessionsApi } from '@/features/studySessions/api/studySessionsApi'
import type { ReviewRating } from '@/features/progress/types/progress'
import { useLanguage } from '@/i18n/LanguageProvider'
import { PronunciationControls } from '@/features/flashcards/components/PronunciationControls'
import { speakText, type SpeechSpeed } from '@/features/flashcards/utils/speech'
import { FlashcardImage } from '@/features/flashcards/components/FlashcardImage'
import { SourceViewer } from '@/features/grounding/components/SourceViewer'
import { LearningSessionShell, SessionHeader, SessionProgress } from '@/features/learning/components/LearningSessionShell'

export function FlashcardStudyPage() {
  const { id = '' } = useParams(); const { language, t } = useLanguage(); const vi = language === 'vi'
  const [index, setIndex] = useState(0); const [readingRevealed, setReadingRevealed] = useState(false); const [revealed, setRevealed] = useState(false); const [mode, setMode] = useState<'browse' | 'review'>('browse'); const [backFirst, setBackFirst] = useState(false); const [order, setOrder] = useState<string[] | null>(null)
  const [autoSpeak, setAutoSpeak] = useState(() => localStorage.getItem('studyflow-auto-speak') === 'true')
  const [speechSpeed, setSpeechSpeed] = useState<SpeechSpeed>(() => localStorage.getItem('studyflow-speech-speed') === 'slow' ? 'slow' : 'normal')
  const [showRomanization, setShowRomanization] = useState(() => localStorage.getItem('studyflow-show-romanization') !== 'false')
  const sessionId = useRef<string | null>(null); const queryClient = useQueryClient(); const navigate = useNavigate()
  const studySet = useQuery({ queryKey: studySetKeys.detail(id), queryFn: () => studySetsApi.get(id), enabled: Boolean(id) })
  const cards = useQuery({ queryKey: flashcardKeys.byStudySet(id), queryFn: () => flashcardsApi.list(id), enabled: Boolean(id) })
  const shownCards = useMemo(() => (order ?? cards.data?.map((item) => item.id) ?? []).map((cardId) => cards.data?.find((item) => item.id === cardId)).filter((item): item is Flashcard => Boolean(item)), [cards.data, order])
  const card = shownCards[index]
  const copy = vi ? { browse: 'Học lướt', review: 'Ôn ghi nhớ', browseHelp: 'Lật và chuyển thẻ tự do, không ghi kết quả.', reviewHelp: 'Tự nhớ trước khi lật, sau đó đánh giá mức độ nhớ.', shuffle: 'Trộn thẻ', frontFirst: 'Câu hỏi trước', backFirst: 'Đáp án trước', next: 'Tiếp theo', flip: 'Lật thẻ', showReading: 'Hiện cách đọc', revealMeaning: 'Hiện nghĩa', hideAnswer: 'Ẩn đáp án', autoSpeak: 'Tự động phát âm', speed: 'Tốc độ', normal: 'Bình thường', slow: 'Chậm', romanization: 'Hiện phiên âm', hint: 'Phím tắt: Space để tiếp tục · ← → để chuyển · 1–4 để đánh giá', saveError: 'Không thể lưu kết quả. Vui lòng thử lại.', sideFront: 'Câu hỏi', sideBack: 'Đáp án' } : { browse: 'Browse', review: 'Memory review', browseHelp: 'Flip and move freely without recording progress.', reviewHelp: 'Recall first, reveal, then rate your memory.', shuffle: 'Shuffle', frontFirst: 'Question first', backFirst: 'Answer first', next: 'Next', flip: 'Flip card', showReading: 'Show reading', revealMeaning: 'Reveal meaning', hideAnswer: 'Hide answer', autoSpeak: 'Auto pronunciation', speed: 'Speed', normal: 'Normal', slow: 'Slow', romanization: 'Show romanization', hint: 'Shortcuts: Space to continue · ← → to move · 1–4 to rate', saveError: 'Could not save this review. Please try again.', sideFront: 'Question', sideBack: 'Answer' }

  function resetReveal() { setReadingRevealed(false); setRevealed(false) }
  function move(offset: number) { if (!shownCards.length) return; setIndex((current) => (current + offset + shownCards.length) % shownCards.length); resetReveal() }
  const review = useMutation({ mutationFn: async ({ cardId, rating }: { cardId: string; rating: ReviewRating }) => { if (!sessionId.current) sessionId.current = (await studySessionsApi.start(id, 'Flashcard')).id; return reviewsApi.review(cardId, rating, sessionId.current) }, onSuccess: () => { queryClient.invalidateQueries({ queryKey: reviewKeys.due }); move(1) } })
  const moveFromKeyboard = useEffectEvent((offset: number) => move(offset))
  const advanceFromKeyboard = useEffectEvent(() => advanceReveal())
  const rateFromKeyboard = useEffectEvent((rating: ReviewRating) => { if (card) review.mutate({ cardId: card.id, rating }) })
  useEffect(() => {
    function onKeyDown(event: KeyboardEvent) {
      if (event.target instanceof HTMLInputElement || event.target instanceof HTMLTextAreaElement || !card) return
      if (event.code === 'Space') { event.preventDefault(); advanceFromKeyboard() }
      if (event.key === 'ArrowLeft') moveFromKeyboard(-1)
      if (event.key === 'ArrowRight') moveFromKeyboard(1)
      const ratings: Record<string, ReviewRating> = { '1': 'Again', '2': 'Hard', '3': 'Good', '4': 'Easy' }
      if (mode === 'review' && revealed && ratings[event.key] && !review.isPending) rateFromKeyboard(ratings[event.key])
    }
    window.addEventListener('keydown', onKeyDown); return () => window.removeEventListener('keydown', onKeyDown)
  }, [card, mode, revealed, review.isPending])
  useEffect(() => {
    if (autoSpeak && card?.languageCode) speakText(card.readingText || card.frontText, card.languageCode, speechSpeed)
  }, [autoSpeak, card?.frontText, card?.id, card?.languageCode, card?.readingText, speechSpeed])
  async function changeMode(next: 'browse' | 'review') { if (mode === 'review' && sessionId.current) { await studySessionsApi.complete(sessionId.current); sessionId.current = null }; setMode(next); resetReveal() }
  function shuffle() { const ids = (cards.data ?? []).map((item) => item.id); for (let current = ids.length - 1; current > 0; current -= 1) { const target = Math.floor(Math.random() * (current + 1)); [ids[current], ids[target]] = [ids[target], ids[current]] } setOrder(ids); setIndex(0); resetReveal() }
  async function exitStudy() { if (sessionId.current) await studySessionsApi.complete(sessionId.current); queryClient.invalidateQueries({ queryKey: ['dashboard'] }); navigate(`/study-sets/${id}`) }

  if (studySet.isPending || cards.isPending) return <main className="grid min-h-screen place-items-center text-slate-500">{t('preparing')}</main>
  if (studySet.isError || cards.isError) return <main className="grid min-h-screen place-items-center text-red-700">{t('unableOpen')}</main>
  if (!cards.data?.length) return <main className="grid min-h-screen place-items-center text-center"><div><h1 className="text-2xl font-semibold">{t('noCards')}</h1><Link to={`/study-sets/${id}`} className="mt-4 inline-block text-indigo-600">{t('addCards')}</Link></div></main>
  if (!card) return null
  const isLanguageCard = Boolean(card.languageCode)
  const hasReadingStep = Boolean(isLanguageCard && card.readingText && !backFirst)
  function advanceReveal() { if (hasReadingStep && !readingRevealed && !revealed) setReadingRevealed(true); else if (revealed) resetReveal(); else setRevealed(true) }
  function updateAutoSpeak(value: boolean) { setAutoSpeak(value); localStorage.setItem('studyflow-auto-speak', String(value)) }
  function updateSpeed(value: SpeechSpeed) { setSpeechSpeed(value); localStorage.setItem('studyflow-speech-speed', value) }
  function updateRomanization(value: boolean) { setShowRomanization(value); localStorage.setItem('studyflow-show-romanization', String(value)) }
  const showBack = backFirst ? !revealed : revealed; const mainText = showBack ? card.backText : card.frontText; const sideLabel = showBack ? copy.sideBack : copy.sideFront
  return <LearningSessionShell width="max-w-4xl">
    <SessionHeader exitLabel={t('exitStudy')} onExit={()=>void exitStudy()} current={index+1} total={shownCards.length}/>
    <section className="mt-5 rounded-2xl bg-white/75 p-3 shadow-sm backdrop-blur sm:flex sm:items-center sm:justify-between"><div className="flex rounded-xl bg-slate-100 p-1"><button onClick={() => changeMode('browse')} className={`flex-1 rounded-lg px-4 py-2 text-sm font-bold ${mode === 'browse' ? 'bg-white text-blue-700 shadow-sm' : 'text-slate-500'}`}>{copy.browse}</button><button onClick={() => changeMode('review')} className={`flex-1 rounded-lg px-4 py-2 text-sm font-bold ${mode === 'review' ? 'bg-white text-blue-700 shadow-sm' : 'text-slate-500'}`}>{copy.review}</button></div><p className="mt-2 px-2 text-xs text-slate-500 sm:mt-0">{mode === 'browse' ? copy.browseHelp : copy.reviewHelp}</p></section>
    <div className="mt-6 flex items-end justify-between gap-4"><div className="min-w-0"><h1 className="truncate text-xl font-bold text-[#111943]">{studySet.data?.title}</h1><div className="mt-2 flex flex-wrap gap-2"><button onClick={shuffle} className="rounded-lg bg-white px-3 py-1.5 text-xs font-semibold text-slate-600 shadow-sm">↝ {copy.shuffle}</button><button onClick={() => { setBackFirst((value) => !value); resetReveal() }} className="rounded-lg bg-white px-3 py-1.5 text-xs font-semibold text-slate-600 shadow-sm">⇄ {backFirst ? copy.backFirst : copy.frontFirst}</button></div></div><p className="metric-number shrink-0 text-sm font-semibold text-slate-500">{index + 1} / {shownCards.length}</p></div>
    {isLanguageCard && <section className="mt-3 flex flex-wrap items-center gap-x-5 gap-y-2 rounded-xl bg-white/70 px-4 py-3 text-xs text-slate-600"><label className="flex cursor-pointer items-center gap-2"><input type="checkbox" checked={autoSpeak} onChange={(event) => updateAutoSpeak(event.target.checked)} />{copy.autoSpeak}</label><label className="flex items-center gap-2">{copy.speed}<select value={speechSpeed} onChange={(event) => updateSpeed(event.target.value as SpeechSpeed)} className="rounded-lg border border-slate-200 bg-white px-2 py-1"><option value="normal">{copy.normal}</option><option value="slow">{copy.slow}</option></select></label>{card.romanization && <label className="flex cursor-pointer items-center gap-2"><input type="checkbox" checked={showRomanization} onChange={(event) => updateRomanization(event.target.checked)} />{copy.romanization}</label>}</section>}
    <div className="mt-4"><SessionProgress current={index+1} total={shownCards.length} label={vi?'Tiến độ phiên học':'Study session progress'}/></div>
    {card.imageUrl && !showBack && <FlashcardImage imageUrl={card.imageUrl} alt={card.frontText} className="mt-5 max-h-64 w-full rounded-2xl bg-white object-contain p-2 shadow-sm"/>}
    <section className="sf-card mt-5 flex min-h-[22rem] w-full flex-col items-center justify-center rounded-[2rem] p-7 text-center sm:min-h-[26rem] sm:p-12"><span className={`rounded-full px-3 py-1 text-xs font-bold ${showBack ? 'bg-emerald-100 text-emerald-700' : 'bg-blue-100 text-blue-700'}`}>{sideLabel}</span><span className="mt-7 whitespace-pre-wrap text-2xl font-bold leading-10 tracking-tight text-[#111943] sm:text-3xl">{mainText}</span>{!showBack && readingRevealed && card.readingText && <span className="mt-4 text-xl font-semibold text-blue-700">{card.readingText}</span>}{!showBack && readingRevealed && showRomanization && card.romanization && <span className="mt-2 text-sm text-slate-500">{card.romanization}</span>}{isLanguageCard && !showBack && <span className="mt-6"><PronunciationControls text={card.readingText || card.frontText} languageCode={card.languageCode!} /></span>}{showBack && card.exampleText && <span className="mt-7 rounded-xl bg-blue-50 px-5 py-4 text-left text-base text-blue-950"><strong>{card.exampleText}</strong>{card.exampleTranslation && <span className="mt-1 block text-sm text-blue-700">{card.exampleTranslation}</span>}</span>}{showBack && card.memoryTip && <span className="mt-4 text-sm text-amber-800">💡 {card.memoryTip}</span>}{showBack && card.explanation && <span className="mt-6 border-t border-slate-100 pt-5 text-base leading-7 text-slate-500">{card.explanation}</span>}</section>
    {showBack && <div className="mt-3 flex justify-center"><SourceViewer target={{type:'flashcard',id:card.id}} compact/></div>}
    <div className="mt-5">{mode === 'review' && revealed ? <RatingButtons pending={review.isPending} onRate={(rating) => review.mutate({ cardId: card.id, rating })} /> : <div className="grid grid-cols-[auto_1fr_auto] gap-3"><button onClick={() => move(-1)} className="rounded-xl bg-white px-5 py-3 font-semibold text-slate-600 shadow-sm">← <span className="hidden sm:inline">{t('previous')}</span></button><button onClick={advanceReveal} className="sf-primary rounded-xl px-5 py-3 font-bold text-white">{revealed ? copy.hideAnswer : hasReadingStep && !readingRevealed ? copy.showReading : isLanguageCard && !backFirst ? copy.revealMeaning : copy.flip}</button><button onClick={() => move(1)} className="rounded-xl bg-white px-5 py-3 font-semibold text-slate-600 shadow-sm"><span className="hidden sm:inline">{copy.next}</span> →</button></div>}</div>
    <p className="mt-4 text-center text-xs text-slate-400">{copy.hint}</p>{review.isError && <p role="alert" className="mt-4 text-center text-sm text-red-600">{copy.saveError}</p>}
  </LearningSessionShell>
}
