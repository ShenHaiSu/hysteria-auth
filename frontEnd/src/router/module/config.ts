import type { RouteRecordRaw } from 'vue-router'

/**
 * 配置下载模块路由定义
 */
export const configRoutes: RouteRecordRaw = {
  path: '/config-download',
  component: () => import('@/layout/MainLayout.vue'),
  meta: {
    requiresAuth: true,
  },
  children: [
    {
      path: '',
      name: 'ConfigDownload',
      component: () => import('@/view/ConfigDownload.vue'),
      meta: {
        title: '配置下载',
      },
    },
  ],
}
