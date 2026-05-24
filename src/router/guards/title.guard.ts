import type { RouteLocationNormalized } from 'vue-router'

export const titleGuard = (to: RouteLocationNormalized, _from: RouteLocationNormalized): true => {
  document.title = to.meta.title ? `${to.meta.title} - Hysteria Auth` : 'Hysteria Auth'
  return true
}
