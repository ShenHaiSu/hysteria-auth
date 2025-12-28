import type { RouteRecordRaw } from 'vue-router'

// #region 节点管理路由定义
/**
 * 节点管理模块路由
 * 包含代理节点列表展示及相关管理操作
 */
export const nodeRoutes: RouteRecordRaw = {
  path: '/nodes',
  component: () => import('@/layout/MainLayout.vue'),
  meta: { requiresAuth: true },
  children: [
    {
      path: '',
      name: 'node-list',
      component: () => import('@/view/Node.vue'),
    },
  ],
}
// #endregion
