import type { AuditAction, AuditTargetType } from '@/types/common.types'

export interface AuditLogEntry {
  id: number
  action: AuditAction
  targetType: AuditTargetType
  targetId: string
  targetName: string | null
  adminId: number
  adminUsername: string
  detail: string | null
  ipAddress: string | null
  createdAt: string
}

export interface AuditLogFilters {
  action?: AuditAction
  targetType?: AuditTargetType
  adminId?: number
  startDate?: string
  endDate?: string
  search?: string
}
