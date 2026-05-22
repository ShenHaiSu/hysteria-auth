import type { NavigationGuard } from 'vue-router'
import { useAuthStore } from '@/stores/auth.store'

export const permissionGuard: NavigationGuard = (_to, _from, next) => {
  const authStore = useAuthStore()
  const requiredRoles = _to.meta.roles

  if (!requiredRoles || requiredRoles.length === 0) {
    return next()
  }

  if (authStore.role && !requiredRoles.includes(authStore.role)) {
    return next({ name: 'Forbidden' })
  }

  next()
}
