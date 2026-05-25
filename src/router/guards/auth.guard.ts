import type { RouteLocationNormalized, RouteLocationRaw } from 'vue-router'
import { useAuthStore } from '@/stores/auth.store'

export const authGuard = async (
  to: RouteLocationNormalized,
  _from: RouteLocationNormalized,
): Promise<boolean | RouteLocationRaw> => {
  if (to.meta.requiresAuth === false) {
    return true
  }

  const authStore = useAuthStore()

  if (!authStore.token) {
    return { name: 'Login', query: { redirect: to.fullPath } } as RouteLocationRaw
  }

  if (!authStore.adminInfo) {
    try {
      await authStore.fetchAdminInfo()
    } catch {
      authStore.logout()
      return { name: 'Login', query: { redirect: to.fullPath } } as RouteLocationRaw
    }
  }

  return true
}
