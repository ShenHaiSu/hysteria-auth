# Phase 3：节点管理模块

> **预计工时**：~2 天  
> **前置依赖**：[Phase 2](./phase-2-user-management.md) 全部验证通过  
> **关联文档**：[阶段总览](./README.md) · [面板 API](../exchange/master-panel-api.md) · [Agent API](../exchange/agent-api-reference.md)

---

## 阶段目标

实现节点管理功能，包括节点列表、节点详情、预注册节点、轮换密钥、节点状态历史查看、在线用户管理（踢用户下线）。

---

## 任务清单

### 3.1 节点 Store 完善

- [ ] `src/stores/nodes.store.ts` — 完整实现：
  - `fetchNodes()` — 分页 + 筛选
  - `fetchNodeById(id)` — 获取详情
  - `fetchStatusHistory(id, hours)` — 获取状态历史
  - `preRegisterNode(data)` — 预注册节点
  - `rotateSecret(id)` — 轮换密钥
  - `kickUser(username, nodeId)` — 踢用户下线

### 3.2 节点列表页

- [ ] `src/views/nodes/NodeListView.vue` — 节点列表：
  - PrimeVue DataTable（分页）
  - 筛选器（激活状态、预注册状态）
  - 每行显示：名称、IP、端口、位置、预注册状态、在线状态、最后心跳时间
  - 操作按钮：预注册节点、查看详情、轮换密钥

### 3.3 节点详情页

- [ ] `src/views/nodes/NodeDetailView.vue` — 节点详情：
  - 基本信息卡片（名称/IP/端口/位置/预注册状态/密钥版本）
  - 状态历史区域（时间范围选择器）
  - （Phase 5 补充 CPU/内存/带宽/连接数图表）

### 3.4 预注册节点对话框

- [ ] `src/views/nodes/NodeRegisterDialog.vue` — 预注册对话框：
  - 表单：节点名称、位置、端口、trafficStats 端口
  - 成功后展示：`provisionToken`、`startupCommand`（可复制）
  - 提示令牌为一次性使用，请妥善保存

### 3.5 在线用户管理

- [ ] `src/components/modals/KickUserModal.vue` — 踢用户确认框：
  - 提示"该操作仅断开当前连接，客户端可能会自动重连。建议同时禁用该用户账号"
  - 提供"踢下线并禁用用户"的快捷操作

---

## 重难点

| 难点 | 说明 | 解决方案 |
|------|------|----------|
| **在线状态判断逻辑** | 需要综合 `isActive` + `lastHeartbeat` 时间差判断。`lastHeartbeat` 为 null 时节点从未上线 | 定义常量 `HEARTBEAT_TIMEOUT_MS = 90_000`（90秒），`isActive && lastHeartbeat && (now - lastHeartbeat) < 90s` 为在线 |
| **预注册令牌的一次性特性** | 令牌创建后仅使用一次即失效。前端需在 UI 上清晰传达此信息 | 对话框中使用醒目警告样式，令牌用等宽字体展示，提供一键复制按钮 |
| **轮换密钥的风险提示** | 密钥轮换有过渡期，新旧密钥短期共存 | 确认框中说明过渡期机制，避免管理员困惑 |
| **节点状态历史的时间范围** | 大量历史数据可能影响渲染性能 | 默认显示最近 24 小时，提供小时数选择器，Phase 5 用 ECharts 的 `dataZoom` 支持缩放 |

---

## 注意点

1. **节点 ID 为 string 类型**（如 `edge-node-01`），不是数字
2. **预注册状态**有 `pending`（待注册）和 `provisioned`（已注册）两种，列表需用不同颜色标识
3. **轮换密钥后**需刷新节点详情以获取最新 `secretVersion`
4. **踢用户操作**需要同时提供 `username` 和 `nodeId`，如果后端在用户列表中有在线节点信息则可用
5. **节点心跳时间**格式为 ISO 8601，前端展示时用 `date-fns` 格式化为相对时间（如"30 秒前"）

---

## 阶段验证清单

Phase 3 完成后，必须通过以下**全部**验证方可进入 [Phase 4](./phase-4-dashboard-admin.md)：

| # | 验证项 | 验证方法 |
|---|--------|----------|
| 3.1 | 节点列表正常分页展示 | 翻页测试 |
| 3.2 | 节点在线/离线状态正确显示 | 对比 `lastHeartbeat` 与当前时间 |
| 3.3 | 预注册节点成功，显示令牌和启动命令 | 填写表单提交，验证返回数据展示 |
| 3.4 | 预注册令牌可一键复制 | 点击复制按钮，粘贴验证 |
| 3.5 | 轮换密钥成功 | 点击轮换，确认框确认，密钥版本递增 |
| 3.6 | 节点详情页正确展示所有信息 | 点击节点进入详情 |
| 3.7 | 节点状态历史数据正确展示 | 选择不同时间范围测试 |
| 3.8 | 踢用户下线成功 | 执行踢人操作，验证提示信息 |
| 3.9 | 预注册状态标签颜色正确（pending = 黄色, provisioned = 绿色） | 检查列表 |
| 3.10 | `readonly` 角色看不到预注册和轮换按钮 | 用 readonly 账号测试 |
