import type { RouteRecordRaw } from 'vue-router'

export const userRoutes: RouteRecordRaw[] = [
  {
    path: 'users',
    name: 'Users',
    component: () => import('@/views/users/UserListView.vue'),
    meta: {
      title: '用户管理',
      icon: 'pi pi-users',
      roles: ['super_admin', 'admin', 'readonly'],
    },
  },
  {
    path: 'users/:id',
    name: 'UserDetail',
    component: () => import('@/views/users/UserDetailView.vue'),
    meta: {
      title: '用户详情',
      roles: ['super_admin', 'admin', 'readonly'],
      hidden: true,
    },
  },
]
