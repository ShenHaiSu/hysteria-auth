import { createRouter, createWebHistory, type RouteRecordRaw } from 'vue-router'
import { authGuard } from '@/router/middleware/auth'
import loginRoute from '@/router/module/login'
import { homeRoute } from '@/router/module/home'
import { userRoutes } from '@/router/module/user'
import { errorRoutes } from '@/router/module/error'

// #region 路由实例配置
/**
 * 创建并配置路由实例
 */
const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    // 登录页路由
    loginRoute,
    // 首页路由
    homeRoute,
    // 用户模块路由
    userRoutes,
    // 兜底 404 路由 (必须放在最后)
    errorRoutes,
  ],
})

/**
 * 挂载全局前置守卫
 */
router.beforeEach(authGuard)
// #endregion

export default router
