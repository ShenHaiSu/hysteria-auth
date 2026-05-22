export interface DashboardOverview {
  totalUsers: number
  activeUsers: number
  onlineUsers: number
  totalNodes: number
  activeNodes: number
  todayTrafficBytes: number
  monthTrafficBytes: number
  trafficTrend: TrafficTrendPoint[]
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
