# Agent 边缘节点 API 接口参考

> **文档定位**：仅供前端开发人员**参考阅读**，了解边缘节点侧的接口协议。前端不对接边缘节点。
> **实际对接方**：Hysteria Server → Edge Agent → 主服务器（Master）
> **相关文档**：[`master-panel-api.md`](master-panel-api.md) · [`master-internal-api.md`](master-internal-api.md)

---

## 架构概览

Edge Agent 是部署在每个边缘节点（VPS）上的轻量级进程，它承担三个角色：

```
┌──────────────────────────────────────────────────────────────┐
│                    边缘节点 (VPS)                             │
│                                                              │
│  Hysteria Server (Hysteria 2)                                │
│  (监听 :Port QUIC，iptables DNAT 端口跳跃→Port)              │
│       │                                                      │
│       │ POST /auth (HTTP Auth)                               │
│       ▼                                                      │
│  ┌─────────────┐     POST /api/v1/auth/hysteria              │
│  │ Edge Agent  │ ──────────────────────────────────────────► │
│  │             │     POST /api/v1/nodes/{id}/heartbeat       │
│  │ :8080 /auth │        ─ 响应含 configVersion (Phase 7)     │
│  │ :8080 /health│    GET  /api/v1/nodes/{id}/config           │
│  │ :8081 /kick │        ─ 响应含 configYaml (Phase 7)        │
│  │             │◄───────────────────────────────────────────  │
│  │ 本地配置管理 │                                            │
│  │ ─────────── │                                            │
│  │ configYaml  │ 写入 /etc/hysteria/config.yaml              │
│  │ configVers. │ 缓存版本号，心跳时对比                      │
│  └─────────────┘                                             │
│       │                                                      │
│       │ GET /traffic?clear=1 + GET /online                   │
│       ▼                                                      │
│  Hysteria trafficStats API                                   │
│  (127.0.0.1:9999)                                            │
└──────────────────────────────────────────────────────────────┘
                                              主服务器 (Master)
```

Edge Agent 暴露**三个本地 HTTP 端点**（仅监听 `127.0.0.1`）：

| 端点 | 调用方 | 用途 |
|------|--------|------|
| `POST /auth` | Hysteria Server | 客户端连接认证 |
| `GET /health` | 监控系统 / Master 探活 | 健康检查 |
| `POST /kick-user` | Master 的 [`KickService`](../../src/HysteriaAuth.Master/Services/KickService.cs) | 踢用户下线 |

---

## 1. POST /auth — Hysteria 原生认证

> **调用方**: Hysteria Server（依据 [`hysteria.yaml`](document/hysteria/hysteria-server-config.md) 中 `auth.http.url` 配置）
> **触发时机**: 每个客户端发起 QUIC 连接时

### 请求

```
POST /auth
Content-Type: application/json
```

**请求体（Hysteria 原生格式）：**

```json
{
    "addr": "123.123.123.123:44556",
    "auth": "dGVzdHVzZXI6dXNlcnBhc3N3b3JkMTIz",
    "tx": 52428800
}
```

| 字段 | 类型 | 说明 |
|------|------|------|
| `addr` | string | 客户端 IP 地址和端口，格式 `IP:Port` 或 `[IPv6]:Port` |
| `auth` | string | 客户端提交的认证凭据。Hysteria 客户端侧配置为 `username:password` 的 **Base64 编码** |
| `tx` | uint64 | 客户端期望的发送速率（**字节/秒**，服务端视角 = 客户端下载速率） |

> **关键**: Hysteria 原生协议**不区分 `username` 和 `password`**，仅通过 `auth` 字段传递单一凭据串。Edge Agent 的 [`AuthProxy`](src/HysteriaAuth.Agent/Services/AuthProxy.cs) 负责 Base64 解码 → 按 `:` 分割 → 提取 username 和 password。

### 响应

**认证成功 (HTTP 200)：**

```json
{
    "ok": true,
    "id": "testuser"
}
```

