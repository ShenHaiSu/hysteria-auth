import type { AdminDto } from '@/types/auth.types'
import type { AdminRole } from '@/types/common.types'

export type { AdminDto, AdminRole }

export interface CreateAdminRequest {
  username: string
  password: string
  role: AdminRole
}

export interface UpdateAdminRequest {
  role?: AdminRole
  password?: string
  isActive?: boolean
}

export interface AdminFilters {
  search?: string
  role?: AdminRole
  isActive?: boolean
}
