import { create } from 'zustand'
import type { AuthPayload } from '@/api/httpClient'

export type CurrentUser = AuthPayload['user']
type AuthState = {
  user: CurrentUser | null
  accessToken: string | null
  isInitialized: boolean
  setSession: (payload: AuthPayload) => void
  clearSession: () => void
  finishInitialization: () => void
}

export const useAuthStore = create<AuthState>((set) => ({
  user: null,
  accessToken: null,
  isInitialized: false,
  setSession: (payload) => set({ user: payload.user, accessToken: payload.accessToken, isInitialized: true }),
  clearSession: () => set({ user: null, accessToken: null, isInitialized: true }),
  finishInitialization: () => set({ isInitialized: true }),
}))
