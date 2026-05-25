import type { RouteLocationNormalized, RouteLocationRaw } from 'vue-router'
import { useAuthStore } from '@/stores/auth.store'

export const permissionGuard = (
  to: RouteLocationNormalized,
  _from: RouteLocationNormalized,
): boolean | RouteLocationRaw => {
  const authStore = useAuthStore()
  const requiredRoles = to.meta.roles

  if (!requiredRoles || requiredRoles.length === 0) {
    return true
  }

  if (authStore.role && !requiredRoles.includes(authStore.role)) {
    return { name: 'Forbidden' }
  }

  return true
}
