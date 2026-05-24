# 后端 API Phase 7 增量变更参考

> **文档定位**：供前端开发人员**单独参考**的增量文档，聚焦 Phase 7（Hysteria 2 配置自动管理）引入的所有后端接口变更。
> **基准版本**：Phase 6 完成时的三份交接文档
> **变更提交**：[`926eb2c`](https://github.com/none) — `docs: 更新 API 文档以支持 Phase 7 Hysteria 2 配置自动管理`
> **相关文档**：[`master-panel-api.md`](master-panel-api.md) · [`master-internal-api.md`](master-internal-api.md) · [`agent-api-reference.md`](agent-api-reference.md)

---

## 1. Phase 7 变更概览

Phase 7 实现了**管理员通过面板修改节点 Hysteria 2 配置 → Master 自动生成 YAML → Edge Agent 自动拉取并热重载**的完整链路。

核心机制：

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

---

## 2. 新增接口

### 2.1 `PUT /api/v1/admin/nodes/{nodeId}/config` — 更新节点配置

> **Phase 7 全新接口**，也是前端唯一需要新增对接的接口。

```
PUT /api/v1/admin/nodes/edge-node-01/config
Authorization: Bearer {admin_token}
Content-Type: application/json
```

**请求体（所有字段可选，`null` 表示不修改）：**

| 分类 | 字段 | 类型 | 说明 |
|------|------|------|------|
| **监听** | `listenAddress` | string / null | 监听地址，默认 `"0.0.0.0"` |
| | `listenPort` | int / null | 监听端口，默认 `6789` |
| | `enablePortHopping` | bool / null | 启用端口跳跃。关闭后 YAML 仅生成单端口 `listen` |
| | `portHopRangeStart` | int / null | 端口跳跃起始，默认 `61000` |
| | `portHopRangeEnd` | int / null | 端口跳跃结束，默认 `63000` |
| **混淆** | `obfsType` | string / null | 混淆类型：`salamander` |
| | `obfsPassword` | string / null | 混淆密码（明文传入，服务端 AES-256-GCM 加密存储） |
| **拥塞控制** | `congestionControl` | string / null | 算法：`bbr`、`cubic`、`brutal` |
| | `brutalTxBandwidth` | long / null | Brutal 发送带宽（bps） |
| **QUIC** | `quicMaxIdleTimeout` | int / null | QUIC 最大空闲超时（秒） |
| | `quicMaxUdpPayloadSize` | int / null | QUIC 最大 UDP 载荷（字节） |
| **带宽** | `bandwidthUp` | string / null | 上行带宽（如 `"100 mbps"`） |
| | `bandwidthDown` | string / null | 下行带宽 |
| | `ignoreClientBandwidth` | bool / null | 忽略客户端带宽设置 |
| **速度测试** | `enableSpeedTest` | bool / null | 启用速度测试 |
| | `speedTestPingInterval` | int / null | Ping 间隔（秒） |
| **UDP** | `udpIdleTimeout` | int / null | UDP 空闲超时（秒） |
| **协议嗅探** | `sniffEnabled` | bool / null | 启用协议嗅探 |
| | `sniffTimeout` | int / null | 嗅探超时（秒） |
| | `sniffRespectHttps` | bool / null | 遵从 HTTPS 语义 |
| **伪装** | `masqueradeType` | string / null | 类型：`file`、`proxy`、`string`、`reply` |
| | `masqueradeFile` | string / null | 伪装文件路径（type=file） |
| | `masqueradeProxyUrl` | string / null | 伪装代理 URL（type=proxy） |
| | `masqueradeStringContent` | string / null | 伪装字符串（type=string） |
| | `masqueradeStringHeaders` | string / null | 伪装响应头 JSON（type=string） |
| | `masqueradeStringStatusCode` | int / null | 伪装状态码（type=string） |
| **DNS** | `resolverType` | string / null | 解析器类型：`system`、`udp`、`tcp`、`tls` |
| | `resolverTcpAddr` | string / null | TCP 解析器地址 |
| | `resolverUdpAddr` | string / null | UDP 解析器地址 |
| | `resolverTlsAddr` | string / null | TLS 解析器地址 |
| **运营管理** | `serverCost` | decimal / null | 服务器费用（月付金额） |
| | `billingCycle` | string / null | 续费周期：`monthly`、`quarterly`、`yearly` |
| | `expirationDate` | string (ISO 8601) / null | 服务器到期日期 |
| | `domainName` | string / null | 节点关联域名 |
| | `remark` | string / null | 节点备注信息 |

**成功响应 (HTTP 200)**：返回更新后的完整 `NodeDto`（含所有 Phase 7 扩展字段），其中 `configVersion` 已递增、`configUpdatedAt` 已更新。

**常用调用示例**：

```json
// 1. 仅关闭端口跳跃
{ "enablePortHopping": false }

// 2. 修改混淆密码
{ "obfsPassword": "new-password-here" }

// 3. 切换拥塞控制算法为 Brutal
{ "congestionControl": "brutal", "brutalTxBandwidth": 104857600 }

// 4. 更新运营信息
{ "serverCost": 49.99, "billingCycle": "monthly", "remark": "升级到高性能实例" }
```

> **前端表单建议**：按分类组织 Tab 页（监听 / 混淆 / 拥塞控制 / 带宽 / 伪装 / DNS / 运营），每个 Tab 提供"重置为默认值"按钮。

---

## 3. 已有接口的响应字段变更

以下接口的响应结构新增了字段，前端需要更新 TS 类型定义以正确解析。

### 3.1 节点列表 `GET /api/v1/nodes`

**新增字段一览（全部位于 `NodeDto` 响应中）：**

#### 监听与端口跳跃

| 字段 | 类型 | 默认值 |
|------|------|--------|
| `listenAddress` | string / null | `"0.0.0.0"` |
| `listenPort` | int / null | `6789` |
| `enablePortHopping` | bool | `true` |
| `portHopRangeStart` | int / null | `61000` |
| `portHopRangeEnd` | int / null | `63000` |

#### 混淆与拥塞控制

| 字段 | 类型 | 说明 |
|------|------|------|
| `obfsType` | string / null | 混淆类型：`salamander` |
| `congestionControl` | string / null | 算法：`bbr`、`cubic`、`brutal` |
| `brutalTxBandwidth` | long / null | Brutal 发送带宽（bps） |

#### 带宽与速度测试

| 字段 | 类型 | 说明 |
|------|------|------|
| `bandwidthUp` | string / null | 上行带宽，`null` = 不限 |
| `bandwidthDown` | string / null | 下行带宽，`null` = 不限 |
| `ignoreClientBandwidth` | bool / null | 忽略客户端带宽设置 |
| `enableSpeedTest` | bool / null | 是否启用速度测试 |

#### UDP 与协议嗅探

| 字段 | 类型 | 说明 |
|------|------|------|
| `udpIdleTimeout` | int / null | UDP 空闲超时（秒），默认 `60` |
| `sniffEnabled` | bool / null | 启用协议嗅探 |

#### 伪装

| 字段 | 类型 | 说明 |
|------|------|------|
| `masqueradeType` | string / null | 类型：`file`、`proxy`、`string`、`reply` |
| `masqueradeFile` | string / null | 伪装文件路径（type=file 时） |

#### DNS 解析器

| 字段 | 类型 | 说明 |
|------|------|------|
| `resolverType` | string / null | 类型：`system`、`udp`、`tcp`、`tls` |

#### 配置版本与运营管理

| 字段 | 类型 | 说明 |
|------|------|------|
| `configVersion` | int | 配置版本号，每次配置变更后自增 |
| `configUpdatedAt` | string (ISO 8601) / null | 配置最后更新时间 |
| `serverCost` | decimal / null | 服务器费用 |
| `billingCycle` | string / null | 续费周期：`monthly`、`quarterly`、`yearly` |
| `expirationDate` | string (ISO 8601) / null | 到期日期 |
| `domainName` | string / null | 关联域名 |
| `remark` | string / null | 备注信息 |

> **注意**：以上字段同时适用于 `GET /api/v1/nodes`（列表）和 `GET /api/v1/nodes/{nodeId}`（详情），可共用同一套 TS 类型。详情接口额外独有 `secretVersion` 和 `trafficStatsSecret`。

### 3.2 节点详情 `GET /api/v1/nodes/{nodeId}`

- 响应体新增字段与 [§3.1](#31-节点列表-get-apiv1nodes) 完全一致
- **变更**：`trafficStatsSecret` 响应值从 `"encrypted_secret_value"` 变为 `"***encrypted***"`（脱敏处理）
- 详情接口包含 3 个独有字段：`secretVersion`（int）、`trafficStatsSecret`（脱敏字符串）、以及所有 Phase 7 扩展字段

### 3.3 预注册节点 `POST /api/v1/admin/nodes/pre-register`

**请求体新增字段：**

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `listenPort` | int | ❌ | Hysteria 2 实际监听端口，默认 `6789` |
| `domainName` | string | ❌ | 节点关联域名，写入 YAML 的 `realm` |
| `remark` | string | ❌ | 节点备注信息 |

**已有字段语义变更**：`port` 现在标记为"历史兼容字段"，实际监听端口优先看 `listenPort`。`expiresAt` 说明补充为"预注册后 7 天"。

---

## 4. 非面板接口变更（仅供了解）

以下变更发生在前端不对接的 Agent ↔ Master 接口中，此处列出便于理解完整链路：

| 接口 | 变更内容 |
|------|---------|
| `POST /api/v1/nodes/register-with-token` | 响应新增 `configVersion`（int）和 `configYaml`（string / null） |
| `GET /api/v1/nodes/{nodeId}/config` | 响应新增 `configVersion`（int）和 `configYaml`（string / null），补充了完整字段说明表 |
| `POST /api/v1/nodes/{nodeId}/heartbeat` | 响应新增 `configVersion`（int），补充了配置变更检测流程图 |
| Edge Agent `/health` | 端点列表从 2 个扩展为 3 个（新增 `:8081 /kick`）；架构图中标注了端口跳跃规则 |

---

## 5. 前端适配清单

基于当前 `src/types/node.types.ts` 的现状，需要以下改动：

### 5.1 TypeScript 类型定义

**文件**：[`src/types/node.types.ts`](../../src/types/node.types.ts)

`NodeDto` 需新增 ~22 个字段，`NodeDetail` 需继承这些字段。`PreRegisterNodeRequest` 需新增 3 个可选字段。

### 5.2 API 模块

**文件**：[`src/api/modules/nodes.ts`](../../src/api/modules/nodes.ts)

新增 `updateConfig(id: string, data: UpdateNodeConfigRequest)` 方法。

### 5.3 新的请求/响应类型

需新增 `UpdateNodeConfigRequest` 类型（对应 PUT 请求体，所有字段可选）。

### 5.4 Store

**文件**：[`src/stores/nodes.store.ts`](../../src/stores/nodes.store.ts)

新增 `updateNodeConfig` action。

### 5.5 视图

- [`NodeListView.vue`](../../src/views/nodes/NodeListView.vue)：表格列可能需要展示 `configVersion`、`listenPort`、`domainName` 等新增字段
- [`NodeDetailView.vue`](../../src/views/nodes/NodeDetailView.vue)：详情页需展示 Phase 7 新增的所有配置字段
- **新增组件**：节点配置编辑表单（建议按分类 Tab 组织）

### 5.6 国际化

`src/locales/zh-CN/nodes.json` 和 `src/locales/en-US/nodes.json` 需新增 Phase 7 相关字段的翻译键（约 25 个新 key）。

---

## 6. 节点在线判定更新

文档中更新了在线判定说明：`isActive == true` 且 `lastHeartbeat` 距当前 < 90 秒（可配）。当前前端 [`nodes.store.ts`](../../src/stores/nodes.store.ts:7) 中已定义 `HEARTBEAT_TIMEOUT_MS = 90_000`，无需修改。

---

## 附录：完整 Phase 7 NodeDto 字段速查

```
基础字段（已有）:
  id, name, ipAddress, port, isActive, createdAt, lastHeartbeat,
  location, trafficStatsPort, provisionStatus

Phase 7 新增:
  listenAddress, listenPort, enablePortHopping, portHopRangeStart, portHopRangeEnd,
  obfsType, congestionControl, brutalTxBandwidth,
  bandwidthUp, bandwidthDown, ignoreClientBandwidth, enableSpeedTest,
  udpIdleTimeout, sniffEnabled,
  masqueradeType, masqueradeFile,
  resolverType,
  configVersion, configUpdatedAt,
  serverCost, billingCycle, expirationDate, domainName, remark

详情独有:
  secretVersion, trafficStatsSecret
```
