import type { AdminRole } from '@/types/common.types'

/** 管理员角色列表 */
export const ADMIN_ROLES: { label: string; value: AdminRole }[] = [
  { label: '超级管理员', value: 'super_admin' },
  { label: '管理员', value: 'admin' },
  { label: '只读管理员', value: 'readonly' },
]

/** 角色映射 (value → label) */
export const ADMIN_ROLE_MAP: Record<AdminRole, string> = {
  super_admin: '超级管理员',
  admin: '管理员',
  readonly: '只读管理员',
}

/** 分页默认值 */
export const DEFAULT_PAGE = 1
export const DEFAULT_PAGE_SIZE = 20
export const PAGE_SIZE_OPTIONS = [10, 20, 50, 100]

/** 状态常量 */
export const STATUS = {
  ACTIVE: 'active' as const,
  INACTIVE: 'inactive' as const,
  ONLINE: 'online' as const,
  OFFLINE: 'offline' as const,
  PENDING: 'pending' as const,
  PROVISIONED: 'provisioned' as const,
} as const
