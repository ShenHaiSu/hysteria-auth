export type AdminRole = 'super_admin' | 'admin' | 'readonly'

export type ProvisionStatus = 'pending' | 'provisioned'

export type Period = 'day' | 'week' | 'month' | 'all'

export type AuditAction = 'create' | 'update' | 'delete' | 'login' | 'logout' | 'kick_user'

export type AuditTargetType = 'user' | 'node' | 'admin' | 'system'

export type Severity = 'success' | 'info' | 'warn' | 'error'
