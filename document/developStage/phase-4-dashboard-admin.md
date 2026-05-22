# Phase 4：仪表盘与管理功能

> **预计工时**：~3 天  
> **前置依赖**：[Phase 3](./phase-3-node-management.md) 全部验证通过  
> **关联文档**：[阶段总览](./README.md) · [面板 API](../exchange/master-panel-api.md)

---

## 阶段目标

实现仪表盘概览页、管理员管理（仅 super_admin）、审计日志查看。

---

## 任务清单

### 4.1 仪表盘页面

- [ ] `src/views/dashboard/DashboardView.vue` — 仪表盘：
  - 统计卡片行：用户总数、活跃用户、在线用户、节点总数、活跃节点
  - 流量概览卡片：今日流量、本月流量
  - `src/components/charts/StatCard.vue` — 统计卡片组件（图标 + 数值 + 标签）
  - （Phase 5 补充流量趋势图表）

### 4.2 仪表盘 Store

- [ ] `src/stores/dashboard.store.ts` — 仪表盘数据获取 + 缓存

### 4.3 管理员管理（仅 super_admin）

- [ ] `src/views/admins/AdminListView.vue` — 管理员列表：
  - PrimeVue DataTable 展示
  - 创建管理员按钮（仅 super_admin 可见）
  - 每行显示：用户名、角色、激活状态、创建时间、上次登录时间
  - 操作按钮：编辑角色、禁用/启用、重置密码
- [ ] `src/views/admins/AdminFormDialog.vue` — 管理员创建/编辑对话框

### 4.4 审计日志

- [ ] `src/views/audit/AuditLogView.vue` — 审计日志列表：
  - PrimeVue DataTable 展示（分页）
  - 筛选器：管理员、操作类型、目标类型、时间范围
  - 每行显示：操作管理员、操作类型、目标类型/ID、详情（JSON 格式化展示）、来源 IP、操作时间
  - `detail` JSON 字符串格式化展示（折叠/展开）

---

## 重难点

| 难点 | 说明 | 解决方案 |
|------|------|----------|
| **仪表盘数据聚合** | 多个数据项从同一个 API 返回，但需要在不同卡片中展示 | `dashboardStore.fetchOverview()` 一次获取，通过 getter 拆分 |
| **管理员自我操作保护** | 管理员不应禁用自己的账号或降低自己的角色 | 前端判断 `admin.id === authStore.adminInfo.id`，对自身隐藏禁用/角色降级操作 |
| **审计日志详情展示** | `detail` 字段为 JSON 字符串，包含变更前后对比，需友好展示 | 使用 `<pre>` + JSON 格式化，或展开面板逐字段对比 |

---

## 注意点

1. **管理员管理路由仅 super_admin 可访问**，路由 meta 中 `roles: ['super_admin']`
2. **仪表盘数据在 API 未就绪时显示占位值**（全为 0 或 `--`），不要报错
3. **审计日志不可删除**，UI 中不应出现删除按钮
4. **上次登录时间**为 `null` 表示从未登录，显示"从未登录"
5. **仪表盘数据刷新**可提供手动刷新按钮，不自动轮询（减少服务器压力）

---

## 阶段验证清单

Phase 4 完成后，必须通过以下**全部**验证方可进入 [Phase 5](./phase-5-charts-export.md)：

| # | 验证项 | 验证方法 |
|---|--------|----------|
| 4.1 | 仪表盘页面正确展示所有统计卡片 | 检查数据与 API 返回值一致 |
| 4.2 | 仪表盘统计卡片图标和颜色正确 | 检查各卡片样式 |
| 4.3 | 管理员列表正确展示（super_admin 可见） | 用 super_admin 登录测试 |
| 4.4 | admin/readonly 角色看不到管理员管理菜单 | 用 admin/readonly 登录测试 |
| 4.5 | 创建管理员成功 | 填写表单提交，列表刷新 |
| 4.6 | 编辑管理员角色/状态成功 | 修改后提交，验证更新 |
| 4.7 | 管理员不能禁用自己 | 确认自身操作的按钮被隐藏 |
| 4.8 | 审计日志列表正常分页 | 翻页测试 |
| 4.9 | 审计日志筛选功能正常 | 各筛选条件测试 |
| 4.10 | 审计日志详情 JSON 可读展示 | 展开详情验证格式 |
| 4.11 | readonly 角色看不到审计日志菜单 | 用 readonly 登录测试 |
