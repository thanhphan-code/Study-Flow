import type { PropsWithChildren } from 'react'
import { Navigate, useLocation } from 'react-router-dom'
import { useAuthStore } from '@/features/auth/store/authStore'
import { AppShell } from '@/components/AppShell'

export function ProtectedRoute({ children }: PropsWithChildren) {
  const user = useAuthStore((state) => state.user)
  const location = useLocation()
  return user ? <AppShell>{children}</AppShell> : <Navigate to="/login" replace state={{ from: location.pathname }} />
}
