import type { RouteRecordRaw } from 'vue-router'

/**
 * 错误模块路由配置
 * 包含 404 兜底路由
 */
export const errorRoutes: RouteRecordRaw = {
  path: '/:pathMatch(.*)*',
  name: 'NotFound',
  component: () => import('@/view/NotFound.vue'),
  meta: {
    title: '页面未找到',
    requiresAuth: false
  }
}
