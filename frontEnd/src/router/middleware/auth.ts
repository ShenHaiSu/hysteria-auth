import type { NavigationGuardNext, RouteLocationNormalized } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

// #region 认证守卫
/**
 * 认证拦截守卫函数
 * 处理未登录跳转和已登录拦截
 * @param to 目标路由
 * @param from 来源路由
 * @param next 放行函数
 */
export const authGuard = (
  to: RouteLocationNormalized,
  from: RouteLocationNormalized,
  next: NavigationGuardNext,
) => {
  const authStore = useAuthStore()

  if (to.meta.requiresAuth && !authStore.isAuthenticated) {
    // 需要认证但未登录
    next('/login')
  } else if (to.meta.guest && authStore.isAuthenticated) {
    // 游客页面但已登录
    next('/')
  } else {
    next()
  }
}
// #endregion
