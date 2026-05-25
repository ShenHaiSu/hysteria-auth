import type { RouteRecordRaw } from 'vue-router'
import { authRoutes } from './auth.route'
import { dashboardRoutes } from './dashboard.route'
import { userRoutes } from './users.route'
import { nodeRoutes } from './nodes.route'
import { adminRoutes } from './admins.route'
import { auditRoutes } from './audit.route'
import { errorRoutes } from './errors.route'

export const routes: RouteRecordRaw[] = [
  // ── Auth 布局：仅 /login ──
  {
    path: '/login',
    name: 'AuthLayout',
    component: () => import('@/layouts/AuthLayout.vue'),
    meta: { requiresAuth: false },
    children: authRoutes,
  },

  // ── Default 布局：侧边栏+头部+内容区，所有受保护页面 ──
  {
    path: '/',
    component: () => import('@/layouts/DefaultLayout.vue'),
    meta: { requiresAuth: true },
    redirect: '/dashboard',
    children: [
      ...dashboardRoutes,
      ...userRoutes,
      ...nodeRoutes,
      ...adminRoutes,
      ...auditRoutes,
    ],
  },

  // ── 错误页面：独立无布局 ──
  ...errorRoutes,
]
