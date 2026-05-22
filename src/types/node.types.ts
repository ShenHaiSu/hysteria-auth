import type { ProvisionStatus } from '@/types/common.types'

export interface NodeDto {
  id: string
  name: string
  ipAddress: string | null
  port: number
  isActive: boolean
  createdAt: string
  lastHeartbeat: string | null
  location: string | null
  trafficStatsPort?: number
  provisionStatus: ProvisionStatus
}

export interface NodeDetail extends NodeDto {
  secretVersion: number
  trafficStatsSecret?: string
}

export interface NodeStatusRecord {
  id: number
  nodeId: string
  cpuUsagePercent: number
  memoryUsagePercent: number
  memoryUsedMb: number
  memoryTotalMb: number
  networkInBytes: number
  networkOutBytes: number
  networkInMbps: number
  networkOutMbps: number
  activeConnections: number
  reportedAt: string
}

export interface PreRegisterNodeRequest {
  name: string
  location?: string
  port?: number
  trafficStatsPort?: number
}

export interface PreRegisterNodeResponse {
  provisionToken: string
  masterServerUrl: string
  expiresAt: string
  startupCommand: string
}