| 字段 | 类型 | 说明 |
|------|------|------|
| `ok` | bool | 是否认证通过。Hysteria 仅根据此字段决定是否允许连接 |
| `id` | string | 用户标识（username），Hysteria 用于流量统计和 `/online` 中的 key |

**认证失败 (HTTP 403)：**

```json
{
    "ok": false,
    "id": ""
}
```

### 内部处理流程

Edge Agent 收到 `POST /auth` 后，执行以下协议转换：

1. **解析 `auth` 字段**：Base64 解码 → 按 `:` 分割为 `username` + `password`
2. **提取客户端 IP**：从 `addr` 中提取纯 IP（去掉端口）
3. **构造内部请求**：生成 [`POST /api/v1/auth/hysteria`](master-internal-api.md#1-post-apiv1authhysteria--hysteria-用户认证) 请求
4. **转发到 Master**：携带 `X-Node-Secret` 头
5. **转换响应格式**：Master 返回的 `{success, userId, ...}` → Hysteria 期望的 `{ok, id}`

协议转换对照表见 [`AuthProxy`](src/HysteriaAuth.Agent/Services/AuthProxy.cs:28-77)。

---

## 2. GET /health — Edge Agent 健康检查

> **调用方**: 外部监控系统 / Master 按需探活

```
GET /health
```

无需认证。

### 响应 (HTTP 200)

```json
{
    "status": "healthy",
    "nodeId": "edge-node-01",
    "timestamp": "2025-01-01T12:00:00Z",
    "checks": {
        "system_monitor": "running",
        "auth_proxy": "running",
        "master_reachable": "unknown",
        "hysteria_reachable": "unknown",
        "traffic_collector": "running"
    }
}
```

| 字段 | 类型 | 说明 |
|------|------|------|
| `status` | string | 固定返回 `"healthy"` |
| `nodeId` | string | 当前 Edge Agent 的节点 ID |
| `timestamp` | string | UTC 时间戳（ISO 8601 格式） |
| `checks.system_monitor` | string | 系统监控模块状态 |
| `checks.auth_proxy` | string | 认证代理模块状态 |
| `checks.master_reachable` | string | Master 是否可达（当前为占位值 `"unknown"`） |
| `checks.hysteria_reachable` | string | Hysteria trafficStats API 是否可达（当前为占位值 `"unknown"`） |
| `checks.traffic_collector` | string | 流量采集模块状态 |

> **注意**: `master_reachable` 和 `hysteria_reachable` 当前为占位值，后续版本将实现实时探活。

---

## 3. POST /kick-user — 踢用户下线

> **调用方**: Master 的 [`KickService`](src/HysteriaAuth.Master/Services/KickService.cs)（管理员在前端触发踢人操作后，Master 通过此端点转发到 Edge Agent）
> **认证**: 当前版本无认证（Edge Agent 监听 `127.0.0.1`，仅本地可访问）

```
POST /kick-user
Content-Type: application/json
```

### 请求体

```json
{
    "username": "user123",
    "nodeId": "edge-node-01"
}
```

| 字段 | 类型 | 说明 |
|------|------|------|
| `username` | string | 要踢下线的用户名 |
| `nodeId` | string | 目标节点 ID（由 Master 确定） |

### 响应

**成功 (HTTP 200)：**

无响应体（Edge Agent 转发到 Hysteria `POST /kick` 后返回 200）

**失败：**

| HTTP 状态码 | 说明 |
|-------------|------|
| `400` | 请求体为空或 `username` 为空 |
| `502` | Hysteria `/kick` API 调用失败（Hysteria 不可达或返回错误） |

### 内部处理流程

Edge Agent 收到请求后，转发到本地 Hysteria trafficStats API 的 `/kick` 端点：
```
POST http://{trafficStats.listen}:{trafficStats.port}/kick?secret={secret}
Content-Type: application/json

{ "username": "user123" }
```

---

## 4. 认证凭据格式说明

前端开发人员需要了解客户端认证凭据的格式，以便在用户配置说明中展示：

### 客户端 auth 字段生成

```
原始凭据:  username:password
Base64编码: echo -n "username:password" | base64
结果:       dXNlcm5hbWU6cGFzc3dvcmQ=
```

### Hysteria 客户端配置示例

```json
{
    "server": "edge-node-ip:61000-63000",
    "auth": "dXNlcm5hbWU6cGFzc3dvcmQ=",
    "socks5": {
        "listen": "127.0.0.1:1080"
    }
}
```

> **端口说明**：Hysteria 2 服务端仅监听单个端口（配置中的 `port`）。若节点启用了端口跳跃（`enablePortHopping=true`），客户端可连接 `portHopRangeStart-portHopRangeEnd` 范围内的任一端口，Agent 通过 iptables DNAT 将范围内 UDP 流量全部转发到服务端监听端口。若未启用端口跳跃，客户端直接连接 `ipAddress:port`。

---

## 5. Edge Agent 配置参考

Edge Agent 的完整配置文件位于 [`agent.json`](../../src/HysteriaAuth.Agent/Config/agent.json)，关键配置项：

| 配置段 | 关键字段 | 说明 |
|--------|---------|------|
| `AuthProxy` | `ListenPort: 8080` | 认证代理监听端口（Hysteria `auth.http.url` 指向此端口） |
| `TrafficStats` | `ListenPort: 9999` | Hysteria trafficStats API 端口 |
| `Reporter` | `IntervalSeconds: 30` | 心跳上报间隔 |
| `TrafficStats` | `CollectIntervalSeconds: 30` | 流量采集间隔 |
| `Cache` | `ExpirationMinutes: 5` | 认证缓存过期时间 |

### 5.1 Phase 7: Hysteria 2 配置自动管理

Edge Agent 启动后通过以下流程自动管理 Hysteria 2 服务端配置：

**注册阶段（`Initializer`）：**

1. 调用 [`POST /api/v1/nodes/register-with-token`](master-internal-api.md#31-post-apiv1nodesregister-with-token--令牌注册推荐) 完成节点注册
2. 从响应中获取 `configYaml` 字段
3. 将 `configYaml` 写入 `/etc/hysteria/config.yaml`
4. 缓存 `configVersion` 到本地
5. 如果节点配置了端口跳跃（`enablePortHopping=true`），应用 iptables DNAT 规则：
   ```bash
   iptables -t nat -A PREROUTING -i eth0 -p udp --dport {start}:{end} -j DNAT --to-destination :{port}
   ```
6. 启动/重载 Hysteria 2 服务

**心跳阶段（`StatusReporter`）：**

1. 每次心跳 [`POST /api/v1/nodes/{id}/heartbeat`](master-internal-api.md#41-post-apiv1nodesnodeidheartbeat--节点心跳状态上报) 的响应包含 `configVersion`
2. 对比本地缓存的版本号：
   - **相同** → 无需操作
   - **不同** → 触发配置重同步
3. 配置重同步流程：
   - 调用 [`GET /api/v1/nodes/{id}/config`](master-internal-api.md#33-get-apiv1nodesnodeidconfig--获取节点配置)
   - 获取最新的 `configYaml`
   - 写入 `/etc/hysteria/config.yaml`
   - 重载 Hysteria 2 服务
   - 更新本地缓存的 `configVersion`

**配置变更触发来源：**

管理员通过管理面板调用 [`PUT /api/v1/admin/nodes/{nodeId}/config`](master-panel-api.md#56-put-apiv1adminnodesnodeidconfig--更新节点配置-phase-7) 修改节点配置，Master 自动递增 `ConfigVersion`。Edge Agent 在下次心跳（最长 30 秒间隔）时检测到版本变化，自动拉取新配置。

> 前端无需关心这些配置，仅供参考。此信息有助于理解节点配置变更的完整传播链路。
