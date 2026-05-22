import type { RouteRecordRaw } from 'vue-router'
import { authRoutes } from './auth.route'
import { dashboardRoutes } from './dashboard.route'
import { userRoutes } from './users.route'
import { nodeRoutes } from './nodes.route'
import { adminRoutes } from './admins.route'
import { auditRoutes } from './audit.route'
import { errorRoutes } from './errors.route'

export const routes: RouteRecordRaw[] = [
  ...authRoutes,
  ...dashboardRoutes,
  ...userRoutes,
  ...nodeRoutes,
  ...adminRoutes,
  ...auditRoutes,
  ...errorRoutes,
]
