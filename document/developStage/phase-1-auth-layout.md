# Phase 1：认证与布局框架

> **预计工时**：~3 天  
> **前置依赖**：[Phase 0](./phase-0-infrastructure.md) 全部验证通过  
> **关联文档**：[阶段总览](./README.md) · [面板 API](../exchange/master-panel-api.md)

---

## 阶段目标

实现管理员登录、JWT 认证流程、主布局框架（侧边栏 + 顶部栏 + 内容区）、亮/暗色主题切换、角色权限控制。

---

## 任务清单

### 1.1 登录页面

- [ ] `src/views/auth/LoginView.vue` — 登录表单（用户名/密码）、登录按钮、错误提示、加载状态
- [ ] `src/layouts/AuthLayout.vue` — 认证页布局（居中卡片）

### 1.2 主布局

- [ ] `src/layouts/DefaultLayout.vue` — 主布局（sidebar + header + `<RouterView />`）
- [ ] `src/components/layout/AppSidebar.vue` — 侧边导航栏（菜单动态生成、折叠/展开、角色过滤）
- [ ] `src/components/layout/AppHeader.vue` — 顶部栏（用户信息、主题切换按钮、语言切换按钮、登出按钮）
- [ ] `src/components/layout/AppFooter.vue` — 底部栏（版权信息）

### 1.3 通用组件

- [ ] `src/components/common/AppBreadcrumb.vue` — 面包屑导航
- [ ] `src/components/common/AppLoading.vue` — 全局加载指示器
- [ ] `src/components/common/AppEmpty.vue` — 空状态占位
- [ ] `src/components/common/AppError.vue` — 错误状态占位

### 1.4 认证守卫完善

- [ ] `authGuard` — Token 有效性检查 + 管理员信息懒加载 + 401 → `/login`
- [ ] `permissionGuard` — 角色权限检查 + 403 → `/403`

### 1.5 错误页面

- [ ] `src/views/errors/NotFoundView.vue` — 404 页面
- [ ] `src/views/errors/ForbiddenView.vue` — 403 页面

### 1.6 主题切换

- [ ] `themeStore` 完整实现（initTheme / toggleTheme / applyTheme）
- [ ] 侧边栏和头部栏的主题切换按钮
- [ ] `document.documentElement.classList.toggle('dark')` 自动同步 PrimeVue + TailwindCSS

### 1.7 Composables 实现

- [ ] `src/composables/useAuth.ts` — 认证逻辑封装
- [ ] `src/composables/useTheme.ts` — 主题切换逻辑
- [ ] `src/composables/useToast.ts` — Toast 封装
- [ ] `src/composables/useConfirm.ts` — 确认对话框封装
- [ ] `src/composables/usePermission.ts` — 角色权限判断
- [ ] `src/composables/useDebounce.ts` — 防抖函数

---

## 重难点

| 难点 | 说明 | 解决方案 |
|------|------|----------|
| **Token 过期自动处理** | axios 响应拦截器需识别 `token_expired`/`token_invalid` 并自动跳转登录页，同时避免多个请求同时触发多次跳转 | 使用 `authStore` 中的 `isRefreshing` 标志位 + 请求队列机制 |
| **管理员信息懒加载** | 首次进入受保护页面时需先 `fetchAdminInfo()`，但多个守卫可能同时触发 | 在 `authStore` 中使用 Promise 缓存：首次调用时存储 Promise，后续调用返回同一个 Promise |
| **侧边栏菜单动态生成** | 菜单项需从路由配置动态生成，与路由守卫权限保持一致 | 从 `routes` 中找到 `DefaultLayout` 的 `children`，过滤 `hidden !== true` 且角色匹配的项 |
| **侧边栏折叠动画** | 折叠/展开需要平滑过渡，同时菜单文字需要渐隐 | 使用 TailwindCSS `transition-all duration-300` + `overflow-hidden` |

---

## 注意点

1. **登录页不可见侧边栏**：`/login` 路由使用 `AuthLayout`，`/` 下所有子路由使用 `DefaultLayout`
2. **登录后重定向**：登录成功后跳转到 `redirect` query 参数指定的页面，无则默认 `/dashboard`
3. **Token 存储安全**：Token 存储在 `localStorage`，不要使用 `sessionStorage`（用户期望关闭浏览器后保持登录）
4. **侧边栏折叠状态持久化**：存入 `localStorage`，刷新后保持
5. **404 页面必须兜底**：使用 `/:pathMatch(.*)*` 捕获所有未匹配路由

---

## 阶段验证清单

Phase 1 完成后，必须通过以下**全部**验证方可进入 [Phase 2](./phase-2-user-management.md)：

| # | 验证项 | 验证方法 |
|---|--------|----------|
| 1.1 | 未登录访问 `/dashboard` 自动跳转 `/login` | 清除 localStorage 后测试 |
| 1.2 | 输入正确用户名密码能成功登录并跳转仪表盘 | 使用真实后端或 mock 数据 |
| 1.3 | 输入错误密码显示错误提示 | Toast 显示 `invalid_credentials` 消息 |
| 1.4 | 登录后刷新页面保持登录状态 | F5 刷新后仍在仪表盘 |
| 1.5 | 侧边栏菜单正确显示（基于角色） | 分别用 super_admin/admin/readonly 账号测试 |
| 1.6 | 侧边栏折叠/展开正常工作 | 点击折叠按钮，动画平滑 |
| 1.7 | 亮/暗色主题切换正常 | 点击切换按钮，所有元素颜色正确变化 |
| 1.8 | 主题切换后刷新页面保持 | F5 刷新后主题不变 |
| 1.9 | 无权限访问 `/admins`（readonly 角色）跳转 403 | 用 readonly 账号测试 |
| 1.10 | 访问不存在的路由显示 404 页面 | 输入 `/nonexistent` 路径 |
| 1.11 | 登出后清除 Token 并跳转登录页 | 点击登出按钮测试 |
| 1.12 | 页面标题正确显示 `{title} - Hysteria Auth` | 切换不同页面观察浏览器标签 |
| 1.13 | Toast 通知正常显示和自动消失 | 各操作触发 Toast 验证 |
| 1.14 | `bun run type-check` 无错误 | 执行类型检查 |
| 1.15 | `bun run build` 无错误 | 执行生产构建 |
