# Phase 0：基础设施搭建

> **预计工时**：~2 天  
> **前置依赖**：无（项目起点）  
> **关联文档**：[阶段总览](./README.md) · [目录结构](../architect/directory-structure.md) · [面板 API](../exchange/master-panel-api.md)

---

## 阶段目标

建立项目的完整骨架：目录结构、类型系统、API 层、状态管理骨架、路由框架、CSS 变量体系，确保后续所有阶段有统一的基础设施可依赖。

---

## 任务清单

### 0.1 目录结构创建

- [ ] 按照 [`directory-structure.md`](../architect/directory-structure.md) 创建完整目录结构
- [ ] 创建所有空占位文件（确保 import 路径不报错）

### 0.2 依赖安装

- [ ] 安装 `vue-i18n` ^11.x — 国际化
- [ ] 安装 `axios` ^1.7.x — HTTP 客户端
- [ ] 安装 `echarts` ^5.5.x — 图表库
- [ ] 安装 `xlsx` ^0.20.x — Excel 导入导出
- [ ] 安装 `@vueuse/core` ^12.x — 通用 composables
- [ ] 安装 `date-fns` ^4.x — 日期格式化

### 0.3 TypeScript 类型体系

| 文件 | 内容 |
|------|------|
| [`src/types/api.types.ts`](../../src/types/api.types.ts) | `PageRequest`、`PageResponse<T>`、`ApiError` |
| [`src/types/common.types.ts`](../../src/types/common.types.ts) | `AdminRole`、`ProvisionStatus`、`Period`、`AuditAction`、`AuditTargetType`、`Severity` |
| [`src/types/auth.types.ts`](../../src/types/auth.types.ts) | `AdminDto`、`LoginRequest`、`LoginResponse` |
| [`src/types/user.types.ts`](../../src/types/user.types.ts) | `UserDto`、`CreateUserRequest`、`UpdateUserRequest`、`UserFilters`、`UserTrafficStats`、`TrafficDataPoint` |
| [`src/types/node.types.ts`](../../src/types/node.types.ts) | `NodeDto`、`NodeDetail`、`NodeStatusRecord`、`PreRegisterNodeRequest`、`PreRegisterNodeResponse` |
| [`src/types/admin.types.ts`](../../src/types/admin.types.ts) | 管理员 CRUD 请求/响应类型 |
| [`src/types/dashboard.types.ts`](../../src/types/dashboard.types.ts) | `DashboardOverview` |
| [`src/types/audit.types.ts`](../../src/types/audit.types.ts) | `AuditLogEntry` |
| [`src/types/vue-router.d.ts`](../../src/types/vue-router.d.ts) | RouteMeta 扩展 |

### 0.4 API 服务层

- [ ] `src/api/index.ts` — axios 实例创建 + 请求拦截器（JWT 注入）+ 响应拦截器（统一错误处理）
- [ ] `src/api/modules/auth.ts` — 登录 API
- [ ] `src/api/modules/dashboard.ts` — 仪表盘 API
- [ ] `src/api/modules/users.ts` — 用户 CRUD API
- [ ] `src/api/modules/nodes.ts` — 节点管理 API
- [ ] `src/api/modules/admins.ts` — 管理员管理 API
- [ ] `src/api/modules/audit-logs.ts` — 审计日志 API
- [ ] `src/api/adapters/user.adapter.ts` — `allowedNodes` JSON 字符串解析
- [ ] `src/api/adapters/node.adapter.ts` — 节点数据适配
- [ ] `src/api/adapters/admin.adapter.ts` — 管理员数据适配

### 0.5 状态管理骨架

- [ ] `src/stores/auth.store.ts` — 认证状态（token/adminInfo/role、login/logout/fetchAdminInfo）
- [ ] `src/stores/theme.store.ts` — 主题状态（mode、toggleTheme/initTheme）
- [ ] `src/stores/app.store.ts` — 应用状态（sidebarCollapsed、globalLoading、locale）
- [ ] `src/stores/users.store.ts` — 用户管理状态（列表/详情/筛选/分页/CRUD）
- [ ] `src/stores/nodes.store.ts` — 节点管理状态
- [ ] `src/stores/dashboard.store.ts` — 仪表盘状态
- [ ] 删除 `src/stores/counter.ts`（模板文件）

### 0.6 路由框架

- [ ] `src/router/routes/auth.route.ts`
- [ ] `src/router/routes/dashboard.route.ts`
- [ ] `src/router/routes/users.route.ts`
- [ ] `src/router/routes/nodes.route.ts`
- [ ] `src/router/routes/admins.route.ts`
- [ ] `src/router/routes/audit.route.ts`
- [ ] `src/router/routes/errors.route.ts`
- [ ] `src/router/routes/index.ts` — 聚合所有路由
- [ ] `src/router/guards/title.guard.ts`
- [ ] `src/router/guards/auth.guard.ts`
- [ ] `src/router/guards/permission.guard.ts`
- [ ] `src/router/guards/index.ts` — `setupRouterGuards()`
- [ ] 重写 `src/router/index.ts` — 使用新的 routes + guards

