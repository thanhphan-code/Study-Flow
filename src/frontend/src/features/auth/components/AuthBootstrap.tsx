import { useEffect, useRef, type PropsWithChildren } from 'react'
import { authApi } from '@/features/auth/api/authApi'
import { useAuthStore } from '@/features/auth/store/authStore'

export function AuthBootstrap({ children }: PropsWithChildren) {
  const isInitialized = useAuthStore((state) => state.isInitialized)
  const setSession = useAuthStore((state) => state.setSession)
  const finishInitialization = useAuthStore((state) => state.finishInitialization)
  const started = useRef(false)

  useEffect(() => {
    if (started.current) return
    started.current = true
    authApi.refresh().then(setSession).catch(finishInitialization)
  }, [finishInitialization, setSession])

  if (!isInitialized) return <main className="grid min-h-screen place-items-center text-slate-600">Loading StudyFlow…</main>
  return children
}
