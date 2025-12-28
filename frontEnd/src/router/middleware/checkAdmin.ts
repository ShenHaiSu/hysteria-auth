import type { NavigationGuardNext, RouteLocationNormalized } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

// #region 权限守卫
/**
 * 管理员权限校验守卫
 * 当路由 meta 中 permissionAdmin 为 true 时，校验当前用户是否为管理员
 * 如果用户不是管理员，则禁止进入并重定向到首页
 * @param to 目标路由
 * @param from 来源路由
 * @param next 放行函数
 */
export const checkAdminGuard = (
  to: RouteLocationNormalized,
  from: RouteLocationNormalized,
  next: NavigationGuardNext,
) => {
  const authStore = useAuthStore()

  // 检查目标路由或其父路由是否要求管理员权限
  const requiresAdmin = to.matched.some((record) => record.meta.permissionAdmin)

  if (requiresAdmin) {
    if (authStore.isAdmin) {
      // 是管理员，放行
      next()
    } else {
      // 不是管理员，禁止进入，重定向到首页
      // 如果未登录，authGuard 会处理跳转到登录页，所以这里主要处理已登录但权限不足的情况
      next('/')
    }
  } else {
    // 不需要管理员权限，直接放行
    next()
  }
}
// #endregion
