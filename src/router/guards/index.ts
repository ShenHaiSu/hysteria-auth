import type { Router } from 'vue-router'
import { titleGuard } from './title.guard'
import { authGuard } from './auth.guard'
import { permissionGuard } from './permission.guard'

export function setupRouterGuards(router: Router) {
  router.beforeEach(titleGuard)
  router.beforeEach(authGuard)
  router.beforeEach(permissionGuard)
}
