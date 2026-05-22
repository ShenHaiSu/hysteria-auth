# Hysteria Auth Web 管理面板 — 架构设计总览

> **文档版本**：v1.1  
> **最后更新**：2026-05-22  
> **项目定位**：基于 Vue 3 生态的企业级单页管理后台，为 Hysteria Auth 系统提供 Web 管理控制台  

---

## 1. 设计目标

| 目标 | 说明 |
|------|------|
| **企业级规范性** | 遵循大型前端项目的目录结构、命名约定、分层架构 |
| **可维护性** | 模块化设计，高内聚低耦合，每个功能域独立自治 |
| **多端适配** | 必须同时适配移动端（≥375px 视口）和桌面端（≥1280px 视口）两种主要界面大小，所有页面和组件在两种端上均可正常使用 |
| **国际化** | 完整 i18n 支持，中/英双语，语言包按功能域拆分 |
| **主题适配** | 亮色/暗色双主题，PrimeVue 主题系统与 TailwindCSS dark mode 协同 |
| **设计一致性** | 全站统一设计令牌（色彩/间距/动效/字体），通过 CSS 变量 + TailwindCSS @theme 扩展管理 |
| **安全性** | JWT 认证、路由守卫、角色权限控制、Token 自动刷新 |
| **开发体验** | TypeScript 严格模式、ESLint + Prettier、自动组件注册 |
| **数据交互** | ECharts 图表可视化 + xlsx 表格导入导出 |

---

## 2. 技术栈总览

| 类别 | 技术选型 | 版本 | 选型理由 |
|------|---------|------|---------|
| **框架** | Vue 3 (Composition API) | 3.5.x | 已引入，Composition API + `<script setup>` 为主 |
| **构建工具** | Vite | 8.x | 已引入，极速 HMR |
| **类型系统** | TypeScript | 6.x | 已引入，严格模式 |
| **UI 组件库** | PrimeVue | 4.5.x | 已引入，Material Design 风格，内置 DataTable/Form 等 |
| **CSS 框架** | TailwindCSS | 4.3.x | 已引入，原子化 CSS 快速布局 |
| **状态管理** | Pinia | 3.x | 已引入，Vue 3 官方推荐 |
| **路由** | Vue Router | 5.x | 已引入，分模块路由表 + 分职责守卫 |
| **国际化** | vue-i18n | 11.x | 待引入，Vue 3 官方 i18n 方案 |
| **HTTP 客户端** | axios | 1.x | 待引入，拦截器支持、请求取消、企业级稳定 |
| **图表** | ECharts | 5.5.x | 待引入，高性能 Canvas 渲染，内置暗色主题，节点监控/流量趋势图表 |
| **表格导入导出** | xlsx (SheetJS) | 0.20.x | 待引入，用户/节点/审计数据 Excel 导出，批量创建用户导入 |
| **工具函数** | @vueuse/core | 12.x | 待引入，常用 composables |
| **日期处理** | date-fns | 4.x | 待引入，树摇友好 |
| **包管理** | bun | latest | 已使用 |

---

## 3. 系统架构图

```
┌─────────────────────────────────────────────────────────────────────┐
│                         前端应用 (SPA)                               │
│                                                                     │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────────────┐   │
│  │  Login   │  │Dashboard │  │  Users   │  │  Nodes/Admins/   │   │
│  │  View    │  │  View    │  │  Views   │  │  Audit Views     │   │
│  └────┬─────┘  └────┬─────┘  └────┬─────┘  └────────┬─────────┘   │
│       │             │             │                   │             │
│  ┌────┴─────────────┴─────────────┴───────────────────┴─────────┐  │
│  │                      视图层 (Views)                           │  │
│  └──────────────────────────┬───────────────────────────────────┘  │
│                             │                                       │
│  ┌──────────────────────────┴───────────────────────────────────┐  │
│  │                   组合式函数层 (Composables)                    │  │
│  │  ┌──────────┐ ┌──────────┐ ┌───────────┐ ┌───────────────┐  │  │
│  │  │useAuth   │ │useTheme  │ │useI18n    │ │useECharts     │  │  │
│  │  └──────────┘ └──────────┘ └───────────┘ │useExportExcel │  │  │
│  │                                          └───────────────┘  │  │
│  └──────────────────────────┬───────────────────────────────────┘  │
│                             │                                       │
│  ┌──────────────────────────┴───────────────────────────────────┐  │
│  │                    状态管理层 (Pinia Stores)                    │  │
│  │  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌───────────────┐   │  │
│  │  │authStore │ │userStore │ │nodeStore │ │ appStore      │   │  │
│  │  └──────────┘ └──────────┘ └──────────┘ └───────────────┘   │  │
│  └──────────────────────────┬───────────────────────────────────┘  │
│                             │                                       │
│  ┌──────────────────────────┴───────────────────────────────────┐  │
│  │                      API 服务层 (api/)                         │  │
│  │  ┌──────────────────┐  ┌──────────────────────────────────┐  │  │
│  │  │  axios instance   │  │  API Modules (auth/user/node/…) │  │  │
│  │  │  + interceptors   │  │                                  │  │  │
│  │  └──────────────────┘  └──────────────────────────────────┘  │  │
│  └──────────────────────────┬───────────────────────────────────┘  │
│                             │ HTTPS                                 │
│  ┌──────────────────────────┴───────────────────────────────────┐  │
│  │              Hysteria Auth Master Server                       │  │
│  │              Base URL: https://{master-host}/api/v1           │  │
│  └──────────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────┘
```

