import type { RouteRecordRaw } from 'vue-router'

export const nodeRoutes: RouteRecordRaw[] = [
  {
    path: 'nodes',
    name: 'Nodes',
    component: () => import('@/views/nodes/NodeListView.vue'),
    meta: {
      title: '节点管理',
      icon: 'pi pi-server',
      roles: ['super_admin', 'admin', 'readonly'],
    },
  },
  {
    path: 'nodes/:id',
    name: 'NodeDetail',
    component: () => import('@/views/nodes/NodeDetailView.vue'),
    meta: {
      title: '节点详情',
      roles: ['super_admin', 'admin', 'readonly'],
      hidden: true,
    },
  },
]
