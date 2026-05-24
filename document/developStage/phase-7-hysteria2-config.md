# Phase 7：Hysteria 2 配置自动管理

> **预计工时**：~2 天  
> **前置依赖**：[Phase 6](./phase-6-i18n-delivery.md) 全部验证通过（国际化框架就绪），[Phase 3](./phase-3-node-management.md) 节点管理模块已就绪  
> **关联文档**：[阶段总览](./README.md) · [增量 API 参考](../exchange/phase-7-incremental-api.md) · [面板 API](../exchange/master-panel-api.md) · [核心模块设计](../architect/core-design.md)

---

## 阶段目标

实现管理员通过面板修改节点 Hysteria 2 配置的完整前端链路：
1. 在节点详情页新增 **配置编辑表单**（按分类 Tab 组织），覆盖监听/混淆/拥塞控制/伪装/DNS/运营管理 6 大板块
2. 在节点列表和详情页 **展示 Phase 7 新增字段**（`configVersion`、`listenPort`、`domainName`、`remark` 等）
3. 更新 TypeScript 类型定义、API 模块、Pinia Store，确保类型安全
4. 更新中英文国际化语言包

---

## 核心机制理解

```
管理员面板 PUT /api/v1/admin/nodes/{nodeId}/config
        │
        ▼
Master: ConfigVersion++（Node 表自增）
        │
        ▼
Edge Agent 心跳 POST /api/v1/nodes/{nodeId}/heartbeat
        │  ← 响应中携带 configVersion
        │  检测到本地版本 < Master 版本
        ▼
Edge Agent: GET /api/v1/nodes/{nodeId}/config
        │  ← 响应中携带 configYaml（完整 YAML 字符串）
        ▼
写入 /etc/hysteria/config.yaml → 重载 Hysteria 2 服务
```

> **前端职责边界**：前端仅负责通过 `PUT /api/v1/admin/nodes/{nodeId}/config` 提交配置变更。配置版本号自增、YAML 生成、Agent 拉取与热重载均由后端自动完成。前端无需（也不应该）关心 YAML 内容和 Agent 交互。

---

## 任务清单

### 7.1 TypeScript 类型定义更新

**文件**：[`src/types/node.types.ts`](../../src/types/node.types.ts)

- [ ] 新增联合类型：`ObfsType`、`CongestionControl`、`MasqueradeType`、`ResolverType`
- [ ] `NodeDto` 新增 ~28 个 Phase 7 字段（分为监听、混淆、拥塞控制、带宽、UDP、伪装、DNS、运营管理 8 组），全部可选
- [ ] `NodeDetail` 继承 `NodeDto` 的 Phase 7 字段（无需额外定义），`trafficStatsSecret` 类型标注脱敏
- [ ] `PreRegisterNodeRequest` 新增 `listenPort`、`domainName`、`remark` 3 个可选字段
- [ ] **新增** `UpdateNodeConfigRequest` 接口（对应 PUT 请求体，所有字段可选，`null` 表示不修改）

