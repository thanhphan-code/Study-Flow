import type { ReviewRating } from '@/features/progress/types/progress'
import { useLanguage } from '@/i18n/LanguageProvider'

const ratings: { value: ReviewRating; hint: string; style: string }[] = [
  { value: 'Again', hint: '10 min', style: 'border-red-200 text-red-700 hover:bg-red-50' },
  { value: 'Hard', hint: '1+ day', style: 'border-orange-200 text-orange-700 hover:bg-orange-50' },
  { value: 'Good', hint: '3+ days', style: 'border-emerald-200 text-emerald-700 hover:bg-emerald-50' },
  { value: 'Easy', hint: '7+ days', style: 'border-indigo-200 text-indigo-700 hover:bg-indigo-50' },
]

export function RatingButtons({ pending, onRate }: { pending: boolean; onRate: (rating: ReviewRating) => void }) {
  const { t } = useLanguage(); const labels = { Again: t('again'), Hard: t('hard'), Good: t('good'), Easy: t('easy') }
  return <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">{ratings.map((item, index) => <button key={item.value} disabled={pending} onClick={() => onRate(item.value)} className={`rounded-2xl border bg-white px-3 py-4 font-semibold shadow-sm transition hover:-translate-y-0.5 active:scale-[.98] disabled:opacity-50 ${item.style}`}><span className="mb-2 block text-xl">{item.value === 'Again' ? '☹' : item.value === 'Hard' ? '◔' : item.value === 'Good' ? '☺' : '☻'}</span><span className="block">{index + 1}. {labels[item.value]}</span><span className="mt-1 block text-xs font-normal opacity-70">{item.hint}</span></button>)}</div>
}
