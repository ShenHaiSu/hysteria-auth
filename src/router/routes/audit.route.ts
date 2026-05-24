import type { RouteRecordRaw } from 'vue-router'

export const auditRoutes: RouteRecordRaw[] = [
  {
    path: 'audit-logs',
    name: 'AuditLogs',
    component: () => import('@/views/audit/AuditLogView.vue'),
    meta: {
      title: '审计日志',
      icon: 'pi pi-history',
      roles: ['super_admin', 'admin'],
    },
  },
]
