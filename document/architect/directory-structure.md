# 目录结构设计

> **所属文档**: [架构设计总览](README.md)  
> **最后更新**: 2026-05-22  

---

## 1. 完整目录树

```
web-dev/
├── .vscode/                        # VS Code 工作区配置
│   ├── extensions.json             # 推荐插件列表
│   └── settings.json               # 编辑器设置（格式化、i18n-ally 等）
├── document/                       # 项目文档（非构建产物）
│   ├── architect/                  # 架构设计文档
│   │   ├── README.md               # 架构总览
│   │   ├── directory-structure.md  # 本文件
│   │   ├── core-design.md          # 核心模块详细设计
│   │   └── design-tokens.md        # 设计令牌规范（色彩/间距/动效/字体）
│   ├── develop/                    # 开发手册（待补充）
│   ├── developStage/               # 开发阶段记录（待补充）
│   ├── exchange/                   # 后端 API 文档
│   │   ├── master-panel-api.md     # 面板 API（核心对接）
│   │   ├── master-internal-api.md  # 内部 API 参考
│   │   └── agent-api-reference.md  # Agent API 参考
│   └── module/                     # 模块说明（待补充）
├── public/                         # 静态资源（不经构建处理）
│   └── favicon.ico                 # 网站图标
├── src/                            # ★ 应用源码
│   ├── api/                        # ★ API 服务层
│   │   ├── index.ts                # axios 实例创建 + 拦截器配置
│   │   ├── types.ts                # 通用 API 类型（分页、错误响应等）
│   │   ├── modules/                # 按后端功能域分模块
│   │   │   ├── auth.ts             # 认证 API（登录）
│   │   │   ├── dashboard.ts        # 仪表盘 API
│   │   │   ├── users.ts            # 用户管理 API
│   │   │   ├── nodes.ts            # 节点管理 API
│   │   │   ├── admins.ts           # 管理员管理 API
│   │   │   └── audit-logs.ts       # 审计日志 API
│   │   └── adapters/               # 数据适配器（API 响应 → 前端 Model）
│   │       ├── user.adapter.ts
│   │       ├── node.adapter.ts
│   │       └── admin.adapter.ts
│   ├── assets/                     # 静态资源（经构建处理）
│   │   ├── images/                 # 图片资源
│   │   │   ├── logo-light.svg      # 亮色主题 Logo
│   │   │   └── logo-dark.svg       # 暗色主题 Logo
│   │   └── styles/                 # 样式文件
│   │       ├── public.css          # TailwindCSS 入口 + 全局基础样式
│   │       ├── variables.css       # CSS 自定义属性 + @theme 扩展（来自 design-tokens 规范）
│   │       ├── transition.css      # 组件过渡动画 + 路由加载浮窗动画（路由过渡已禁用）
│   │       └── scrollbar.css       # 自定义滚动条样式
│   ├── components/                 # ★ 全局共享组件（被 2+ 视图/布局使用）
│   │   └── common/                 # 通用基础组件
│   │       ├── AppEmpty.vue        # 空状态占位
│   │       ├── AppError.vue        # 错误状态占位
│   │       ├── AppLoading.vue      # 加载状态占位
│   │       ├── AppStatusBadge.vue  # 状态标签（激活/禁用/在线/离线）
│   │       └── AppTrafficText.vue  # 流量格式化展示（自动单位换算）
│   ├── composables/                # ★ 组合式函数（可复用逻辑）
│   │   ├── useAuth.ts              # 认证逻辑（login/logout/token）
│   │   ├── useTheme.ts             # 主题切换（亮色/暗色）
│   │   ├── useI18n.ts              # 国际化辅助（语言切换）
│   │   ├── usePagination.ts        # 分页逻辑
│   │   ├── useConfirm.ts           # PrimeVue ConfirmationService 封装
│   │   ├── useToast.ts             # PrimeVue ToastService 封装
│   │   ├── useDebounce.ts          # 防抖
│   │   ├── useTrafficFormat.ts     # 流量单位格式化
│   │   ├── usePermission.ts        # 角色权限判断
│   │   ├── useECharts.ts           # ECharts 实例管理（init / setOption / resize / dispose）
│   │   └── useExportExcel.ts       # Excel 导出逻辑（基于 xlsx 库）
│   ├── directives/                 # 自定义指令
│   │   └── permission.ts           # v-permission 角色权限指令
│   ├── layouts/                    # 布局模板
│   │   ├── DefaultLayout.vue       # 默认管理布局（含侧边栏）
│   │   ├── AuthLayout.vue          # 认证页布局（居中卡片）
│   │   └── DefaultLayout/
│   │       └── components/         # DefaultLayout 独占布局组件
│   │           ├── AppSidebar.vue  # 侧边导航栏
│   │           ├── AppHeader.vue   # 顶部栏
│   │           ├── AppFooter.vue   # 底部栏
│   │           └── AppBreadcrumb.vue # 面包屑导航
│   ├── locales/                    # ★ 国际化语言包
│   │   ├── index.ts                # vue-i18n 实例创建 + 配置
│   │   ├── zh-CN/                  # 简体中文
│   │   │   ├── index.ts            # 中文语言包入口（聚合）
│   │   │   ├── common.json         # 通用文案（按钮、提示、状态）
│   │   │   ├── auth.json           # 认证页文案
│   │   │   ├── dashboard.json      # 仪表盘文案
│   │   │   ├── users.json          # 用户管理文案
│   │   │   ├── nodes.json          # 节点管理文案
│   │   │   ├── admins.json         # 管理员管理文案
│   │   │   ├── audit.json          # 审计日志文案
│   │   │   └── validation.json     # 表单校验文案
│   │   └── en-US/                  # 英文
│   │       ├── index.ts
│   │       └── ... (与 zh-CN 结构对称)
│   ├── router/                     # ★ 路由配置
│   │   ├── index.ts                # 路由聚合入口：创建 router 实例 + 注册所有 guards
│   │   ├── routes/                 # 按功能模块拆分的路由表
│   │   │   ├── index.ts            # 路由表聚合（合并所有模块路由为 RouteRecordRaw[]）
│   │   │   ├── auth.route.ts       # 认证路由（/login）
│   │   │   ├── dashboard.route.ts  # 仪表盘路由
│   │   │   ├── users.route.ts      # 用户管理路由（列表 + 详情）
│   │   │   ├── nodes.route.ts      # 节点管理路由（列表 + 详情）
│   │   │   ├── admins.route.ts     # 管理员管理路由
│   │   │   ├── audit.route.ts      # 审计日志路由
│   │   │   └── errors.route.ts     # 错误页面路由（403 / 404）
│   │   └── guards/                 # 导航守卫（每个守卫独立文件）
│   │       ├── index.ts            # 守卫注册入口：setupRouterGuards(router) 聚合调用
│   │       ├── title.guard.ts      # 页面标题守卫
│   │       ├── auth.guard.ts       # 认证守卫（Token 检查 + 401 重定向）
│   │       └── permission.guard.ts # 权限守卫（角色检查 + 403 重定向）
│   ├── stores/                     # ★ Pinia 状态管理
│   │   ├── app.store.ts            # 应用级状态（侧边栏折叠、全局加载、语言）
│   │   ├── auth.store.ts           # 认证状态（Token、管理员信息、角色）
│   │   ├── theme.store.ts          # 主题状态（亮/暗色、主题持久化）
│   │   ├── users.store.ts          # 用户管理状态（列表、详情、筛选）
│   │   ├── nodes.store.ts          # 节点管理状态（列表、详情、状态历史）
│   │   └── dashboard.store.ts      # 仪表盘状态（概览数据）
│   ├── types/                      # ★ TypeScript 类型定义
│   │   ├── api.types.ts            # API 通用类型（分页请求/响应、错误体）
│   │   ├── auth.types.ts           # 认证相关类型（登录请求/响应、管理员信息）
│   │   ├── user.types.ts           # 用户相关类型
│   │   ├── node.types.ts           # 节点相关类型
│   │   ├── admin.types.ts          # 管理员类型
│   │   ├── dashboard.types.ts      # 仪表盘类型
│   │   ├── audit.types.ts          # 审计日志类型
│   │   └── common.types.ts         # 通用类型（选项、枚举等）
│   ├── utils/                      # ★ 工具函数
│   │   ├── format.ts               # 格式化（日期、文件大小、数字）
│   │   ├── validators.ts           # 表单校验规则
│   │   ├── storage.ts              # localStorage/sessionStorage 封装
│   │   ├── constants.ts            # 常量定义（角色枚举、状态枚举等）
│   │   └── export-xlsx.ts          # Excel 导出工具函数（基于 xlsx 库）
│   └── views/                      # ★ 页面视图（按功能模块划分）
│       ├── auth/                   # 认证模块
│       │   └── LoginView.vue       # 登录页
│       ├── dashboard/              # 仪表盘模块
│       │   ├── DashboardView.vue   # 系统概览页
│       │   └── components/         # 仪表盘独占组件
│       │       └── StatCard.vue    # 统计卡片
│       ├── users/                  # 用户管理模块
│       │   ├── UserListView.vue    # 用户列表页
│       │   ├── UserDetailView.vue  # 用户详情页（含流量统计图表）
│       │   └── UserFormDialog.vue  # 用户创建/编辑对话框
│       ├── nodes/                  # 节点管理模块
│       │   ├── NodeListView.vue    # 节点列表页
│       │   ├── NodeDetailView.vue  # 节点详情页（含状态历史图表、配置编辑）
│       │   ├── NodeRegisterDialog.vue # 预注册节点对话框
│       │   └── components/         # 节点管理独占组件
│       │       ├── KickUserModal.vue # 踢用户确认弹窗
│       │       └── NodeConfigForm.vue # Phase 7: Hysteria 2 配置编辑表单
│       ├── admins/                 # 管理员管理模块
│       │   ├── AdminListView.vue   # 管理员列表页
│       │   └── AdminFormDialog.vue # 管理员创建/编辑对话框
│       ├── audit/                  # 审计日志模块
│       │   └── AuditLogView.vue    # 审计日志列表页
│       └── errors/                 # 错误页面
│           ├── NotFoundView.vue    # 404 页面
│           └── ForbiddenView.vue   # 403 页面
├── App.vue                         # 根组件
├── main.ts                         # 应用入口
├── env.d.ts                        # Vite 环境类型声明
├── components.d.ts                 # 自动组件注册类型声明（自动生成）
├── index.html                      # HTML 入口
├── package.json                    # 项目依赖
├── bun.lock                        # 依赖锁定
├── vite.config.ts                  # Vite 配置
├── tsconfig.json                   # TS 根配置
├── tsconfig.app.json               # TS 应用配置
├── tsconfig.node.json              # TS Node 端配置
├── .prettierrc.json                # Prettier 配置
├── .gitignore                      # Git 忽略
└── README.md                       # 项目说明
```

