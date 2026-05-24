# 核心模块详细设计

> **所属文档**: [架构设计总览](README.md)  
> **最后更新**: 2026-05-22  

---

## 目录

- [1. 路由设计 (routes/ + guards/ 分文件架构)](#1-路由设计)
- [2. 状态管理设计](#2-状态管理设计)
- [3. API 服务层设计](#3-api-服务层设计)
- [4. 国际化设计](#4-国际化设计)
- [5. 主题系统设计](#5-主题系统设计)
- [6. 认证与权限设计](#6-认证与权限设计)
- [7. TypeScript 类型体系](#7-typescript-类型体系)
- [8. 图表系统设计 (ECharts)](#8-图表系统设计-echarts)
- [9. Excel 导入导出设计 (xlsx)](#9-excel-导入导出设计-xlsx)
- [10. 应用入口改造](#10-应用入口改造)
- [11. 依赖补充清单](#11-依赖补充清单)
- [12. 环境变量](#12-环境变量)

---

## 1. 路由设计

### 1.1 分文件架构概览

```
src/router/
├── index.ts                  ← 聚合入口：createRouter() + setupRouterGuards()
├── routes/                   ← 路由表定义（按功能域拆分）
│   ├── index.ts              ← 聚合导出：合并所有模块路由为 RouteRecordRaw[]
│   ├── auth.route.ts         ← 公开路由 /login
│   ├── dashboard.route.ts    ← 仪表盘路由
│   ├── users.route.ts        ← 用户管理路由（列表 + 详情 + 流量统计）
│   ├── nodes.route.ts        ← 节点管理路由（列表 + 详情 + 状态历史）
│   ├── admins.route.ts       ← 管理员管理路由（仅 super_admin）
│   ├── audit.route.ts        ← 审计日志路由
│   └── errors.route.ts       ← 错误页面路由（403 / 404）
└── guards/                   ← 导航守卫（按职责拆分）
    ├── index.ts              ← 聚合注册：依次注册所有守卫函数
    ├── title.guard.ts        ← 页面标题守卫（document.title 设置）
    ├── auth.guard.ts         ← 认证守卫（Token 检查 + 401 重定向 + 用户信息懒加载）
    └── permission.guard.ts   ← 权限守卫（角色检查 + 403 重定向）
```

### 1.2 路由聚合入口 (`router/index.ts`)

```typescript
// src/router/index.ts

import { createRouter, createWebHistory } from 'vue-router'
import { routes } from './routes'
import { setupRouterGuards } from './guards'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes,
})

// 注册所有导航守卫
setupRouterGuards(router)

export default router
```

### 1.3 路由表聚合 (`routes/index.ts`)

```typescript
// src/router/routes/index.ts

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
```

### 1.4 各模块路由表示例

```typescript
// src/router/routes/auth.route.ts

import type { RouteRecordRaw } from 'vue-router'

export const authRoutes: RouteRecordRaw[] = [
  {
    path: '/login',
    name: 'Login',
    component: () => import('@/views/auth/LoginView.vue'),
    meta: { title: '登录', requiresAuth: false, layout: 'auth' },
  },
]
```

```typescript
// src/router/routes/dashboard.route.ts

import type { RouteRecordRaw } from 'vue-router'

export const dashboardRoutes: RouteRecordRaw[] = [
  {
    path: '/',
    component: () => import('@/layouts/DefaultLayout.vue'),
    meta: { requiresAuth: true },
    redirect: '/dashboard',
    children: [
      {
        path: 'dashboard',
        name: 'Dashboard',
        component: () => import('@/views/dashboard/DashboardView.vue'),
        meta: {
          title: '仪表盘',
          icon: 'pi pi-home',
          roles: ['super_admin', 'admin', 'readonly'],
        },
      },
    ],
  },
]
```

```typescript
// src/router/routes/users.route.ts

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
```

```typescript
// src/router/routes/nodes.route.ts

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
```

```typescript
// src/router/routes/admins.route.ts

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
```

```typescript
// src/router/routes/audit.route.ts

import type { RouteRecordRaw } from 'vue-router'

export const auditRoutes: RouteRecordRaw[] = [
  {
    path: 'audit-logs',
    name: 'AuditLogs',
    component: () => import('@/views/audit/AuditLogView.vue'),
    meta: {
      title: '审计日志',
      icon: 'pi pi-history',
      roles: ['super_admin', 'admin'],
    },
  },
]
```

```typescript
// src/router/routes/errors.route.ts

import type { RouteRecordRaw } from 'vue-router'

export const errorRoutes: RouteRecordRaw[] = [
  {
    path: '/403',
    name: 'Forbidden',
    component: () => import('@/views/errors/ForbiddenView.vue'),
    meta: { title: '无权限' },
  },
  {
    path: '/:pathMatch(.*)*',
    name: 'NotFound',
    component: () => import('@/views/errors/NotFoundView.vue'),
    meta: { title: '页面未找到' },
  },
]
```

### 1.5 路由 Meta 扩展

```typescript
// src/types/common.types.ts 或 router 内联

declare module 'vue-router' {
  interface RouteMeta {
    /** 页面标题 (用于 document.title 和面包屑) */
    title?: string
    /** 侧边栏图标 (PrimeIcons class) */
    icon?: string
    /** 允许访问的角色列表，undefined = 不限制 */
    roles?: AdminRole[]
    /** 是否需要认证 */
    requiresAuth?: boolean
    /** 是否在侧边栏隐藏 */
    hidden?: boolean
    /** 使用的布局模板 */
    layout?: 'auth' | 'default'
  }
}
```

### 1.6 导航守卫 — 聚合注册 (`guards/index.ts`)

```typescript
// src/router/guards/index.ts

import type { Router } from 'vue-router'
import { titleGuard } from './title.guard'
import { authGuard } from './auth.guard'
import { permissionGuard } from './permission.guard'

export function setupRouterGuards(router: Router) {
  router.beforeEach(titleGuard)
  router.beforeEach(authGuard)
  router.beforeEach(permissionGuard)
}
```

### 1.7 页面标题守卫 (`guards/title.guard.ts`)

```typescript
// src/router/guards/title.guard.ts

import type { Router } from 'vue-router'
import type { NavigationGuard } from 'vue-router'

export const titleGuard: NavigationGuard = (to, from, next) => {
  document.title = to.meta.title ? `${to.meta.title} - Hysteria Auth` : 'Hysteria Auth'
  next()
}
```

### 1.8 认证守卫 (`guards/auth.guard.ts`)

```typescript
// src/router/guards/auth.guard.ts

import type { NavigationGuard } from 'vue-router'
import { useAuthStore } from '@/stores/auth.store'

export const authGuard: NavigationGuard = async (to, from, next) => {
  // 公开页面放行
  if (to.meta.requiresAuth === false) {
    return next()
  }

  const authStore = useAuthStore()

  // 无 Token → 重定向登录
  if (!authStore.token) {
    return next({ name: 'Login', query: { redirect: to.fullPath } })
  }

  // 懒加载管理员信息（首次进入）
  if (!authStore.adminInfo) {
    try {
      await authStore.fetchAdminInfo()
    } catch {
      authStore.logout()
      return next({ name: 'Login', query: { redirect: to.fullPath } })
    }
  }

  next()
}
```

### 1.9 权限守卫 (`guards/permission.guard.ts`)

```typescript
// src/router/guards/permission.guard.ts

import type { NavigationGuard } from 'vue-router'
import { useAuthStore } from '@/stores/auth.store'

export const permissionGuard: NavigationGuard = async (to, from, next) => {
  const authStore = useAuthStore()
  const requiredRoles = to.meta.roles

  // 无角色限制 → 放行
  if (!requiredRoles || requiredRoles.length === 0) {
    return next()
  }

  // 角色不在允许列表中 → 403
  if (authStore.role && !requiredRoles.includes(authStore.role)) {
    return next({ name: 'Forbidden' })
  }

  next()
}
```

### 1.10 导航守卫执行顺序

```
每个路由导航按照 guards/index.ts 中的注册顺序依次执行：

  1. titleGuard        → 设置 document.title
  2. authGuard         → Token 有效性检查 + 管理员信息懒加载
  3. permissionGuard   → 角色权限检查

所有守卫通过 next() 链式放行，任一守卫调用 next({ name: 'XXX' }) 则中断后续执行并跳转。
```

### 1.11 侧边栏菜单生成

侧边栏菜单**从路由配置动态生成**，而非硬编码，确保与路由守卫权限一致：

```typescript
// 侧边栏菜单项 = routes 中 children 过滤 hidden !== true && 角色匹配
import { routes } from '@/router/routes'

const menuItems = computed(() => {
  const defaultLayout = routes.find(r => r.path === '/')
  return (
    defaultLayout?.children?.filter(
      r => !r.meta?.hidden && (!r.meta?.roles || r.meta.roles.includes(authStore.role))
    ) ?? []
  )
})
```

---

## 2. 状态管理设计

### 2.1 Store 全景

```
┌───────────────────────────────────────────────────────────────────┐
│                        Pinia Stores                                │
│                                                                    │
│  ┌─────────────┐  ┌──────────────┐  ┌────────────────────────┐    │
│  │  authStore   │  │  appStore    │  │     themeStore         │    │
│  │             │  │              │  │                        │    │
│  │ token       │  │ sidebarColl. │  │ mode: 'light'|'dark'   │    │
│  │ adminInfo   │  │ globalLoad.  │  │ toggleTheme()          │    │
│  │ role        │  │ locale       │  │ initTheme()            │    │
│  │ login()     │  │ setLocale()  │  │                        │    │
│  │ logout()    │  │              │  │ → localStorage 持久化   │    │
│  │ refreshT.() │  │              │  └────────────────────────┘    │
│  └──────┬──────┘  └──────┬───────┘                                │
│         │                │                                        │
│  ┌──────┴──────┐  ┌──────┴──────────┐  ┌──────────────────┐      │
│  │ userStore   │  │  nodeStore      │  │ dashboardStore   │      │
│  │             │  │                 │  │                  │      │
│  │ list (分页) │  │ list (分页)     │  │ overview 数据    │      │
│  │ current     │  │ current         │  │ fetchOverview()  │      │
│  │ filters     │  │ statusHistory   │  │                  │      │
│  │ trafficData │  │ filters         │  └──────────────────┘      │
│  │ CRUD ops   │  │ rotateSecret()  │                              │
│  └─────────────┘  └─────────────────┘                             │
└───────────────────────────────────────────────────────────────────┘
```

### 2.2 `authStore` — 认证状态

```typescript
// src/stores/auth.store.ts (接口定义)

interface AuthState {
  token: string | null
  refreshToken: string | null
  adminInfo: AdminDto | null
  expiresAt: string | null
}

interface AuthActions {
  login(username: string, password: string): Promise<void>
  logout(): void
  fetchAdminInfo(): Promise<void>
  refreshToken(): Promise<void>          // Phase 2 实现
}

interface AuthGetters {
  isAuthenticated: boolean
  role: AdminRole | null
  isSuperAdmin: boolean
  isAdmin: boolean
}
```

**Token 持久化**：`token` 存储于 `localStorage`，Store 初始化时从 localStorage 恢复。

**登录流程**：
```
LoginView                  authStore                     API
   │                          │                          │
   │── login(u, p) ────────►│                          │
   │                          │── apiLogin(u, p) ──────►│
   │                          │◄── {token, admin, ...} ─│
   │                          │  保存 token → localStorage
   │                          │  保存 adminInfo → state
   │◄── resolve ─────────────│                          │
   │                          │                          │
   │   router.push(redirect)  │                          │
```

**登出流程**：
```
Header/Token expired          authStore                   Router
   │                          │                          │
   │── logout() ────────────►│                          │
   │                          │  清除 localStorage       │
   │                          │  重置 state              │
   │                          │── router.push('/login') ─►│
```

### 2.3 `themeStore` — 主题状态

```typescript
// src/stores/theme.store.ts (接口定义)

type ThemeMode = 'light' | 'dark'

interface ThemeState {
  mode: ThemeMode
}

interface ThemeActions {
  setTheme(mode: ThemeMode): void   // 切换主题
  toggleTheme(): void               // light ↔ dark
  initTheme(): void                 // 从 localStorage 恢复或跟随系统偏好
}
```

**主题切换链路**：
```
ThemeStore.setTheme('dark')
    │
    ├─ ① 更新 state.mode = 'dark'
    │
    ├─ ② localStorage.setItem('theme', 'dark')
    │
    ├─ ③ document.documentElement.classList.add('dark')
    │     → 触发 TailwindCSS darkMode: 'class'
    │     → 触发 CSS 变量 .dark { } 覆盖（来自 design-tokens 规范）
    │
    └─ ④ PrimeVue darkModeSelector: '.dark' 自动同步
```

### 2.4 `usersStore` / `nodesStore` — 列表 Store 通用模式

所有列表 Store 遵循统一模式：

```typescript
// 通用列表 Store 模式 (以 usersStore 为例)

interface ListState<T, F> {
  items: T[]
  total: number
  page: number
  pageSize: number
  loading: boolean
  filters: F
  searchQuery: string
}

// 通用操作
fetchList(): Promise<void>       // 获取列表 (带当前分页+筛选参数)
resetFilters(): void             // 重置筛选
setPage(page: number): void      // 翻页
setSearch(q: string): void       // 搜索 (防抖)
```

---

## 3. API 服务层设计

### 3.1 axios 实例配置

```typescript
// src/api/index.ts

import axios from 'axios'
import type { AxiosInstance, InternalAxiosRequestConfig, AxiosResponse, AxiosError } from 'axios'
import { useAuthStore } from '@/stores/auth.store'

const http: AxiosInstance = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL,  // 如 https://master.example.com/api/v1
  timeout: 30000,
  headers: { 'Content-Type': 'application/json' },
})

// ── 请求拦截器：自动注入 JWT Token ──
http.interceptors.request.use((config: InternalAxiosRequestConfig) => {
  const authStore = useAuthStore()
  if (authStore.token) {
    config.headers.Authorization = `Bearer ${authStore.token}`
  }
  return config
})

// ── 响应拦截器：统一错误处理 ──
http.interceptors.response.use(
  (response: AxiosResponse) => response,
  (error: AxiosError<{ error?: { code: string; message: string; requestId: string } }>) => {
    const authStore = useAuthStore()
    const errorData = error.response?.data?.error

    switch (errorData?.code) {
      case 'token_expired':
      case 'token_invalid':
      case 'unauthorized':
        authStore.logout()
        break
      case 'forbidden':
        break
    }

    return Promise.reject(error)
  }
)

export default http
```

### 3.2 API 模块划分

对照 [`master-panel-api.md`](../exchange/master-panel-api.md) 的接口分组：

| API 模块文件 | 后端路由前缀 | 包含接口 |
|-------------|-------------|---------|
| `auth.ts` | `/api/v1/admin/login` | POST login |
| `dashboard.ts` | `/api/v1/admin/dashboard` | GET dashboard |
| `users.ts` | `/api/v1/users` | POST 创建, GET 列表, GET 详情, PUT 更新, DELETE 删除, POST 重置流量, GET 流量统计 |
| `nodes.ts` | `/api/v1/nodes` + `/api/v1/admin/nodes` | GET 列表, GET 详情, GET 状态历史, POST 预注册, POST 轮换密钥, POST 踢用户, **PUT 更新节点配置 (Phase 7)** |
| `admins.ts` | `/api/v1/admin/admins` | POST 创建, GET 列表, PUT 更新 |
| `audit-logs.ts` | `/api/v1/admin/audit-logs` | GET 列表 |

### 3.3 API 模块代码结构示例

```typescript
// src/api/modules/users.ts

import http from '@/api'
import type { PageResponse } from '@/api/types'
import type { UserDto, CreateUserRequest, UpdateUserRequest, UserFilters, UserTrafficStats } from '@/types/user.types'

export const userApi = {
  /** 获取用户列表 (分页+筛选) */
  getList(params: UserFilters & { page: number; pageSize: number }) {
    return http.get<PageResponse<UserDto>>('/users', { params })
  },

  /** 获取用户详情 */
  getById(id: number) {
    return http.get<UserDto>(`/users/${id}`)
  },

  /** 创建用户 */
  create(data: CreateUserRequest) {
    return http.post<UserDto>('/users', data)
  },

  /** 更新用户 (部分更新) */
  update(id: number, data: UpdateUserRequest) {
    return http.put<UserDto>(`/users/${id}`, data)
  },

  /** 删除用户 (软删除) */
  delete(id: number) {
    return http.delete(`/users/${id}`)
  },

  /** 重置用户流量 */
  resetTraffic(id: number) {
    return http.post<{ message: string }>(`/users/${id}/reset-traffic`)
  },

  /** 获取用户流量统计 */
  getTrafficStats(id: number, period: 'day' | 'week' | 'month' | 'all' = 'month') {
    return http.get<UserTrafficStats>(`/users/${id}/traffic-stats`, { params: { period } })
  },
}
```

### 3.4 Phase 7 新增 API — 更新节点配置

```typescript
// src/api/modules/nodes.ts (Phase 7 追加)

import type { UpdateNodeConfigRequest } from '@/types/node.types'

export const nodeApi = {
  // ... 已有方法

  /** Phase 7: 更新节点 Hysteria 2 配置 (所有字段可选, null=不修改) */
  updateConfig(id: string, data: UpdateNodeConfigRequest) {
    return http.put<NodeDto>(`/admin/nodes/${id}/config`, data)
  },
}
```

### 3.5 数据适配器

处理 API 原始数据与前端使用格式的转换：

```typescript
// src/api/adapters/user.adapter.ts

// 后端 allowedNodes 为 JSON 字符串 "[\"node-01\",\"node-02\"]"
// 前端需要 string[]
export function adaptUserDto(raw: RawUserDto): UserDto {
  return {
    ...raw,
    allowedNodes: raw.allowedNodes ? JSON.parse(raw.allowedNodes) as string[] : [],
  }
}
```

### 3.6 分页类型

```typescript
// src/api/types.ts

export interface PageRequest {
  page: number      // 从 1 开始
  pageSize: number  // 最大 100
}

export interface PageResponse<T> {
  total: number
  page: number
  pageSize: number
  items: T[]
}

export interface ApiError {
  error: {
    code: string
    message: string
    requestId: string
  }
}
```

---

## 4. 国际化设计

### 4.1 技术方案

- **库**：`vue-i18n` ^11.x
- **语言**：简体中文 (默认 `zh-CN`)、英文 (`en-US`)
- **持久化**：用户选择的语言存储于 `localStorage`
- **Lazy Loading**：按语言按需加载（非首屏必要）

### 4.2 语言包结构

```
src/locales/
├── index.ts            # vue-i18n 实例创建 + 全局配置
├── zh-CN/
│   ├── index.ts        # 聚合导出
│   ├── common.json     # 通用：
│   │                    #   actions: { save, cancel, delete, edit, create, search, reset }
│   │                    #   status: { active, inactive, online, offline, pending, provisioned }
│   │                    #   unit: { bytes, kb, mb, gb, tb }
│   │                    #   confirm: { title, message, yes, no }
│   │                    #   toast: { success, error, warning, info }
│   │                    #   pagination: { total, perPage, goto }
│   ├── auth.json       # 登录页：title, username, password, loginBtn, error.*
│   ├── dashboard.json  # 仪表盘：title, cards.*, charts.*
│   ├── users.json      # 用户管理：table.*, form.*, detail.*, traffic.*
│   ├── nodes.json      # 节点管理：table.*, form.*, detail.*, statusHistory.*
│   ├── admins.json     # 管理员管理
│   ├── audit.json      # 审计日志
│   └── validation.json # 校验规则：required, minLength, maxLength, pattern, traffic.*
└── en-US/
    └── (与 zh-CN 结构完全对称)
```

### 4.3 Key 命名规范

```
{domain}.{section}.{key}

示例:
  users.table.columns.username          → "用户名"
  users.form.fields.totalTraffic.label  → "总流量配额"
  users.form.fields.totalTraffic.hint   → "0 表示不限"
  users.toast.createSuccess             → "用户创建成功"
  users.toast.deleteConfirm             → "确定要删除该用户吗？此操作不可恢复。"
  validation.required                   → "此项为必填"
  common.actions.save                   → "保存"
```

### 4.4 vue-i18n 实例创建

```typescript
// src/locales/index.ts

import { createI18n } from 'vue-i18n'
import zhCN from './zh-CN'
import enUS from './en-US'

const i18n = createI18n({
  legacy: false,           // Composition API 模式
  locale: 'zh-CN',         // 默认语言
  fallbackLocale: 'zh-CN', // 回退语言
  messages: {
    'zh-CN': zhCN,
    'en-US': enUS,
  },
  missingWarn: import.meta.env.DEV,
  fallbackWarn: import.meta.env.DEV,
})

export default i18n
```

### 4.5 语言切换

```typescript
// src/stores/app.store.ts 中维护

function setLocale(locale: 'zh-CN' | 'en-US') {
  appState.locale = locale
  localStorage.setItem('locale', locale)
  i18n.global.locale.value = locale
}

function initLocale() {
  const saved = localStorage.getItem('locale') as 'zh-CN' | 'en-US' | null
  setLocale(saved || 'zh-CN')
}
```

---

## 5. 主题系统设计

### 5.1 双主题策略

```
┌─────────────────────────────────────────────────────────────────┐
│                      主题系统架构                                │
│                                                                 │
│  ThemeStore                                                      │
│    mode: 'light' | 'dark'                                        │
│                                                                 │
│    ├─→ document.documentElement.classList.toggle('dark')         │
│    │     ├─→ TailwindCSS dark: 变体生效                          │
│    │     │     dark:bg-gray-900 dark:text-gray-100               │
│    │     └─→ CSS 变量 .dark { } 覆盖生效                         │
│    │           --bg-primary  → #1F2937 (亮 → 暗)                 │
│    │           --text-primary → #F9FAFB                         │
│    │                                                             │
│    └─→ PrimeVue darkModeSelector: '.dark' 自动同步               │
│                                                                 │
│  所有颜色/间距/动效令牌参见: [design-tokens.md](design-tokens.md) │
└─────────────────────────────────────────────────────────────────┘
```

### 5.2 PrimeVue 暗色主题

```typescript
// main.ts — 主题初始化

import Material from '@primeuix/themes/material'

app.use(PrimeVue, {
  theme: {
    preset: Material,
    options: {
      darkModeSelector: '.dark',  // 跟随 TailwindCSS 的 dark class
    },
  },
  ripple: true,
})
```

### 5.3 主题持久化与初始化

```typescript
// src/stores/theme.store.ts

export const useThemeStore = defineStore('theme', () => {
  const mode = ref<'light' | 'dark'>('light')

  function initTheme() {
    const saved = localStorage.getItem('theme')
    if (saved === 'dark' || saved === 'light') {
      mode.value = saved
    } else if (window.matchMedia('(prefers-color-scheme: dark)').matches) {
      mode.value = 'dark'
    }
    applyTheme()
  }

  function toggleTheme() {
    mode.value = mode.value === 'light' ? 'dark' : 'light'
    applyTheme()
  }

  function applyTheme() {
    document.documentElement.classList.toggle('dark', mode.value === 'dark')
    localStorage.setItem('theme', mode.value)
  }

  return { mode, initTheme, toggleTheme }
})
```

### 5.4 设计令牌引用

组件中的颜色/间距/动效必须遵循 [`design-tokens.md`](design-tokens.md) 规范，通过 CSS 变量或 TailwindCSS `@theme` 扩展使用。亮/暗色切换通过 CSS 变量 `.dark` 覆盖自动完成。

---

## 6. 认证与权限设计

### 6.1 认证流程图

```
                    ┌─────────────────────────┐
                    │     访问受保护页面        │
                    └───────────┬─────────────┘
                                │
                        ┌───────▼────────┐
                        │ 有有效 Token?   │
                        └───┬────────┬───┘
                        No  │        │ Yes
                    ┌───────▼──┐ ┌───▼───────────┐
                    │跳转登录页 │ │ 有 adminInfo?   │
                    └──────────┘ └───┬────────┬───┘
                                No  │        │ Yes
                    ┌───────────────▼──┐ ┌───▼──────┐
                    │ fetchAdminInfo() │ │ 权限检查  │
                    └───┬──────────────┘ └───┬──────┘
                   失败 │                    │ 通过
               ┌────────▼────────┐   ┌───────▼──────┐
               │ 清除 Token      │   │   渲染页面    │
               │ 跳转登录页      │   └──────────────┘
               └─────────────────┘
```

### 6.2 角色权限矩阵

| 功能 | `super_admin` | `admin` | `readonly` |
|------|:---:|:---:|:---:|
| 查看仪表盘 | ✅ | ✅ | ✅ |
| 查看用户列表/详情 | ✅ | ✅ | ✅ |
| 查看用户流量统计 | ✅ | ✅ | ✅ |
| 创建/编辑/删除用户 | ✅ | ✅ | ❌ |
| 踢用户下线 | ✅ | ✅ | ❌ |
| 重置用户流量 | ✅ | ✅ | ❌ |
| 查看节点列表/详情 | ✅ | ✅ | ✅ |
| 查看节点状态历史 | ✅ | ✅ | ✅ |
| 预注册节点 | ✅ | ✅ | ❌ |
| 轮换节点密钥 | ✅ | ✅ | ❌ |
| 管理员管理 | ✅ | ❌ | ❌ |
| 查看审计日志 | ✅ | ✅ | ❌ |

### 6.3 权限指令

```typescript
// src/directives/permission.ts

import type { Directive } from 'vue'
import { useAuthStore } from '@/stores/auth.store'

export const vPermission: Directive<HTMLElement, string[]> = {
  mounted(el, binding) {
    const authStore = useAuthStore()
    const requiredRoles = binding.value

    if (!requiredRoles || requiredRoles.length === 0) return

    const hasPermission = authStore.role && requiredRoles.includes(authStore.role)
    if (!hasPermission) {
      el.parentNode?.removeChild(el)
    }
  },
}
```

**使用示例**：
```vue
<Button v-permission="['super_admin', 'admin']" label="创建用户" @click="openCreateDialog" />
```

---

## 7. TypeScript 类型体系

### 7.1 类型文件对照

| 类型文件 | 对应后端 DTO | 对应前端 Store |
|----------|-------------|---------------|
| `api.types.ts` | 统一错误响应、分页 | — |
| `auth.types.ts` | `AdminDto`, 登录请求/响应 | `authStore` |
| `user.types.ts` | `UserDto`, 创建/更新请求 | `usersStore` |
| `node.types.ts` | `NodeDto`, 状态历史等 | `nodesStore` |
| `admin.types.ts` | 管理员 CRUD 请求/响应 | — |
| `dashboard.types.ts` | 仪表盘概览 | `dashboardStore` |
| `audit.types.ts` | 审计日志条目 | — |
| `common.types.ts` | 枚举、选项等 | 全局 |

### 7.2 核心类型定义示例

```typescript
// src/types/auth.types.ts

export type AdminRole = 'super_admin' | 'admin' | 'readonly'

export interface AdminDto {
  id: number
  username: string
  role: AdminRole
  isActive: boolean
  createdAt: string  // ISO 8601
  lastLoginAt: string | null
}

export interface LoginRequest {
  username: string
  password: string
}

export interface LoginResponse {
  token: string
  expiresAt: string
  admin: AdminDto
}
```

```typescript
// src/types/user.types.ts

export interface UserDto {
  id: number
  username: string
  email: string | null
  totalTrafficBytes: number
  usedTrafficBytes: number
  isActive: boolean
  createdAt: string
  updatedAt: string
  expiresAt: string | null
  allowedNodes: string[]
  allowedNodesRaw: string
  remark: string | null
}

export interface CreateUserRequest {
  username: string
  password: string
  email?: string
  totalTrafficBytes?: number
  isActive?: boolean
  expiresAt?: string | null
  allowedNodes?: string[]
  remark?: string
}

export interface UpdateUserRequest {
  email?: string
  totalTrafficBytes?: number
  isActive?: boolean
  expiresAt?: string | null
  allowedNodes?: string[]
  remark?: string
  password?: string
}

export interface UserFilters {
  search?: string
  isActive?: boolean
  nodeId?: string
}

export interface UserTrafficStats {
  userId: number
  period: 'day' | 'week' | 'month' | 'all'
  totalBytesIn: number
  totalBytesOut: number
  dataPoints: TrafficDataPoint[]
}

export interface TrafficDataPoint {
  date: string
  bytesIn: number
  bytesOut: number
}
```

```typescript
// src/types/node.types.ts

export type ProvisionStatus = 'pending' | 'provisioned'

// Phase 7 新增：混淆类型、拥塞控制算法、伪装类型、DNS 解析器类型
export type ObfsType = 'salamander'
export type CongestionControl = 'bbr' | 'cubic' | 'brutal'
export type MasqueradeType = 'file' | 'proxy' | 'string' | 'reply'
export type ResolverType = 'system' | 'udp' | 'tcp' | 'tls'

export interface NodeDto {
  id: string
  name: string
  ipAddress: string | null
  port: number
  isActive: boolean
  createdAt: string
  lastHeartbeat: string | null
  location: string | null
  trafficStatsPort?: number
  provisionStatus: ProvisionStatus
  // ═══ Phase 7 新增: 监听与端口跳跃 ═══
  listenAddress?: string | null          // 默认 "0.0.0.0"
  listenPort?: number | null             // 默认 6789
  enablePortHopping?: boolean            // 默认 true
  portHopRangeStart?: number | null      // 默认 61000
  portHopRangeEnd?: number | null        // 默认 63000
  // ═══ Phase 7 新增: 混淆与拥塞控制 ═══
  obfsType?: ObfsType | null
  obfsPassword?: string | null           // 服务端 AES-256-GCM 加密存储，返回脱敏值
  congestionControl?: CongestionControl | null
  brutalTxBandwidth?: number | null      // Brutal 发送带宽 (bps)
  // ═══ Phase 7 新增: 带宽与速度测试 ═══
  bandwidthUp?: string | null            // 如 "100 mbps"
  bandwidthDown?: string | null
  ignoreClientBandwidth?: boolean | null
  enableSpeedTest?: boolean | null
  speedTestPingInterval?: number | null  // Ping 间隔 (秒)
  // ═══ Phase 7 新增: UDP 与协议嗅探 ═══
  udpIdleTimeout?: number | null         // 默认 60
  sniffEnabled?: boolean | null
  sniffTimeout?: number | null
  sniffRespectHttps?: boolean | null
  // ═══ Phase 7 新增: 伪装 ═══
  masqueradeType?: MasqueradeType | null
  masqueradeFile?: string | null         // type=file
  masqueradeProxyUrl?: string | null     // type=proxy
  masqueradeStringContent?: string | null // type=string
  masqueradeStringHeaders?: string | null // type=string
  masqueradeStringStatusCode?: number | null // type=string
  // ═══ Phase 7 新增: DNS ═══
  resolverType?: ResolverType | null
  resolverTcpAddr?: string | null
  resolverUdpAddr?: string | null
  resolverTlsAddr?: string | null
  // ═══ Phase 7 新增: 配置版本与运营管理 ═══
  configVersion?: number                 // 每次 PUT 后自增
  configUpdatedAt?: string | null        // ISO 8601
  serverCost?: number | null             // 月付金额
  billingCycle?: 'monthly' | 'quarterly' | 'yearly' | null
  expirationDate?: string | null         // ISO 8601
  domainName?: string | null             // 关联域名, 写入 YAML realm
  remark?: string | null                 // 备注信息
}

export interface NodeDetail extends NodeDto {
  secretVersion: number
  trafficStatsSecret?: string            // Phase 7: 脱敏为 "***encrypted***"
}

export interface NodeStatusRecord {
  id: number
  nodeId: string
  cpuUsagePercent: number
  memoryUsagePercent: number
  memoryUsedMb: number
  memoryTotalMb: number
  networkInBytes: number
  networkOutBytes: number
  networkInMbps: number
  networkOutMbps: number
  activeConnections: number
  reportedAt: string
}

export interface PreRegisterNodeRequest {
  name: string
  location?: string
  port?: number
  trafficStatsPort?: number
  // Phase 7 新增
  listenPort?: number                    // 默认 6789
  domainName?: string                    // 写入 YAML realm
  remark?: string                        // 备注信息
}

export interface PreRegisterNodeResponse {
  provisionToken: string
  masterServerUrl: string
  expiresAt: string
  startupCommand: string
}

// ═══ Phase 7 全新类型 ═══

/** PUT /api/v1/admin/nodes/{nodeId}/config 请求体 — 所有字段可选, null=不修改 */
export interface UpdateNodeConfigRequest {
  // 监听
  listenAddress?: string | null
  listenPort?: number | null
  enablePortHopping?: boolean | null
  portHopRangeStart?: number | null
  portHopRangeEnd?: number | null
  // 混淆
  obfsType?: ObfsType | null
  obfsPassword?: string | null
  // 拥塞控制
  congestionControl?: CongestionControl | null
  brutalTxBandwidth?: number | null
  // QUIC
  quicMaxIdleTimeout?: number | null
  quicMaxUdpPayloadSize?: number | null
  // 带宽
  bandwidthUp?: string | null
  bandwidthDown?: string | null
  ignoreClientBandwidth?: boolean | null
  // 速度测试
  enableSpeedTest?: boolean | null
  speedTestPingInterval?: number | null
  // UDP
  udpIdleTimeout?: number | null
  // 协议嗅探
  sniffEnabled?: boolean | null
  sniffTimeout?: number | null
  sniffRespectHttps?: boolean | null
  // 伪装
  masqueradeType?: MasqueradeType | null
  masqueradeFile?: string | null
  masqueradeProxyUrl?: string | null
  masqueradeStringContent?: string | null
  masqueradeStringHeaders?: string | null
  masqueradeStringStatusCode?: number | null
  // DNS
  resolverType?: ResolverType | null
  resolverTcpAddr?: string | null
  resolverUdpAddr?: string | null
  resolverTlsAddr?: string | null
  // 运营管理
  serverCost?: number | null
  billingCycle?: string | null
  expirationDate?: string | null
  domainName?: string | null
  remark?: string | null
}
```

```typescript
// src/types/common.types.ts

export type Period = 'day' | 'week' | 'month' | 'all'
export type AuditAction = 'create' | 'update' | 'delete' | 'login' | 'logout' | 'kick_user'
export type AuditTargetType = 'user' | 'node' | 'admin' | 'system'
export type Severity = 'success' | 'info' | 'warn' | 'error'
```

---

## 8. 图表系统设计 (ECharts)

### 8.1 技术选型

- **库**：`echarts` ^5.5.x
- **封装方式**：图表组件按就近归置原则，放置在使用图表的 views 模块的 `components/` 子目录下（如 `src/views/dashboard/components/`、`src/views/nodes/components/`）
- **辅助 composable**：[`useECharts.ts`](../../src/composables/useECharts.ts) 管理 init / setOption / resize / dispose

### 8.2 BaseChart 组件

```vue
<!-- 图表组件应就近归置到使用图表的模块: src/views/{module}/components/BaseChart.vue -->

<template>
  <div ref="chartRef" :style="{ width: '100%', height: height }" />
</template>

<script setup lang="ts">
import { ref, onMounted, onBeforeUnmount, watch } from 'vue'
import { useECharts } from '@/composables/useECharts'
import type { EChartsOption } from 'echarts'

const props = withDefaults(defineProps<{
  option: EChartsOption
  height?: string
  theme?: 'light' | 'dark'
}>(), {
  height: '320px',
  theme: 'light',
})

const chartRef = ref<HTMLElement>()
const { initChart, setOption, resize, dispose, getInstance } = useECharts()

onMounted(() => {
  if (chartRef.value) {
    initChart(chartRef.value, props.theme)
    setOption(props.option)
    window.addEventListener('resize', resize)
  }
})

onBeforeUnmount(() => {
  window.removeEventListener('resize', resize)
  dispose()
})

watch(() => props.option, (newOpt) => {
  setOption(newOpt, true) // notMerge = true 用于完全替换
}, { deep: true })

watch(() => props.theme, (newTheme) => {
  if (chartRef.value) {
    dispose()
    initChart(chartRef.value, newTheme)
    setOption(props.option)
  }
})

defineExpose({ getInstance, resize })
</script>
```

### 8.3 useECharts Composable

```typescript
// src/composables/useECharts.ts

import * as echarts from 'echarts/core'
import { CanvasRenderer } from 'echarts/renderers'
import { LineChart, BarChart, PieChart } from 'echarts/charts'
import {
  TitleComponent, TooltipComponent, LegendComponent,
  GridComponent, ToolboxComponent, DataZoomComponent,
} from 'echarts/components'
import type { EChartsOption, ECharts } from 'echarts'

// 按需注册
echarts.use([
  CanvasRenderer,
  LineChart, BarChart, PieChart,
  TitleComponent, TooltipComponent, LegendComponent,
  GridComponent, ToolboxComponent, DataZoomComponent,
])

export function useECharts() {
  let instance: ECharts | null = null

  function initChart(el: HTMLElement, theme: 'light' | 'dark' = 'light') {
    instance = echarts.init(el, theme)
  }

  function setOption(option: EChartsOption, notMerge = false) {
    instance?.setOption(option, { notMerge })
  }

  function resize() {
    instance?.resize()
  }

  function dispose() {
    instance?.dispose()
    instance = null
  }

  function getInstance() {
    return instance
  }

  return { initChart, setOption, resize, dispose, getInstance }
}
```

### 8.4 图表使用场景

| 场景 | 图表类型 | 封装组件（就近归置） | 数据来源 |
|------|---------|---------|---------|
| 仪表盘 - 今日/本月流量趋势 | 面积图 | `AreaChart.vue`（→ `src/views/dashboard/components/`） | `GET /api/v1/admin/dashboard` |
| 节点详情 - CPU 使用率 | 折线图 | `LineChart.vue`（→ `src/views/nodes/components/`） | `GET /api/v1/nodes/{id}/status-history` |
| 节点详情 - 内存使用率 | 折线图 | `LineChart.vue`（→ `src/views/nodes/components/`） | 同上 |
| 节点详情 - 网络速率 | 双 Y 轴折线图 | `LineChart.vue`（→ `src/views/nodes/components/`） | 同上 |
| 节点详情 - 活跃连接数 | 折线图 | `LineChart.vue`（→ `src/views/nodes/components/`） | 同上 |
| 用户详情 - 流量趋势 | 面积图 | `AreaChart.vue`（→ `src/views/users/components/`） | `GET /api/v1/users/{id}/traffic-stats` |

### 8.5 暗色主题适配

ECharts 实例初始化时传入 `theme` 参数。BaseChart 组件通过 `theme` prop 接收当前主题模式（来自 [`themeStore`](#23-themestore--主题状态)），亮色传 `'light'`，暗色传 `'dark'`。

ECharts 内置 `'dark'` 主题，也可通过 `echarts.registerTheme()` 自定义主题以匹配 [`design-tokens.md`](design-tokens.md) 中的色彩规范。

---

## 9. Excel 导入导出设计 (xlsx)

### 9.1 技术选型

- **库**：`xlsx` (SheetJS) ^0.20.x
- **定位**：后续开发阶段支持以下功能：
  - **导出**：用户列表 / 节点列表 / 审计日志 / 流量统计数据 → `.xlsx` 文件下载
  - **导入**：批量创建用户（Excel 模板上传解析）

### 9.2 Composable — `useExportExcel`

```typescript
// src/composables/useExportExcel.ts

import * as XLSX from 'xlsx'
import { useToast } from '@/composables/useToast'
import { useI18n } from 'vue-i18n'

export interface ExcelColumn<T = Record<string, unknown>> {
  /** 列标题 (需国际化) */
  header: string
  /** 数据字段 key */
  key: keyof T | string
  /** 宽度 (字符数)，默认 15 */
  width?: number
  /** 自定义格式化函数 */
  format?: (value: unknown, row: T) => string | number
}

export function useExportExcel() {
  const toast = useToast()
  const { t } = useI18n()

  /** 将数据导出为 Excel 并触发下载 */
  function exportToExcel<T extends Record<string, unknown>>(
    data: T[],
    columns: ExcelColumn<T>[],
    filename: string,
    sheetName: string = 'Sheet1',
  ) {
    try {
      // 构造表格数据（第一行为列头）
      const headers = columns.map(col => col.header)
      const rows = data.map(row =>
        columns.map(col => {
          const raw = (row as Record<string, unknown>)[col.key as string]
          return col.format ? col.format(raw, row) : (raw ?? '')
        })
      )

      const sheetData = [headers, ...rows]
      const worksheet = XLSX.utils.aoa_to_sheet(sheetData)

      // 设置列宽
      worksheet['!cols'] = columns.map(col => ({
        wch: col.width ?? 15,
      }))

      const workbook = XLSX.utils.book_new()
      XLSX.utils.book_append_sheet(workbook, worksheet, sheetName)
      XLSX.writeFile(workbook, `${filename}.xlsx`)

      toast.success(t('common.toast.exportSuccess'))
    } catch (err) {
      console.error('Excel export failed:', err)
      toast.error(t('common.toast.exportFailed'))
    }
  }

  return { exportToExcel }
}
```

### 9.3 工具函数 — `export-xlsx`

```typescript
// src/utils/export-xlsx.ts

import * as XLSX from 'xlsx'
import type { ExcelColumn } from '@/composables/useExportExcel'

/**
 * 创建 Excel 工作簿下载（纯函数版本，不依赖 Toast）
 */
export function downloadExcel<T extends Record<string, unknown>>(
  data: T[],
  columns: ExcelColumn<T>[],
  filename: string,
  sheetName: string = 'Sheet1',
) {
  const headers = columns.map(col => col.header)
  const rows = data.map(row =>
    columns.map(col => {
      const raw = (row as Record<string, unknown>)[col.key as string]
      return col.format ? col.format(raw, row) : (raw ?? '')
    })
  )

  const worksheet = XLSX.utils.aoa_to_sheet([headers, ...rows])
  worksheet['!cols'] = columns.map(col => ({ wch: col.width ?? 15 }))

  const workbook = XLSX.utils.book_new()
  XLSX.utils.book_append_sheet(workbook, worksheet, sheetName)
  XLSX.writeFile(workbook, `${filename}.xlsx`)
}

/**
 * 从上传的 Excel 文件中解析数据
 * @returns 解析后的行数组，每行为 Record<string, unknown>
 */
export function parseExcelFile(file: File): Promise<Record<string, unknown>[]> {
  return new Promise((resolve, reject) => {
    const reader = new FileReader()
    reader.onload = (e) => {
      try {
        const data = new Uint8Array(e.target!.result as ArrayBuffer)
        const workbook = XLSX.read(data, { type: 'array' })
        const firstSheet = workbook.Sheets[workbook.SheetNames[0]!]
        const json = XLSX.utils.sheet_to_json<Record<string, unknown>>(firstSheet)
        resolve(json)
      } catch (err) {
        reject(err)
      }
    }
    reader.onerror = reject
    reader.readAsArrayBuffer(file)
  })
}
```

### 9.4 导出功能应用场景

| 页面 | 导出内容 | 触发方式 |
|------|---------|---------|
| 用户列表 | 当前筛选结果导出为 Excel | PrimeVue DataTable 导出按钮 / Toolbar 按钮 |
| 节点列表 | 当前筛选结果导出为 Excel | 同上 |
| 审计日志 | 当前筛选结果导出为 Excel | 同上 |
| 用户流量统计 | 流量数据点导出为 Excel | 用户详情页图表旁按钮 |

> **导入功能**（批量创建用户）：后续开发阶段实现，UI 层面通过 `parseExcelFile()` 解析上传的 `.xlsx` 文件后批量调用 `POST /api/v1/users` 创建。

---

## 10. 应用入口改造

### 10.1 新的 `main.ts`

```typescript
import '@/assets/styles/public.css'
import '@/assets/styles/variables.css'
import { createApp } from 'vue'
import { createPinia } from 'pinia'
import App from './App.vue'
import router from './router'
import i18n from './locales'

// PrimeVue
import PrimeVue from 'primevue/config'
import Material from '@primeuix/themes/material'
import ToastService from 'primevue/toastservice'
import ConfirmationService from 'primevue/confirmationservice'
import Tooltip from 'primevue/tooltip'

// 初始化
import { useThemeStore } from '@/stores/theme.store'
import { useAuthStore } from '@/stores/auth.store'
import { vPermission } from '@/directives/permission'

const app = createApp(App)

// ── 插件注册 ──
const pinia = createPinia()
app.use(pinia)

// 恢复认证状态
const authStore = useAuthStore()
authStore.initFromStorage()

// 恢复主题
const themeStore = useThemeStore()
themeStore.initTheme()

// 路由 (守卫已在 router/index.ts 中通过 setupRouterGuards 注册)
app.use(router)

app.use(i18n)

// PrimeVue
app.use(PrimeVue, {
  theme: {
    preset: Material,
    options: {
      darkModeSelector: '.dark',
    },
  },
  ripple: true,
})
app.use(ToastService)
app.use(ConfirmationService)
app.directive('tooltip', Tooltip)

// 自定义指令
app.directive('permission', vPermission)

app.mount('#app')
```

### 10.2 新的 `App.vue`

```vue
<template>
  <RouterView />
  <Toast position="top-right" />
  <ConfirmDialog />
</template>

<script setup lang="ts">
import { useI18n } from 'vue-i18n'

const { locale } = useI18n()
const savedLocale = localStorage.getItem('locale')
if (savedLocale) {
  locale.value = savedLocale
}
</script>
```

---

## 11. 依赖补充清单

当前 `package.json` 中已包含的依赖之外，需要新增：

```json
{
  "dependencies": {
    "vue-i18n": "^11.0.0",
    "axios": "^1.7.0",
    "echarts": "^5.5.0",
    "xlsx": "^0.20.0",
    "@vueuse/core": "^12.0.0",
    "date-fns": "^4.0.0"
  }
}
```

| 包 | 用途 |
|----|------|
| `vue-i18n` | 国际化 |
| `axios` | HTTP 客户端（拦截器、请求取消） |
| `echarts` | 图表库（节点 CPU/内存/带宽监控，用户流量趋势） |
| `xlsx` | Excel 导入导出（用户/节点/审计数据表格导出，批量创建用户导入） |
| `@vueuse/core` | 常用 composables（`useStorage`、`useMediaQuery` 等） |
| `date-fns` | 日期格式化（树摇友好，替代 moment.js） |

---

## 12. 环境变量

```bash
# .env.development
VITE_API_BASE_URL=http://localhost:5000/api/v1

# .env.production
VITE_API_BASE_URL=https://master.example.com/api/v1
```