> 完整字段速查见 [增量 API 参考 §附录](../exchange/phase-7-incremental-api.md#附录完整-phase-7-nodedto-字段速查) 和 [增量 API 参考 §2.1](../exchange/phase-7-incremental-api.md#21-put-apiv1adminnodesnodeidconfig--更新节点配置)。

### 7.2 API 模块更新

**文件**：[`src/api/modules/nodes.ts`](../../src/api/modules/nodes.ts)

- [ ] 新增 `updateConfig(id: string, data: UpdateNodeConfigRequest)` 方法
- [ ] 导入 `UpdateNodeConfigRequest` 类型

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

### 7.3 Pinia Store 更新

**文件**：[`src/stores/nodes.store.ts`](../../src/stores/nodes.store.ts)

- [ ] 新增 `updateNodeConfig(id: string, data: UpdateNodeConfigRequest)` action
- [ ] 调用 `nodeApi.updateConfig()` → 成功后自动刷新当前节点详情（`fetchNodeDetail(id)`）
- [ ] 导入 `UpdateNodeConfigRequest` 类型

```typescript
// src/stores/nodes.store.ts (Phase 7 追加)

import type { UpdateNodeConfigRequest } from '@/types/node.types'

async function updateNodeConfig(id: string, data: UpdateNodeConfigRequest) {
  isSubmitting.value = true
  try {
    await nodeApi.updateConfig(id, data)
    // 配置更新后自动刷新节点详情以获取最新 configVersion 等
    await fetchNodeDetail(id)
  } finally {
    isSubmitting.value = false
  }
}
```

### 7.4 节点列表页扩展

**文件**：[`src/views/nodes/NodeListView.vue`](../../src/views/nodes/NodeListView.vue)

- [ ] DataTable 新增可选的展示列：`configVersion`（配置版本）、`listenPort`（监听端口）、`domainName`（域名）
- [ ] 上述列为**默认隐藏**，通过 PrimeVue DataTable 的列选择器（Column Toggler）按需显示
- [ ] 已有的列无需修改

> **设计原则**：列表页不宜一次性展示过多新增字段，否则在移动端造成信息过载。`configVersion`、`listenPort`、`domainName` 作为最重要的 3 个字段，通过列选择器按需展示。

### 7.5 节点详情页扩展

**文件**：[`src/views/nodes/NodeDetailView.vue`](../../src/views/nodes/NodeDetailView.vue)

- [ ] **基本信息卡片**：在现有字段基础上，新增展示以下字段（全部可选，`null` 时显示 `-`）：
  - `configVersion` — 配置版本号
  - `configUpdatedAt` — 最后配置更新时间（`date-fns` 格式化）
  - `listenPort` — Hysteria 2 监听端口
  - `domainName` — 关联域名
  - `remark` — 节点备注

- [ ] **配置版本区域**（仅 admin+ 可见）：展示 `configVersion` 和 `configUpdatedAt`，与已有"密钥信息"区域同级

- [ ] **「编辑配置」按钮**（仅 admin+ 可见）：点击打开 `NodeConfigForm.vue` 对话框

### 7.6 节点配置编辑表单（新建组件）

**文件**：`src/views/nodes/components/NodeConfigForm.vue`

> 按就近归置原则，该组件仅被 `NodeDetailView.vue` 使用，故放在 `src/views/nodes/components/`。

#### 7.6.1 表单结构

按以下 6 个 Tab 页组织（使用 PrimeVue `Tabs` 组件）：

| Tab | 包含字段 | 说明 |
|-----|---------|------|
| **监听与端口** | `listenAddress`, `listenPort`, `enablePortHopping`, `portHopRangeStart`, `portHopRangeEnd` | 端口跳跃开启时显示起止端口输入框 |
| **混淆与拥塞控制** | `obfsType`, `obfsPassword`, `congestionControl`, `brutalTxBandwidth` | 拥塞控制选 `brutal` 时显示带宽输入框 |
| **带宽与速度测试** | `bandwidthUp`, `bandwidthDown`, `ignoreClientBandwidth`, `enableSpeedTest`, `speedTestPingInterval` | `enableSpeedTest` 开启时显示 ping 间隔 |
| **UDP 与协议嗅探** | `udpIdleTimeout`, `sniffEnabled`, `sniffTimeout`, `sniffRespectHttps` | `sniffEnabled` 开启时显示嗅探参数 |
| **伪装** | `masqueradeType`, `masqueradeFile`, `masqueradeProxyUrl`, `masqueradeStringContent`, `masqueradeStringHeaders`, `masqueradeStringStatusCode` | 根据 `masqueradeType` 动态显示对应字段 |
| **DNS 与运营** | `resolverType`, `resolverTcpAddr`, `resolverUdpAddr`, `resolverTlsAddr`, `serverCost`, `billingCycle`, `expirationDate`, `domainName`, `remark` | 根据 `resolverType` 动态显示对应地址字段 |

#### 7.6.2 交互设计

- **Props**：
  - `visible: boolean` — 对话框可见性
  - `nodeId: string` — 当前节点 ID
  - `currentConfig: NodeDto` — 当前节点完整数据（用于回填表单初始值）

- **Emits**：
  - `update:visible` — 关闭对话框
  - `config-updated` — 配置更新成功通知父组件刷新

- **表单行为**：
  - 打开对话框时，通过 `currentConfig` 回填所有字段当前值
  - 每个 Tab 内提供「重置为默认值」按钮（仅重置当前 Tab 的字段为默认值，不清空）
  - 提交时仅发送**用户实际修改过的字段**（与初始值对比，未修改的字段不包含在请求体中）
  - 提交前做基本前端校验：
    - `listenPort` 范围 1～65535
    - `portHopRangeStart` < `portHopRangeEnd`
    - `brutalTxBandwidth` > 0（当 congestionControl = brutal 时）
    - `speedTestPingInterval` > 0（当 enableSpeedTest = true 时）
    - `serverCost` ≥ 0
  - 提交成功后：
    - Toast 成功提示 → `emit('config-updated')` → 父组件重新 `fetchNodeDetail(id)`
    - 关闭对话框
  - 提交失败：
    - 保留表单数据不清空
    - Toast 错误提示

#### 7.6.3 条件显隐逻辑

```
enablePortHopping === true
  → 显示 portHopRangeStart, portHopRangeEnd
  → 关闭时请求体自动设置 portHopRangeStart=null, portHopRangeEnd=null (后端不生成端口跳跃配置)

congestionControl === 'brutal'
  → 显示 brutalTxBandwidth (bps)

enableSpeedTest === true
  → 显示 speedTestPingInterval

sniffEnabled === true
  → 显示 sniffTimeout, sniffRespectHttps

masqueradeType === 'file'
  → 显示 masqueradeFile
masqueradeType === 'proxy'
  → 显示 masqueradeProxyUrl
masqueradeType === 'string'
  → 显示 masqueradeStringContent, masqueradeStringHeaders, masqueradeStringStatusCode

resolverType 为 'tcp'/'udp'/'tls'
  → 分别显示 resolverTcpAddr / resolverUdpAddr / resolverTlsAddr
```

#### 7.6.4 提交差异计算

```typescript
// 计算实际变更的字段（仅发送与初始值不同的字段）
function computeDiff(original: NodeDto, current: UpdateNodeConfigRequest): UpdateNodeConfigRequest {
  const diff: UpdateNodeConfigRequest = {}
  const allKeys = Object.keys(current) as (keyof UpdateNodeConfigRequest)[]
  
  for (const key of allKeys) {
    if (current[key] !== original[key as keyof NodeDto]) {
      diff[key] = current[key]
    }
  }
  return diff
}
```

### 7.7 国际化语言包更新

**文件**：[`src/locales/zh-CN/nodes.json`](../../src/locales/zh-CN/nodes.json) 和 [`src/locales/en-US/nodes.json`](../../src/locales/en-US/nodes.json)

新增 ~25 个国际化 key，按如下结构组织：

```json
// nodes.json 新增字段
{
  "config": {
    "title": "Hysteria 2 配置",
    "editButton": "编辑配置",
    "resetTab": "重置为默认值",
    "tab": {
      "listener": "监听与端口",
      "obfs": "混淆与拥塞控制",
      "bandwidth": "带宽与速度测试",
      "udpSniff": "UDP 与协议嗅探",
      "masquerade": "伪装",
      "dnsOps": "DNS 与运营"
    },
    "fields": {
      "listenAddress": { "label": "监听地址", "default": "0.0.0.0" },
      "listenPort": { "label": "监听端口", "default": "6789" },
      "enablePortHopping": { "label": "启用端口跳跃" },
      "portHopRangeStart": { "label": "端口跳跃起始" },
      "portHopRangeEnd": { "label": "端口跳跃结束" },
      "obfsType": { "label": "混淆类型" },
      "obfsPassword": { "label": "混淆密码" },
      "congestionControl": { "label": "拥塞控制算法" },
      "brutalTxBandwidth": { "label": "Brutal 发送带宽", "unit": "bps" },
      "bandwidthUp": { "label": "上行带宽" },
      "bandwidthDown": { "label": "下行带宽" },
      "ignoreClientBandwidth": { "label": "忽略客户端带宽" },
      "enableSpeedTest": { "label": "启用速度测试" },
      "speedTestPingInterval": { "label": "Ping 间隔", "unit": "秒" },
      "udpIdleTimeout": { "label": "UDP 空闲超时", "unit": "秒" },
      "sniffEnabled": { "label": "启用协议嗅探" },
      "sniffTimeout": { "label": "嗅探超时", "unit": "秒" },
      "sniffRespectHttps": { "label": "遵从 HTTPS 语义" },
      "masqueradeType": { "label": "伪装类型" },
      "masqueradeFile": { "label": "伪装文件路径" },
      "masqueradeProxyUrl": { "label": "伪装代理 URL" },
      "masqueradeStringContent": { "label": "伪装字符串内容" },
      "masqueradeStringHeaders": { "label": "伪装响应头 JSON" },
      "masqueradeStringStatusCode": { "label": "伪装状态码" },
      "resolverType": { "label": "DNS 解析器类型" },
      "resolverTcpAddr": { "label": "TCP 解析器地址" },
      "resolverUdpAddr": { "label": "UDP 解析器地址" },
      "resolverTlsAddr": { "label": "TLS 解析器地址" },
      "serverCost": { "label": "服务器费用", "unit": "/月" },
      "billingCycle": { "label": "续费周期" },
      "expirationDate": { "label": "到期日期" },
      "domainName": { "label": "关联域名" },
      "remark": { "label": "备注" },
      "configVersion": { "label": "配置版本" },
      "configUpdatedAt": { "label": "配置更新时间" }
    }
  },
  "table": {
    "columns": {
      "configVersion": "配置版本",
      "listenPort": "监听端口",
      "domainName": "域名"
    }
  },
  "toast": {
    "updateConfigSuccess": "节点配置更新成功",
    "updateConfigFailed": "节点配置更新失败"
  }
}
```

> 英文语言包结构完全对称，值翻译为对应英文。

---

## 重难点

| 难点 | 说明 | 解决方案 |
|------|------|----------|
| **条件显隐逻辑复杂** | 多个字段之间存在依赖关系（如 `enablePortHopping` → `portHopRangeStart/End`，`congestionControl` → `brutalTxBandwidth`），表单需动态显示/隐藏 | 使用 `v-if` / `v-show` 绑定对应开关字段，配合 `watch` 在开关关闭时自动清空依赖字段的值 |
| **「仅提交变更字段」的差异计算** | 后端 PUT 接口 `null` 表示"不修改"，前端需精确区分"用户设为 null"与"字段未修改"。所有字段可选的请求体中，未变更字段不能发送 | 表单初始化时深拷贝一份 `initialValues`，提交前逐字段对比 `currentValues` 与 `initialValues`，仅将不同的字段加入请求体 |
| **回填当前值** | 打开编辑对话框时需要将节点当前的 Phase 7 字段值回填到表单。但当前 `NodeDto` 可能尚未包含 Phase 7 字段（旧数据），表单字段均为可选 | 回填时使用 `??` 操作符提供默认值（如 `listenPort ?? 6789`），未返回的字段保持 `undefined` |
| **Tab 切换时的表单状态保持** | 用户在 Tab A 填写数据后切换到 Tab B 再切回，数据不应丢失 | 所有表单字段的 `v-model` 绑定到同一个 reactive 对象上，Tab 切换不会销毁组件（使用 `v-if` 时不销毁，或用 `v-show` 保持 DOM），数据自然保持 |
| **移动端 Tab 过多** | 6 个 Tab 在移动端 375px 视口下可能横向溢出 | 使用 PrimeVue `Tabs` 的 `scrollable` 属性，或移动端改用 `SelectButton` 下拉切换模式 |
| **`obfsPassword` 安全处理** | 后端存储为 AES-256-GCM 加密，返回时脱敏。编辑时若用户不修改密码，不应发送该字段 | `obfsPassword` 在表单中用独立输入框，未修改时 `undefined` 不发送；若用户清空密码框，发送 `null` |

---

## 注意点

1. **`NodeDto` 所有 Phase 7 新增字段均为可选**（`?:`），确保对旧数据的向后兼容。旧节点可能没有这些字段，组件需通过 `??` 提供默认值
2. **配置表单仅 admin+ 角色可见**，使用 `v-permission="['super_admin', 'admin']"` 控制
3. **`trafficStatsSecret` 的脱敏变更**：Phase 7 后该字段返回值从明文变为 `"***encrypted***"`，详情页展示无需特殊处理（本身就是展示用途）
4. **预注册节点表单**新增了 3 个可选字段（`listenPort`、`domainName`、`remark`），更新 [`NodeRegisterDialog.vue`](../../src/views/nodes/NodeRegisterDialog.vue) 时需同步增加输入项
5. **列表页新增列为默认隐藏**，通过 PrimeVue DataTable 的 `column` 组件 `hidden` 初始状态控制，用户可通过列选择器手动显示
6. **配置更新成功后必须刷新详情**：Store action 中 `updateNodeConfig` 成功后自动调用 `fetchNodeDetail(id)`，确保 `configVersion` 等字段实时更新
7. **提交按钮 Loading 状态**：使用 `nodesStore.isSubmitting` 绑定按钮 `loading` 属性，防止重复提交
8. **不建议在编辑表单中提供"实时预览 YAML"功能**：YAML 由后端生成，前端不关心其内容格式
9. **该接口不涉及路由变更**：配置编辑表单以对话框形式在 [`NodeDetailView.vue`](../../src/views/nodes/NodeDetailView.vue) 中打开，无需新增路由

---

## 阶段验证清单

Phase 7 完成后，必须通过以下**全部**验证：

| # | 验证项 | 验证方法 |
|---|--------|----------|
| 7.01 | `bun run type-check` 无错误 | 执行命令，确认 0 错误 |
| 7.02 | 节点详情页显示 `configVersion`、`configUpdatedAt`、`listenPort`、`domainName`、`remark` 字段 | 打开任意节点详情页检查 |
| 7.03 | admin+ 角色可见「编辑配置」按钮，readonly 角色不可见 | 分别用 admin 和 readonly 账号登录测试 |
| 7.04 | 点击「编辑配置」打开对话框，6 个 Tab 均正常显示 | 依次点击各 Tab |
| 7.05 | 表单回填当前节点配置值 | 打开已配置过的节点，检查表单初始值与详情页展示一致 |
| 7.06 | 条件显隐逻辑正确（端口跳跃/拥塞控制/速度测试/嗅探/伪装/DNS） | 切换各开关，确认关联字段显示/隐藏 |
| 7.07 | 修改任一字段值后提交，Toast 提示成功，详情页数据自动刷新 | 修改 `listenPort` → 提交 → 检查详情页 `listenPort` 已更新 |
| 7.08 | 未修改任何字段直接提交，请求体中仅发送变更字段（0 个字段变更时前端拦截不发送请求） | DevTools Network 检查 PUT 请求体 |
| 7.09 | `configVersion` 在配置更新后自增 | 记录提交前版本号 → 提交 → 检查版本号 +1 |
| 7.10 | 表单前端校验正常工作 | 输入非法值（如端口 99999），提交按钮禁用/提示错误 |
| 7.11 | 每个 Tab 的「重置为默认值」按钮正常工作 | 修改监听字段 → 点击重置 → 字段恢复默认值 |
| 7.12 | 移动端（375px 视口）Tab 可滚动切换，表单单列布局正常 | DevTools 切换到 375px 测试 |
| 7.13 | 桌面端（1920px 视口）表单双列布局正常 | DevTools 切换到 1920px 测试 |
| 7.14 | 节点列表页可通过列选择器显示 `configVersion`、`listenPort`、`domainName` 列 | 操作列选择器，检查新增列数据 |
| 7.15 | 预注册节点对话框新增 3 个字段（`listenPort`、`domainName`、`remark`）可用 | 打开预注册对话框检查 |
| 7.16 | 中英文语言包中所有新增 key 均已翻译且无遗漏 | 分别切换 zh-CN / en-US，检查配置表单所有文案 |
| 7.17 | 亮/暗色主题下配置表单显示正常 | 切换主题后检查表单 Tab、输入框、按钮颜色 |
| 7.18 | `bun run build` 无错误无警告 | 执行生产构建 |

---

## 文件变更汇总

| 操作 | 文件 | 说明 |
|------|------|------|
| ✏️ 修改 | [`src/types/node.types.ts`](../../src/types/node.types.ts) | 新增 ~28 个 Phase 7 字段 + `UpdateNodeConfigRequest` 类型 |
| ✏️ 修改 | [`src/api/modules/nodes.ts`](../../src/api/modules/nodes.ts) | 新增 `updateConfig()` 方法 |
| ✏️ 修改 | [`src/stores/nodes.store.ts`](../../src/stores/nodes.store.ts) | 新增 `updateNodeConfig()` action |
| ✏️ 修改 | [`src/views/nodes/NodeListView.vue`](../../src/views/nodes/NodeListView.vue) | DataTable 新增 3 个可选列 |
| ✏️ 修改 | [`src/views/nodes/NodeDetailView.vue`](../../src/views/nodes/NodeDetailView.vue) | 新增字段展示 + 编辑配置按钮 |
| ✏️ 修改 | [`src/views/nodes/NodeRegisterDialog.vue`](../../src/views/nodes/NodeRegisterDialog.vue) | 新增 3 个可选输入字段 |
| ✨ 新建 | [`src/views/nodes/components/NodeConfigForm.vue`](../../src/views/nodes/components/NodeConfigForm.vue) | 配置编辑表单（6 Tab） |
| ✏️ 修改 | [`src/locales/zh-CN/nodes.json`](../../src/locales/zh-CN/nodes.json) | 新增 ~25 个中文 key |
| ✏️ 修改 | [`src/locales/en-US/nodes.json`](../../src/locales/en-US/nodes.json) | 新增 ~25 个英文 key |
