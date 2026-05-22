import type { NavigationGuard } from 'vue-router'

export const titleGuard: NavigationGuard = (to, _from, next) => {
  document.title = to.meta.title ? `${to.meta.title} - Hysteria Auth` : 'Hysteria Auth'
  next()
}
