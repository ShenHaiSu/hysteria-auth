import type { RouteRecordRaw } from 'vue-router'

export const adminRoutes: RouteRecordRaw[] = [
  {
    path: 'admins',
    name: 'Admins',
    component: () => import('@/views/admins/AdminListView.vue'),
    meta: {
      title: '管理员管理',
      icon: 'pi pi-shield',
      roles: ['super_admin'],
    },
  },
]