---

## 2. 目录职责详解

### 2.1 `src/api/` — API 服务层

**职责**：封装所有对后端的 HTTP 请求，上层不得直接调用 `axios`。

**文件职责**：

| 文件 | 职责 |
|------|------|
| [`index.ts`](core-design.md#31-axios-实例配置) | 创建 axios 实例，配置 baseURL、超时、请求/响应拦截器 |
| `types.ts` | 定义 `PageRequest`、`PageResponse<T>`、`ApiError` 等通用结构 |
| `adapters/*.ts` | 将 API 返回的原始数据（如 `allowedNodes` JSON 字符串）转换为前端友好的结构 |
| `modules/*.ts` | 每个文件对应后端一个功能域，导出该域所有 API 函数 |

**设计原则**：
- 每个 API 函数签名明确：输入 → Promise<输出>
- 不使用 Magic String，所有端点路径集中管理
- 返回类型完整，利用 TypeScript 泛型

### 2.2 `src/stores/` — 状态管理层

**职责**：管理全局共享状态，作为视图层与 API 层之间的桥梁。

**设计原则**：
- 每个 Store 对应一个功能域
- 使用 Pinia Setup Store 语法（`defineStore` + Composition API）
- Store 不直接操作 DOM，仅管理数据和调用 API
- 列表 Store 统一封装分页、筛选、搜索状态

### 2.3 `src/composables/` — 组合式函数层

**职责**：封装可复用的有状态/无状态逻辑。

**设计原则**：
- 函数名统一 `use` 前缀
- 返回响应式状态 + 操作方法
- 优先使用 `@vueuse/core` 已有能力

| 文件 | 职责 |
|------|------|
| [`useAuth.ts`](core-design.md#22-authstore--认证状态) | 认证逻辑（login/logout/token 管理） |
| `useTheme.ts` | 主题切换（亮色/暗色），桥接 themeStore → DOM |
| `useI18n.ts` | 国际化辅助（语言切换持久化） |
| `usePagination.ts` | 通用分页逻辑（page/pageSize/total → computed offset） |
| `useConfirm.ts` | PrimeVue ConfirmationService 封装（统一 confirm 调用） |
| `useToast.ts` | PrimeVue ToastService 封装（统一 toast 调用） |
| `useDebounce.ts` | 防抖函数 |
| `useTrafficFormat.ts` | 流量单位格式化（bytes → KB/MB/GB/TB 自动换算） |
| `usePermission.ts` | 角色权限判断（基于 authStore.role） |
| `useECharts.ts` | ECharts 实例生命周期管理（init / setOption / resize / dispose） |
| `useExportExcel.ts` | Excel 导出逻辑（将数据数组 + 列定义 → .xlsx Blob 下载） |

### 2.4 `src/views/` — 页面视图层

**职责**：纯展示逻辑，组合组件 + composables + stores。

**设计原则**：
- 每个功能模块一个子目录
- 页面组件命名：`{Module}{Action}View.vue`
- 对话框组件命名：`{Module}{Action}Dialog.vue`
- 使用 `<script setup lang="ts">` 语法

### 2.5 `src/components/` — 全局共享组件层

**职责**：存放被 **2 个及以上** 布局/视图共享的 UI 组件。仅被单一消费者使用的组件归置到消费者同级的 `components/` 子目录（就近归置原则）。

| 子目录 | 职责 | 复用范围 |
|--------|------|---------|
| `common/` | 通用基础组件（状态占位、格式化展示、状态标签） | 全局 |

**各模块独占组件的位置**：

| 消费者 | 独占组件位置 | 当前包含组件 |
|--------|------------|------------|
| `DefaultLayout` | `src/layouts/DefaultLayout/components/` | `AppSidebar`, `AppHeader`, `AppFooter`, `AppBreadcrumb` |
| `DashboardView` | `src/views/dashboard/components/` | `StatCard` |
| `Nodes` 模块 | `src/views/nodes/components/` | `KickUserModal` |

### 2.6 `src/router/` — 路由配置层

**职责**：管理 SPA 路由表与导航守卫，按功能模块与守卫类型拆分文件。

**核心架构**：

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
    ├── index.ts              ← 聚合注册：依次注册 title / auth / permission 守卫
    ├── title.guard.ts        ← 页面标题守卫（document.title 设置）
    ├── auth.guard.ts         ← 认证守卫（Token 检查 + 401 重定向 + 用户信息懒加载）
    └── permission.guard.ts   ← 权限守卫（角色检查 + 403 重定向）
```

**设计原则**：
- **路由表**：按功能域独立文件，`routes/index.ts` 聚合后再由 `router/index.ts` 引入
- **守卫**：每个守卫独立文件，负责单一职责；`guards/index.ts` 统一注册到 router 实例
- 新增模块时只需新建 `xxx.route.ts` 并在 `routes/index.ts` 中引入，无需修改其他文件

### 2.7 `src/types/` — 类型定义层

**职责**：集中管理所有 TypeScript 类型/接口定义。

**设计原则**：
- 类型文件按功能域划分，与 API 模块 / Store 一一对应
- 优先使用 `interface`，枚举使用 union type
- 所有 API DTO 必须有对应类型定义

### 2.8 `src/locales/` — 国际化层

**职责**：管理多语言文案。

**设计原则**：
- 中英双语，按功能域拆分 JSON 文件
- key 命名规范：`{domain}.{section}.{key}` 如 `users.table.columns.username`
- 所有面向用户的文案（包括 Toast 提示）必须国际化

### 2.9 `src/utils/` — 工具函数层

**职责**：纯函数工具，无状态、无 DOM 操作。

| 文件 | 职责 |
|------|------|
| `format.ts` | 格式化函数（日期、文件大小、数字） |
| `validators.ts` | 表单校验规则集合 |
| `storage.ts` | localStorage/sessionStorage 类型安全封装 |
| `constants.ts` | 常量定义（角色枚举、状态枚举、分页默认值等） |
| `export-xlsx.ts` | Excel 导出工具（基于 `xlsx` 库，接收列定义 + 数据 → 触发 .xlsx 下载） |

---

## 3. 命名规范

### 3.1 文件命名

| 类型 | 规范 | 示例 |
|------|------|------|
| 页面组件 | PascalCase + `View` 后缀 | `UserListView.vue` |
| 对话框组件 | PascalCase + `Dialog` 后缀 | `UserFormDialog.vue` |
| 通用组件 | PascalCase + `App` 前缀 | `AppStatusBadge.vue` |
| Composables | camelCase + `use` 前缀 | `useAuth.ts` |
| Stores | camelCase + `.store` 后缀 | `auth.store.ts` |
| API 模块 | kebab-case（与后端域对齐） | `users.ts` |
| 路由表文件 | kebab-case + `.route` 后缀 | `users.route.ts` |
| 守卫文件 | kebab-case + `.guard` 后缀 | `auth.guard.ts` |
| 类型文件 | kebab-case + `.types` 后缀 | `user.types.ts` |
| 语言包 | kebab-case（与视图域对齐） | `users.json` |
| 工具函数 | kebab-case | `export-xlsx.ts` |

### 3.2 变量/函数命名

| 元素 | 规范 | 示例 |
|------|------|------|
| 组件 Props | camelCase | `isActive`, `totalTrafficBytes` |
| 组件 Events | kebab-case | `@update:model-value`, `@user-deleted` |
| Store state | camelCase | `userList`, `currentUser` |
| Store actions | camelCase + 动词 | `fetchUsers()`, `deleteUser()` |
| Store getters | camelCase + 名词 | `activeUsers`, `filteredNodes` |
| API 函数 | camelCase + 动词 | `getUsers()`, `createUser()` |
| Guard 函数 | camelCase + `guard` 前缀 | `authGuard()`, `permissionGuard()` |

### 3.3 CSS 类名

- **TailwindCSS** 优先于自定义 CSS 类
- 自定义类名使用 kebab-case
- 在 `<style scoped>` 中仅定义 TailwindCSS 无法表达或复用度高的样式
- 涉及色彩/间距/动效的值必须引用 [`design-tokens.md`](design-tokens.md) 中定义的 Token

---

## 4. 路径别名

通过 `vite.config.ts` 中已配置：

```ts
resolve: {
  alias: {
    '@': '/src',
  },
}
```

| 别名 | 实际路径 | 使用示例 |
|------|---------|---------|
| `@/` | `src/` | `import { useAuth } from '@/composables/useAuth'` |
| `@/api` | `src/api/` | `import { userApi } from '@/api/modules/users'` |
| `@/stores` | `src/stores/` | `import { useAuthStore } from '@/stores/auth.store'` |
| `@/composables` | `src/composables/` | `import { useTheme } from '@/composables/useTheme'` |
| `@/components` | `src/components/` | `import { AppLoading } from '@/components/common/AppLoading.vue'` |
| `@/views` | `src/views/` | 路由懒加载中使用 |
| `@/types` | `src/types/` | `import type { UserDto } from '@/types/user.types'` |
| `@/utils` | `src/utils/` | `import { formatTraffic } from '@/utils/format'` |
| `@/assets` | `src/assets/` | `<img src="@/assets/images/logo-light.svg" />` |
