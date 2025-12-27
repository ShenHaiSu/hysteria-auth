import type { RouteRecordRaw } from 'vue-router'

// #region 用户管理路由定义
/**
 * 用户管理模块路由
 * 包含用户列表展示及相关管理操作
 */
export const userRoutes: RouteRecordRaw = {
  path: '/users',
  component: () => import('@/layout/MainLayout.vue'),
  meta: { requiresAuth: true },
  children: [
    {
      path: '',
      name: 'user-list',
      component: () => import('@/view/User.vue'),
    },
  ],
}
// #endregion
