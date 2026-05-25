import http from '@/api'
import type { PageResponse } from '@/api/types'
import type { AuditLogEntry, AuditLogFilters } from '@/types/audit.types'

export const auditLogApi = {
  getList(params: AuditLogFilters & { page: number; pageSize: number }) {
    return http.get<PageResponse<AuditLogEntry>>('/admin/audit-logs', { params })
  },
}