---

## 4. 分层架构

```
┌──────────────────────────────────────────────┐
│  View Layer      视图层    pages/views        │  ← 纯展示 + 用户交互
├──────────────────────────────────────────────┤
│  Component Layer 组件层    components/        │  ← 可复用 UI 组件
├──────────────────────────────────────────────┤
│  Composable Layer 逻辑层  composables/        │  ← 可复用逻辑
├──────────────────────────────────────────────┤
│  Store Layer     状态层    stores/            │  ← 全局状态管理
├──────────────────────────────────────────────┤
│  API Layer       服务层    api/               │  ← HTTP 请求封装
├──────────────────────────────────────────────┤
│  Util Layer      工具层    utils/             │  ← 纯函数工具
└──────────────────────────────────────────────┘
```

**核心原则**：
- **单向数据流**：View → Composable → Store → API → Server
- **关注点分离**：视图不直接调用 API，通过 Store/Composable 中转
- **类型安全**：所有 API 响应和 Store 状态均有完整 TypeScript 类型定义

---

## 5. 功能模块映射

根据后端 [`master-panel-api.md`](../exchange/master-panel-api.md) 的功能划分：

| 前端模块 | 对应 API | 路由路径 | 所需角色 |
|---------|----------|---------|---------|
| **认证** | `POST /api/v1/admin/login` | `/login` | 公开 |
| **仪表盘** | `GET /api/v1/admin/dashboard` | `/dashboard` | 全部 |
| **用户管理** | `CRUD /api/v1/users/*` | `/users` | 全部 |
| **节点管理** | `/api/v1/nodes/*` + `/api/v1/admin/nodes/*` | `/nodes` | 全部 |
| **在线管理** | `POST /api/v1/admin/kick-user` | `/users` (内联) | admin+ |
| **管理员管理** | `CRUD /api/v1/admin/admins` | `/admins` | super_admin |
| **审计日志** | `GET /api/v1/admin/audit-logs` | `/audit-logs` | admin+ |

---

## 6. 横切关注点

| 关注点 | 实现方案 | 说明 |
|--------|---------|------|
| **认证** | JWT Token + axios 拦截器 + Pinia authStore | 登录获取 Token，请求自动携带，过期自动跳转 |
| **权限** | 路由 meta.roles + 分职责导航守卫 (auth/perm) + v-permission 指令 | 三重权限保护 |
| **i18n** | vue-i18n + 按模块拆分语言包 | 默认中文，支持英文切换，localStorage 持久化 |
| **主题** | PrimeVue darkModeSelector + TailwindCSS dark class + CSS 变量 | 亮色/暗色双主题，详见 [design-tokens.md](design-tokens.md) |
| **设计令牌** | CSS 自定义属性 + TailwindCSS @theme 扩展 | 全站色彩/间距/动效/字体统一管理 |
| **错误处理** | axios 响应拦截器 + PrimeVue Toast | 统一错误提示，按 error.code 差异化处理 |
| **加载状态** | appStore.globalLoading + PrimeVue ProgressBar | 全局加载指示器 |
| **表单校验** | 自定义 validators + PrimeVue Form 集成 | 前端参数校验 |
| **图表可视化** | ECharts + BaseChart 封装 + useECharts | 节点监控、流量趋势 |
| **表格导出导入** | xlsx + useExportExcel + export-xlsx 工具 | Excel 数据导出，批量用户导入 |

---

## 7. 关联文档

| 文档 | 路径 | 说明 |
|------|------|------|
| 目录结构设计 | [`directory-structure.md`](directory-structure.md) | 完整文件树及职责说明（含路由分文件架构） |
| 核心模块设计 | [`core-design.md`](core-design.md) | 路由/状态/API/i18n/主题/权限/ECharts/xlsx 详细设计 |
| 设计令牌规范 | [`design-tokens.md`](design-tokens.md) | 全站统一色彩/间距/动效/字体/阴影规范 |
| 面板 API 文档 | [`../exchange/master-panel-api.md`](../exchange/master-panel-api.md) | 后端 API 接口参考 |
| 内部 API 参考 | [`../exchange/master-internal-api.md`](../exchange/master-internal-api.md) | 后端内部接口（仅供了解） |
| Agent API 参考 | [`../exchange/agent-api-reference.md`](../exchange/agent-api-reference.md) | Edge Agent 接口（仅供了解） |
