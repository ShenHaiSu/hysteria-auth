import type { AdminRole } from '@/types/common.types'

export interface AdminDto {
  id: number
  username: string
  role: AdminRole
  isActive: boolean
  createdAt: string
  lastLoginAt: string | null
}

export interface LoginRequest {
  username: string
  password: string
}

export interface LoginResponse {
  token: string
  expiresAt: string
  admin: AdminDto
}
