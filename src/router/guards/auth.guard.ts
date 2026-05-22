import type { NavigationGuard } from 'vue-router'
import { useAuthStore } from '@/stores/auth.store'

export const authGuard: NavigationGuard = async (to, _from, next) => {
  if (to.meta.requiresAuth === false) {
    return next()
  }

  const authStore = useAuthStore()

  if (!authStore.token) {
    return next({ name: 'Login', query: { redirect: to.fullPath } })
  }

  if (!authStore.adminInfo) {
    try {
      await authStore.fetchAdminInfo()
    } catch {
      authStore.logout()
      return next({ name: 'Login', query: { redirect: to.fullPath } })
    }
  }

  next()
}
