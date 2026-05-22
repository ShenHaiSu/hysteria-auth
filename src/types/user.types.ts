export interface UserDto {
  id: number
  username: string
  email: string | null
  totalTrafficBytes: number
  usedTrafficBytes: number
  isActive: boolean
  createdAt: string
  updatedAt: string
  expiresAt: string | null
  allowedNodes: string[]
  allowedNodesRaw: string
  remark: string | null
}

export interface CreateUserRequest {
  username: string
  password: string
  email?: string
  totalTrafficBytes?: number
  isActive?: boolean
  expiresAt?: string | null
  allowedNodes?: string[]
  remark?: string
}

export interface UpdateUserRequest {
  email?: string
  totalTrafficBytes?: number
  isActive?: boolean
  expiresAt?: string | null
  allowedNodes?: string[]
  remark?: string
  password?: string
}

export interface UserFilters {
  search?: string
  isActive?: boolean
  nodeId?: string
}

export interface TrafficDataPoint {
  date: string
  bytesIn: number
  bytesOut: number
}

export interface UserTrafficStats {
  userId: number
  period: 'day' | 'week' | 'month' | 'all'
  totalBytesIn: number
  totalBytesOut: number
  dataPoints: TrafficDataPoint[]
}
