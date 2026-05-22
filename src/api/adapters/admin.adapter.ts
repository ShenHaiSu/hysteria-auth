import type { AdminDto } from '@/types/admin.types'

/**
 * 管理员数据适配器 — 当前仅做透传，保留扩展点
 */
export function adaptAdminDto(raw: AdminDto): AdminDto {
  return raw
}
