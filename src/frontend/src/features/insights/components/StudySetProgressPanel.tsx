import { useQuery } from '@tanstack/react-query'
import { insightKeys, insightsApi } from '@/features/insights/api/insightsApi'
import { useLanguage } from '@/i18n/LanguageProvider'

export function StudySetProgressPanel({ studySetId }: { studySetId: string }) {
  const progress = useQuery({ queryKey: insightKeys.studySet(studySetId), queryFn: () => insightsApi.studySet(studySetId) })
  const {language}=useLanguage();const vi=language==='vi'
  if (progress.isPending) return <section className="mt-8 rounded-2xl bg-slate-50 p-6 text-sm text-slate-500">{vi?'Đang tải tiến độ…':'Loading progress…'}</section>
  if (progress.isError) return <section className="mt-8 rounded-2xl bg-red-50 p-6 text-sm text-red-700">{vi?'Không thể tải tiến độ.':'Unable to load progress.'}</section>
  const value = progress.data
  const statuses = [[vi?'Mới':'New', value.newCards], [vi?'Đang học':'Learning', value.learningCards], [vi?'Đang ôn':'Reviewing', value.reviewingCards], [vi?'Đã thuộc':'Mastered', value.masteredCards]]
  return <section className="mt-8 rounded-3xl border border-slate-200 bg-white p-7"><div className="flex flex-wrap items-end justify-between gap-3"><div><p className="text-sm font-semibold uppercase tracking-[0.16em] text-indigo-600">{vi?'Tiến độ học':'Learning progress'}</p><h2 className="mt-2 text-2xl font-semibold text-slate-950">{vi?`${value.cardsReviewed}/${value.totalCards} thẻ đã ôn`:`${value.cardsReviewed} of ${value.totalCards} cards reviewed`}</h2></div><p className="text-sm text-slate-500">{vi?'Độ chính xác trắc nghiệm':'Quiz accuracy'} <strong className="text-slate-900">{value.quizAccuracy}%</strong> · {value.quizAttempts} {vi?'lượt làm':'attempts'}</p></div><div className="mt-6 grid grid-cols-2 gap-3 sm:grid-cols-4">{statuses.map(([label, count]) => <div key={label} className="rounded-xl bg-slate-50 p-4"><p className="text-xs font-medium text-slate-500">{label}</p><p className="mt-1 text-2xl font-semibold text-slate-950">{count}</p></div>)}</div>{value.weakCards > 0 && <p className="mt-5 rounded-xl bg-amber-50 px-4 py-3 text-sm font-medium text-amber-800">{vi?`${value.weakCards} thẻ yếu cần ôn thêm.`:`${value.weakCards} weak ${value.weakCards === 1 ? 'card needs' : 'cards need'} extra review.`}</p>}</section>
}
