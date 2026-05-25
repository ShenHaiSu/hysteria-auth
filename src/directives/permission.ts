import type { Directive } from 'vue'
import { useAuthStore } from '@/stores/auth.store'

export const vPermission: Directive<HTMLElement, string[]> = {
  mounted(el, binding) {
    const authStore = useAuthStore()
    const requiredRoles = binding.value

    if (!requiredRoles || requiredRoles.length === 0) return

    const hasPermission = authStore.role && requiredRoles.includes(authStore.role)
    if (!hasPermission) {
      el.parentNode?.removeChild(el)
    }
  },
}
