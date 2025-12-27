import type { RouteRecordRaw } from 'vue-router'

// #region 路由定义
/**
 * 根路径路由定义
 * 包含主布局及首页
 */
export const homeRoute: RouteRecordRaw = {
  path: '/',
  component: () => import('@/layout/MainLayout.vue'),
  meta: { requiresAuth: true },
  children: [
    {
      path: '',
      name: 'home',
      component: () => import('@/view/Home.vue'),
    },
  ],
}

// #endregion
