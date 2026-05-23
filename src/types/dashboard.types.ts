export interface DashboardOverview {
  totalUsers: number
  activeUsers: number
  totalNodes: number
  activeNodes: number
  onlineUsersNow: number
  totalTrafficToday: number
  totalTrafficThisMonth: number
  /** Phase 5 补充 */
  trafficTrend: TrafficTrendPoint[]
  /** Phase 5 补充 */
  userTrend: UserTrendPoint[]
}

export interface TrafficTrendPoint {
  date: string
  bytesIn: number
  bytesOut: number
}

export interface UserTrendPoint {
  date: string
  count: number
}
