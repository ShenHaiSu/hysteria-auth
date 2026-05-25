import http from '@/api'
import type { DashboardOverview } from '@/types/dashboard.types'

export const dashboardApi = {
  getOverview() {
    return http.get<DashboardOverview>('/admin/dashboard')
  },
}
