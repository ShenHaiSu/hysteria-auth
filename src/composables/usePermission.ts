import { computed } from 'vue'
import { useAuthStore } from '@/stores/auth.store'
import type { AdminRole } from '@/types/common.types'

/**
 * 角色权限判断 Composable
 */
export function usePermission() {
  const authStore = useAuthStore()

  const role = computed(() => authStore.role)

  /** 检查当前用户是否拥有指定角色之一 */
  function hasRole(...roles: AdminRole[]): boolean {
    if (!authStore.role) return false
    return roles.includes(authStore.role)
  }

  /** 检查当前用户是否拥有任一指定角色 */
  function hasAnyRole(roles: AdminRole[]): boolean {
    return hasRole(...roles)
  }

  const canEdit = computed(() => authStore.isAdmin)
  const isSuperAdmin = computed(() => authStore.isSuperAdmin)
  const isReadonly = computed(() => authStore.role === 'readonly')

  return { role, hasRole, hasAnyRole, canEdit, isSuperAdmin, isReadonly }
}
