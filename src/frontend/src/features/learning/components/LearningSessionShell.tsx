import type { ReactNode } from 'react'
import { ChevronLeft } from 'lucide-react'
import { Link } from 'react-router-dom'
import { LanguageSwitch } from '@/components/AppShell'

export function LearningSessionShell({ children, width = 'max-w-3xl' }: { children: ReactNode; width?: 'max-w-3xl' | 'max-w-4xl' }) {
  return <main className={`learning-session mx-auto min-h-dvh ${width} px-4 pb-[calc(1.5rem+env(safe-area-inset-bottom))] pt-4 sm:px-6 sm:py-8`}>{children}</main>
}

export function SessionHeader({ exitLabel, exitTo, onExit, current, total, children }: { exitLabel: string; exitTo?: string; onExit?: () => void; current?: number; total?: number; children?: ReactNode }) {
  const exitContent = <><ChevronLeft aria-hidden size={18}/><span>{exitLabel}</span></>
  return <header className="session-header">
    {exitTo ? <Link to={exitTo} className="session-exit">{exitContent}</Link> : <button type="button" onClick={onExit} className="session-exit">{exitContent}</button>}
    <div className="flex items-center gap-3">{children}{current !== undefined && total !== undefined && <span className="metric-number min-w-14 text-right text-sm font-semibold text-slate-500" aria-label={`${current} / ${total}`}>{current} / {total}</span>}<LanguageSwitch/></div>
  </header>
}

export function SessionProgress({ current, total, label }: { current: number; total: number; label?: string }) {
  const percent = total > 0 ? Math.min(100, Math.max(0, current * 100 / total)) : 0
  return <div className="session-progress" aria-label={label} role="progressbar" aria-valuemin={0} aria-valuemax={total} aria-valuenow={current}>
    <span style={{ transform: `scaleX(${percent / 100})` }} />
  </div>
}

export function SessionSummary({ eyebrow, title, description, metrics, primaryAction, secondaryAction }: { eyebrow: string; title: string; description?: string; metrics?: Array<{ label: string; value: ReactNode }>; primaryAction: ReactNode; secondaryAction?: ReactNode }) {
  return <LearningSessionShell><section className="session-summary sf-card">
    <div className="session-summary-mark" aria-hidden>✓</div><p className="mt-6 text-sm font-semibold text-emerald-700">{eyebrow}</p><h1 className="mt-2 text-3xl font-semibold tracking-tight text-slate-950 sm:text-4xl">{title}</h1>{description&&<p className="mx-auto mt-3 max-w-md leading-7 text-slate-500">{description}</p>}
    {metrics?.length?<dl className="mt-7 grid grid-cols-2 gap-3 text-left">{metrics.map(metric=><div key={metric.label} className="rounded-2xl bg-slate-50 p-4"><dt className="text-xs font-medium text-slate-500">{metric.label}</dt><dd className="metric-number mt-1 text-2xl font-semibold text-slate-950">{metric.value}</dd></div>)}</dl>:null}
    <div className="mt-7 grid gap-3">{primaryAction}{secondaryAction}</div>
  </section></LearningSessionShell>
}

