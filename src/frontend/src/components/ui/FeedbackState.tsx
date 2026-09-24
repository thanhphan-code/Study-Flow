import type { ReactNode } from 'react'
import { AlertCircle, Inbox, RefreshCw } from 'lucide-react'

export function PageSkeleton() {
  return <main className="mx-auto max-w-6xl px-5 py-8" aria-busy="true" aria-label="Loading"><div className="skeleton h-44 rounded-3xl"/><div className="mt-5 grid grid-cols-2 gap-3 sm:grid-cols-4">{[1,2,3,4].map(value=><div key={value} className="skeleton h-24 rounded-2xl"/>)}</div></main>
}

export function EmptyState({ title, description, action, icon }: { title: string; description: string; action?: ReactNode; icon?: ReactNode }) {
  return <section className="empty-state"><span className="empty-state-icon" aria-hidden>{icon??<Inbox size={24}/>}</span><h2>{title}</h2><p>{description}</p>{action&&<div className="mt-5">{action}</div>}</section>
}

export function ErrorState({ title, description, retryLabel, onRetry }: { title: string; description: string; retryLabel?: string; onRetry?: () => void }) {
  return <section className="error-state" role="alert"><AlertCircle aria-hidden size={22}/><div><h2>{title}</h2><p>{description}</p>{onRetry&&<button type="button" onClick={onRetry} className="mt-4 inline-flex min-h-11 items-center gap-2 rounded-xl bg-red-700 px-4 py-2 text-sm font-semibold text-white"><RefreshCw aria-hidden size={16}/>{retryLabel}</button>}</div></section>
}