### 0.7 CSS 变量与设计令牌

- [ ] `src/assets/styles/variables.css` — 全部 CSS 自定义属性（品牌色/亮暗色主题/排版/圆角/动效）+ TailwindCSS `@theme` 扩展
- [ ] `src/assets/styles/transition.css` — 路由过渡动画 + 组件过渡动画
- [ ] `src/assets/styles/scrollbar.css` — 自定义滚动条样式
- [ ] 更新 `src/assets/styles/public.css` — 确保 `variables.css` 在 TailwindCSS 之前引入

> 详细设计令牌规范见 [`../architect/design-tokens.md`](../architect/design-tokens.md)

### 0.8 应用入口改造

- [ ] 重写 `src/App.vue` — `<RouterView />` + `<Toast />` + `<ConfirmDialog />`
- [ ] 重写 `src/main.ts` — 完整插件注册链：

```
Pinia → auth/theme init → Router → i18n → PrimeVue + darkModeSelector → Toast → Confirmation → Tooltip → v-permission
```

### 0.9 工具函数

- [ ] `src/utils/constants.ts` — 枚举常量（角色/状态/分页默认值）
- [ ] `src/utils/storage.ts` — localStorage 类型安全封装
- [ ] `src/utils/format.ts` — 日期/文件大小/数字格式化
- [ ] `src/utils/validators.ts` — 表单校验规则

### 0.10 自定义指令

- [ ] `src/directives/permission.ts` — `v-permission` 角色权限指令

### 0.11 环境变量

- [ ] `.env.development` — `VITE_API_BASE_URL=http://localhost:5000/api/v1`
- [ ] `.env.production` — `VITE_API_BASE_URL=https://master.example.com/api/v1`

---

## 重难点

| 难点 | 说明 | 解决方案 |
|------|------|----------|
| **axios 响应拦截器与 Pinia Store 循环依赖** | 拦截器中需要使用 `useAuthStore()` 处理 token 过期跳转，但 axios 实例在 Pinia 之前创建 | 在拦截器函数内部延迟调用 `useAuthStore()`（函数调用时 Pinia 已初始化） |
| **RouteMeta TypeScript 扩展** | `vue-router` 的 `RouteMeta` 类型需要通过 `declare module` 扩展，位置和方式需正确 | 在 `src/types/vue-router.d.ts` 中声明，确保 `tsconfig` 包含此文件 |
| **TailwindCSS v4 `@theme` 扩展与 CSS 变量优先级** | TailwindCSS v4 的 `@theme` 与 `:root` CSS 变量作用域不同 | 品牌色通过 `@theme` 扩展（不随主题变化），亮/暗色切换的变量仅通过 CSS 自定义属性 `.dark` 覆盖 |
| **目录结构一次性创建** | 大量空文件需要一次性创建，容易遗漏 | 严格按照 [`directory-structure.md`](../architect/directory-structure.md) 逐条核对 |

---

## 注意点

1. **所有文件必须使用 `<script setup lang="ts">`**，禁止使用 Options API
2. **所有 import 使用 `@/` 别名**，不要使用相对路径 `../../../`
3. **Token 持久化**：`authStore` 的 token 必须存入 `localStorage`，初始化时恢复
4. **主题持久化**：`themeStore` 的主题选择存入 `localStorage`，初始化时恢复（优先 localStorage → 系统偏好 → 默认亮色）
5. **路由守卫顺序**：`titleGuard` → `authGuard` → `permissionGuard`，不可打乱

---

## 阶段验证清单

Phase 0 完成后，必须通过以下**全部**验证方可进入 [Phase 1](./phase-1-auth-layout.md)：

| # | 验证项 | 验证方法 |
|---|--------|----------|
| 0.1 | `bun run type-check` 无错误 | 执行命令，确认 0 错误 |
| 0.2 | `bun run dev` 正常启动，无编译错误 | 浏览器打开，无白屏 |
| 0.3 | 所有依赖安装完毕 | `bun list` 确认 vue-i18n/axios/echarts/xlsx/@vueuse/core/date-fns 均存在 |
| 0.4 | 目录结构与 [`directory-structure.md`](../architect/directory-structure.md) 完全一致 | 逐目录对比 |
| 0.5 | 所有类型文件定义与 [`master-panel-api.md`](../exchange/master-panel-api.md) 字段对齐 | 逐接口对照检查 |
| 0.6 | CSS 变量在浏览器 DevTools 中可见 | 检查 `:root` 和 `.dark` 下变量值正确 |
| 0.7 | `localStorage` 读写封装正常工作 | 手动调用 `storage.get/set/remove` 验证 |
| 0.8 | ESLint/Prettier 无报错 | `bun run format` 执行成功 |
