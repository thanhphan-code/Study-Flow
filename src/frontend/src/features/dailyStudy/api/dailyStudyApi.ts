import { apiRequest } from '@/api/httpClient'
import type { DailyStudyFocus, DailyStudyPlan } from '@/features/dailyStudy/types/dailyStudy'

export const dailyStudyApi = {
  getPlan: (minutes: number, focus: DailyStudyFocus) => apiRequest<DailyStudyPlan>(`/daily-study?minutes=${minutes}&focus=${focus}`),
}
