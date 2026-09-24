import { apiRequest } from '@/api/httpClient'
import type { Dashboard } from '@/features/dashboard/types/dashboard'

export const dashboardKeys = { all: ['dashboard'] as const }
export const dashboardApi = { get: () => apiRequest<Dashboard>('/dashboard') }
