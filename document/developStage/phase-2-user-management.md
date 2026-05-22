# Phase 2：用户管理模块

> **预计工时**：~3 天  
> **前置依赖**：[Phase 1](./phase-1-auth-layout.md) 全部验证通过  
> **关联文档**：[阶段总览](./README.md) · [面板 API](../exchange/master-panel-api.md) · [前端规范](../develop/frontend-standards.md)

---

## 阶段目标

实现完整的用户 CRUD 管理功能，包括列表（分页/筛选/搜索）、创建/编辑/删除用户、用户详情查看、流量统计查询、流量重置。

---

## 任务清单

### 2.1 用户 Store 完善

- [ ] `src/stores/users.store.ts` — 完整实现：
  - `fetchUsers()` — 分页 + 筛选 + 搜索
  - `fetchUserById(id)` — 获取详情
  - `createUser(data)` — 创建用户
  - `updateUser(id, data)` — 更新用户（部分更新）
  - `deleteUser(id)` — 删除用户（软删除，带确认）
  - `resetTraffic(id)` — 重置流量（带确认）
  - `fetchTrafficStats(id, period)` — 获取流量统计

### 2.2 用户列表页

- [ ] `src/views/users/UserListView.vue` — 用户列表：
  - PrimeVue DataTable 展示（分页/排序）
  - 搜索框（模糊搜索用户名/邮箱，带防抖）
  - 筛选器（激活状态、节点）
  - 操作按钮：创建用户、查看详情、编辑、删除、重置流量
  - 角色权限控制：`readonly` 隐藏创建/编辑/删除按钮
- [ ] `src/components/common/AppTrafficText.vue` — 流量格式化展示组件（自动 bytes→KB/MB/GB/TB 换算）
- [ ] `src/components/common/AppStatusBadge.vue` — 状态标签组件

### 2.3 用户创建/编辑对话框

- [ ] `src/views/users/UserFormDialog.vue` — 用户表单对话框：
  - 创建模式 / 编辑模式（通过 props 控制）
  - 表单字段：用户名、密码、邮箱、总流量配额、是否激活、过期时间、允许节点（多选）、备注
  - 表单校验（`src/utils/validators.ts` 中的规则）
  - 提交中 Loading 状态
  - 成功后关闭对话框 + Toast 提示 + 列表刷新

### 2.4 用户详情页

- [ ] `src/views/users/UserDetailView.vue` — 用户详情：
  - 基本信息卡片（用户名/邮箱/流量使用/状态/过期时间/节点/备注）
  - 流量统计区域（周期选择器：日/周/月/全部）
  - （Phase 5 补充图表）

### 2.5 删除确认对话框

- [ ] `src/components/modals/ConfirmModal.vue` — 通用确认对话框封装

---

## 重难点

| 难点 | 说明 | 解决方案 |
|------|------|----------|
| **`allowedNodes` 字段的双向转换** | 后端存储为 JSON 字符串，前端需要 `string[]`。创建时需将数组序列化，读取时需解析 | 在 `user.adapter.ts` 中统一处理，Store 和 View 只操作 `string[]` |
| **部分更新（PATCH 语义用 PUT 实现）** | 后端 PUT 支持部分更新（未传字段保持不变），前端确保只发送变更字段 | 表单提交时用 `lodash.omitBy` 或手动过滤 `undefined` 字段 |
| **流量数据的数字精度** | `totalTrafficBytes` 可能非常大（TB 级别），JS `number` 安全整数为 `2^53-1`，远超实际需求但需注意 | 使用 `number` 类型即可（TB 级在安全范围内），展示时通过 `useTrafficFormat` 换算 |
| **搜索防抖** | 用户输入搜索关键词时不应每次按键都触发请求 | 使用 `useDebounce` composable，300ms 延迟 |

---

## 注意点

1. **删除用户为软删除**：后端将 `isActive` 设为 `false`，前端需在确认框中明确告知用户此操作的影响
2. **流量重置不可逆**：确认框中必须强调"此操作不可恢复"
3. **创建用户的默认值**：`isActive` 默认 `true`，`totalTrafficBytes` 默认 `0`（不限）
4. **编辑用户不强制传密码**：编辑模式下密码字段为空表示不修改密码
5. **列表数据刷新策略**：创建/编辑/删除成功后自动刷新当前页，不要跳回第一页
6. **`readonly` 角色的按钮隐藏**：使用 `v-permission` 指令控制，不要仅靠 CSS 隐藏

---

## 阶段验证清单

Phase 2 完成后，必须通过以下**全部**验证方可进入 [Phase 3](./phase-3-node-management.md)：

| # | 验证项 | 验证方法 |
|---|--------|----------|
| 2.1 | 用户列表正常分页展示 | 翻页测试，数据正确加载 |
| 2.2 | 搜索功能正常（模糊匹配用户名/邮箱） | 输入关键词，列表实时筛选 |
| 2.3 | 筛选功能正常（激活状态、节点） | 切换筛选条件，列表正确响应 |
| 2.4 | 创建用户成功 | 填写完整表单提交，列表出现新用户 |
| 2.5 | 创建用户表单校验正常工作 | 不填必填项提交，显示校验错误 |
| 2.6 | 编辑用户成功 | 修改字段后提交，详情正确更新 |
| 2.7 | 删除用户弹出确认框 | 点击删除，确认框出现，点击确认后用户消失 |
| 2.8 | 重置流量弹出确认框 | 点击重置，确认框出现，确认后流量归零 |
| 2.9 | 用户详情页正确展示所有信息 | 点击用户进入详情，字段正确 |
| 2.10 | `readonly` 角色看不到创建/编辑/删除按钮 | 用 readonly 账号登录测试 |
| 2.11 | 流量数值正确格式化显示（KB/MB/GB/TB） | 检查不同数量级流量的显示 |
| 2.12 | 操作后 Toast 提示正确 | 创建/编辑/删除后均有成功/失败提示 |
| 2.13 | Loading 状态正常显示 | 网络慢时显示加载指示器 |
