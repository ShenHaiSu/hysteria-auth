# Master 主节点非面板 API 参考

> **文档定位**：仅供前端开发人员**参考阅读**，了解主服务器的完整 API 体系。前端不对接这些接口。
> **实际调用方**：Edge Agent（边缘节点进程）
> **Base URL**: `https://{master-host}/api/v1`
> **认证方式**: `X-Node-Secret: {node_secret}` 请求头（节点密钥认证）
> **相关文档**：[`master-panel-api.md`](master-panel-api.md) · [`agent-api-reference.md`](agent-api-reference.md)

---

## 目录

- [1. 认证架构说明](#1-认证架构说明)
- [2. Hysteria 用户认证](#2-hysteria-用户认证)
  - [2.1 POST /api/v1/auth/hysteria](#21-post-apiv1authhysteria--hysteria-用户认证)
- [3. 节点生命周期](#3-节点生命周期)
  - [3.1 POST /api/v1/nodes/register-with-token — 令牌注册](#31-post-apiv1nodesregister-with-token--令牌注册推荐)
  - [3.2 POST /api/v1/nodes/register — 旧版注册](#32-post-apiv1nodesregister--旧版注册保留兼容)
  - [3.3 GET /api/v1/nodes/{nodeId}/config — 获取节点配置](#33-get-apiv1nodesnodeidconfig--获取节点配置)
- [4. 节点心跳与流量上报](#4-节点心跳与流量上报)
  - [4.1 POST /api/v1/nodes/{nodeId}/heartbeat](#41-post-apiv1nodesnodeidheartbeat--节点心跳状态上报)
- [5. 健康检查](#5-健康检查)
  - [5.1 GET /health — 主服务器健康检查](#51-get-health--主服务器健康检查)
- [附录：认证中间件路由规则](#附录认证中间件路由规则)

---

## 1. 认证架构说明

Master 采用**双层认证架构**，通过中间件链实现路由级别的认证分流：

```
                    请求进入
                       │
                       ▼
            ┌─────────────────────┐
            │ GlobalExceptionHandler│  ← 全局异常处理（最外层）
            └──────────┬──────────┘
                       ▼
            ┌─────────────────────┐
            │   NodeAuthMiddleware │  ← X-Node-Secret 验证
            │   /api/v1/auth/*     │    仅作用于 /api/v1/auth/* 和 /api/v1/nodes/*
            │   /api/v1/nodes/*    │    （排除 /api/v1/nodes/register-with-token）
            └──────────┬──────────┘
                       ▼
            ┌─────────────────────┐
            │    JwtMiddleware    │  ← JWT Bearer Token 验证
            │   /api/v1/admin/*   │    仅作用于 /api/v1/admin/* 和 /api/v1/users/*
            │   /api/v1/users/*   │
            └──────────┬──────────┘
                       ▼
                  Controller
```

| 认证方式 | 请求头 | 适用路由 | 调用方 |
|----------|--------|---------|--------|
| **节点密钥** | `X-Node-Secret: {secret}` | `/api/v1/auth/*`, `/api/v1/nodes/*` | Edge Agent |
| **JWT Token** | `Authorization: Bearer {token}` | `/api/v1/admin/*`, `/api/v1/users/*` | 前端管理面板 |
| **无认证** | — | `/health`, `/api/v1/nodes/register-with-token` | 监控系统 / 新节点 |

---

## 2. Hysteria 用户认证

### 2.1 `POST /api/v1/auth/hysteria` — Hysteria 用户认证

> **调用方**: Edge Agent 的 [`AuthProxy`](src/HysteriaAuth.Agent/Services/AuthProxy.cs)
> **认证**: `X-Node-Secret` 请求头
> **触发时机**: 每当 Hysteria 客户端发起 QUIC 连接，Hysteria Server 向 Edge Agent 发送 `POST /auth`，Edge Agent 完成协议转换后调用此接口。

```
POST /api/v1/auth/hysteria
Content-Type: application/json
X-Node-Secret: {node_secret}
```

**请求体：**

```json
{
    "username": "user123",
    "password": "password123",
    "nodeId": "edge-node-01",
    "clientIp": "192.168.1.100"
}
```

| 字段 | 类型 | 说明 |
|------|------|------|
| `username` | string | 认证用户名 |
| `password` | string | 认证密码（明文，传输层由 HTTPS 保护） |
| `nodeId` | string | 发起认证的节点 ID |
| `clientIp` | string | 客户端真实 IP（由 Edge Agent 从 Hysteria 的 `addr` 字段提取） |

**成功响应 (HTTP 200)：**

```json
{
    "success": true,
    "userId": 12345,
    "message": "Authentication successful",
    "remainingTraffic": 1073741824,
    "expiresAt": "2025-12-31T23:59:59Z"
}
```

| 字段 | 类型 | 说明 |
|------|------|------|
| `success` | bool | 是否认证成功。`false` 时检查响应中的 `error` 对象 |
| `userId` | long | 用户 ID |
| `message` | string | 认证结果描述 |
| `remainingTraffic` | long | 剩余流量（字节）。`totalTrafficBytes - usedTrafficBytes` |
| `expiresAt` | string (ISO 8601) / null | 账号过期时间，`null` = 永不过期 |

**失败响应：**

认证失败时返回标准错误格式。可能的失败原因：

| 错误代码 | HTTP 状态码 | 说明 |
|----------|-------------|------|
| `invalid_credentials` | 401 | 用户名或密码错误 |
| `account_disabled` | 403 | 用户账号已被禁用 |
| `account_expired` | 403 | 用户账号已过期 |
| `traffic_exhausted` | 403 | 流量已用尽 |
| `node_not_allowed` | 403 | 该用户不允许使用此节点 |
| `internal_error` | 500 | 服务器内部错误 |

**错误响应示例 (HTTP 403)：**

```json
{
    "error": {
        "code": "traffic_exhausted",
        "message": "流量已用尽",
        "requestId": "req_a1b2c3d4"
    }
}
```

### 协议转换链路

完整认证链路的数据流转：

```
Hysteria Client          Hysteria Server         Edge Agent              Master
     │                         │                      │                     │
     │── QUIC 连接请求 ──────►│                      │                     │
     │                         │── POST /auth ──────►│                     │
     │                         │  {addr, auth, tx}   │                     │
     │                         │                      │ Base64解码auth      │
     │                         │                      │ 提取username+pass   │
     │                         │                      │── POST /auth/ ────►│
     │                         │                      │   hysteria          │
     │                         │                      │   {username,        │
     │                         │                      │    password,        │
     │                         │                      │    nodeId,          │
     │                         │                      │    clientIp}        │
     │                         │                      │                     │── 验证密码
     │                         │                      │                     │── 检查状态
     │                         │                      │                     │── 检查流量
     │                         │                      │                     │── 记录日志
     │                         │                      │◄── {success, ...} ─│
     │                         │◄── {ok, id} ───────│                      │
     │◄── QUIC 连接建立 ──────│                      │                     │
```

---

## 3. 节点生命周期

### 3.1 `POST /api/v1/nodes/register-with-token` — 令牌注册（推荐）

> **调用方**: Edge Agent 的 [`Initializer`](src/HysteriaAuth.Agent/Services/Initializer.cs)
> **认证**: 无需认证（公开端点，通过一次性令牌保证安全）
> **触发时机**: Edge Agent 首次启动或令牌变更时

```
POST /api/v1/nodes/register-with-token
Content-Type: application/json
```

**请求体：**

```json
{
    "provisionToken": "prov_a1b2c3d4e5f6g7h8i9j0",
    "nodeId": "edge-node-01",
    "ipAddress": "10.0.0.1",
    "agentVersion": "1.0.0",
    "name": "Edge Node"
}
```

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `provisionToken` | string | ✅ | 预注册令牌（由管理员在面板调用 [`POST /api/v1/admin/nodes/pre-register`](master-panel-api.md#51-post-apiv1adminnodespre-register--预注册节点) 获取） |
| `ipAddress` | string | ✅ | 边缘节点公网 IP |
| `nodeId` | string | ❌ | 节点 ID。不传则由 Master 自动生成 UUID |
| `agentVersion` | string | ❌ | Agent 版本号 |
| `name` | string | ❌ | 节点名称。不传则由 Master 使用预注册时的名称 |

**成功响应 (HTTP 200)：**

```json
{
    "nodeId": "edge-node-01",
    "nodeSecret": "a1b2c3d4e5f6...",
    "trafficStatsSecret": "f6e5d4c3b2a1...",
    "config": {
        "authProxyPort": 8080,
        "healthCheckPort": 8081,
        "trafficStatsPort": 9999,
        "collectIntervalSeconds": 30,
        "heartbeatIntervalSeconds": 30
    }
}
```

| 字段 | 类型 | 说明 |
|------|------|------|
| `nodeId` | string | 节点唯一标识（UUID） |
| `nodeSecret` | string | 节点通信密钥（256-bit 随机生成），**后续所有 API 调用需通过 `X-Node-Secret` 头传递** |
| `trafficStatsSecret` | string | Hysteria trafficStats API 密钥，Edge Agent 用于调用 Hysteria 的 `/traffic` 和 `/online` 端点 |
| `config.authProxyPort` | int | 认证代理监听端口（Hysteria `auth.http.url` 指向此端口） |
| `config.healthCheckPort` | int | 健康检查端口 |
| `config.trafficStatsPort` | int | Hysteria trafficStats API 端口 |
| `config.collectIntervalSeconds` | int | 流量采集间隔（秒） |
| `config.heartbeatIntervalSeconds` | int | 心跳上报间隔（秒） |

> **重要**: 令牌为**一次性使用**。注册成功后 Master 将令牌清零，`provisionStatus` 设为 `provisioned`。重复使用同一令牌返回 `403`。

---

### 3.2 `POST /api/v1/nodes/register` — 旧版注册（保留兼容）

> **调用方**: Edge Agent（旧版兼容）
> **认证**: `X-Node-Secret` 请求头

```
POST /api/v1/nodes/register
Content-Type: application/json
X-Node-Secret: {node_secret}
```

**请求体：**

```json
{
    "nodeId": "edge-node-01",
    "name": "东京节点",
    "ipAddress": "10.0.0.1",
    "port": 443,
    "location": "Tokyo, Japan",
    "trafficStatsPort": 9999,
    "trafficStatsSecret": "some_secret"
}
```

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `nodeId` | string | ✅ | 节点 ID |
| `name` | string | ✅ | 节点名称 |
| `ipAddress` | string | ✅ | 节点 IP |
| `port` | int | ❌ | Hysteria 服务端口，默认 `443` |
| `location` | string | ❌ | 位置描述 |
| `trafficStatsPort` | int | ❌ | trafficStats API 端口 |
| `trafficStatsSecret` | string | ❌ | trafficStats API 密钥 |

> **新版推荐**: 使用令牌注册方式（`register-with-token`），旧版注册仅用于向后兼容。

---

### 3.3 `GET /api/v1/nodes/{nodeId}/config` — 获取节点配置

> **调用方**: Edge Agent（启动时或心跳前同步配置）
> **认证**: `X-Node-Secret` 请求头

```
GET /api/v1/nodes/edge-node-01/config
X-Node-Secret: {node_secret}
```

**成功响应 (HTTP 200)：**

```json
{
    "nodeId": "edge-node-01",
    "nodeSecret": "当前有效的节点密钥",
    "isActive": true,
    "config": {
        "authProxyPort": 8080,
        "healthCheckPort": 8081,
        "trafficStatsPort": 9999,
        "collectIntervalSeconds": 30,
        "heartbeatIntervalSeconds": 30
    }
}
```

> **用途**: Edge Agent 可通过此接口定期同步配置，确保本地配置与 Master 一致。例如密钥轮换后，新密钥通过此接口下发。

---

## 4. 节点心跳与流量上报

### 4.1 `POST /api/v1/nodes/{nodeId}/heartbeat` — 节点心跳/状态上报

> **调用方**: Edge Agent 的 [`StatusReporter`](src/HysteriaAuth.Agent/Services/StatusReporter.cs)
> **认证**: `X-Node-Secret` 请求头
> **频率**: 默认每 30 秒一次
> **复合功能**: 同时承载系统状态上报 + 用户流量数据上报

```
POST /api/v1/nodes/edge-node-01/heartbeat
Content-Type: application/json
X-Node-Secret: {node_secret}
```

**请求体：**

```json
{
    "nodeId": "edge-node-01",
    "cpuUsagePercent": 45.2,
    "memoryUsagePercent": 62.5,
    "memoryUsedMb": 2048.0,
    "memoryTotalMb": 4096.0,
    "networkInBytes": 1073741824,
    "networkOutBytes": 2147483648,
    "networkInMbps": 12.5,
    "networkOutMbps": 25.3,
    "activeConnections": 150,
    "reportedAt": "2025-01-01T12:00:00Z",
    "userTraffic": {
        "user1": { "tx": 1073741824, "rx": 536870912 },
        "user2": { "tx": 2147483648, "rx": 1073741824 }
    },
    "onlineUsers": {
        "user1": 2,
        "user2": 1
    }
}
```

| 字段 | 类型 | 说明 |
|------|------|------|
| `nodeId` | string | 节点 ID（与路径参数一致） |
| `cpuUsagePercent` | float | CPU 使用率 (%) |
| `memoryUsagePercent` | float | 内存使用率 (%) |
| `memoryUsedMb` | float | 已用内存 (MB) |
| `memoryTotalMb` | float | 总内存 (MB) |
| `networkInBytes` | long | 节点级累计网络流入（字节） |
| `networkOutBytes` | long | 节点级累计网络流出（字节） |
| `networkInMbps` | float | 当前网络流入速率 (Mbps) |
| `networkOutMbps` | float | 当前网络流出速率 (Mbps) |
| `activeConnections` | int | 当前活跃连接数 |
| `reportedAt` | string (ISO 8601) | 上报时间戳 |

**流量数据字段（Phase 3）：**

| 流量字段 | 类型 | 说明 |
|----------|------|------|
| `userTraffic` | object | 从 Hysteria `GET /traffic?clear=1` 采集的各用户流量增量 |
| `userTraffic.{user}.tx` | long | Hysteria 服务端发送字节数（**= 用户下载流量**）→ Master 记录为 `TrafficRecords.BytesOut` |
| `userTraffic.{user}.rx` | long | Hysteria 服务端接收字节数（**= 用户上传流量**）→ Master 记录为 `TrafficRecords.BytesIn` |
| `onlineUsers` | object | 从 Hysteria `GET /online` 采集的在线用户连接数 |
| `onlineUsers.{user}` | int | 该用户在节点的活跃连接数 |

> **流量方向记忆口诀**: `tx` = transmit（服务端发送）= 用户下载；`rx` = receive（服务端接收）= 用户上传。

**成功响应 (HTTP 200)：**

```json
{
    "received": true
}
```

**失败情况：**

| HTTP 状态码 | 说明 |
|-------------|------|
| `401` | 未提供 `X-Node-Secret` |
| `403` | 密钥无效（`node_secret_invalid`） |
| `404` | 节点不存在 |

**后端处理逻辑：**

接收心跳后，Master 执行以下操作：
1. 更新 `Nodes` 表的 `LastHeartbeat` 时间戳
2. 写入 `NodeStatus` 记录（系统指标历史）
3. 更新 `NodeTraffic` 汇总
4. 遍历 `userTraffic`，对每个用户：
   - 生成 `IdempotencyKey`（`{nodeId}_{userId}_{timestamp}`）
   - 插入 `TrafficRecords`（幂等：唯一约束防重复计入）
   - 累加 `Users.UsedTrafficBytes`
5. 更新 `Sessions` 表（基于 `onlineUsers`）

---

## 5. 健康检查

### 5.1 `GET /health` — 主服务器健康检查

> **调用方**: 负载均衡器、外部监控系统（如 UptimeRobot、Prometheus）
> **认证**: 无需认证

```
GET /health
```

**成功响应 (HTTP 200)：**

```json
{
    "status": "healthy",
    "timestamp": "2025-01-01T12:00:00Z",
    "version": "1.0.0",
    "uptime": "3d02h15m30s",
    "checks": {
        "database": "ok",
        "disk_space": "ok"
    }
}
```

| 字段 | 类型 | 说明 |
|------|------|------|
| `status` | string | 健康状态：`healthy` / `degraded` / `unhealthy` |
| `timestamp` | string (ISO 8601) | 检查时间戳 |
| `version` | string | 服务版本号 |
| `uptime` | string | 服务运行时长，格式 `{d}d{hh}h{mm}m{ss}s` |
| `checks.database` | string | 数据库连接检查：`ok` / `error` |
| `checks.disk_space` | string | 磁盘空间检查：`ok` / `unknown` / `error` |

**状态判定逻辑：**

| 状态 | 条件 | 建议处理 |
|------|------|---------|
| `healthy` | 所有检查为 `ok` | 正常 |
| `degraded` | 部分检查为 `unknown` | 关注但不紧急 |
| `unhealthy` | 任一检查为 `error` | 触发告警 |

> **当前实现**: 检查项包括数据库连通性和基础磁盘可用性。后续可扩展更多检查项。

---

## 附录：认证中间件路由规则

### 需要 `X-Node-Secret` 认证的路由

中间件 [`NodeAuthMiddleware`](src/HysteriaAuth.Master/Middleware/NodeAuthMiddleware.cs) 拦截以下路由前缀：

| 路由前缀 | 认证方式 | 说明 |
|----------|---------|------|
| `/api/v1/auth/*` | `X-Node-Secret` | Hysteria 认证接口 |
| `/api/v1/nodes/*` | `X-Node-Secret` | 节点心跳、配置同步接口 |

**例外**（跳过认证）：
- `/api/v1/nodes/register-with-token` — 令牌注册是公开端点

### 需要 `Authorization: Bearer` 认证的路由

中间件 [`JwtMiddleware`](src/HysteriaAuth.Master/Middleware/JwtMiddleware.cs) 拦截以下路由前缀：

| 路由前缀 | 认证方式 | 说明 |
|----------|---------|------|
| `/api/v1/admin/*` | `Authorization: Bearer {token}` | 管理功能接口 |
| `/api/v1/users/*` | `Authorization: Bearer {token}` | 用户管理接口 |

### 无需认证的路由

| 路由 | 说明 |
|------|------|
| `GET /health` | 健康检查 |
| `POST /api/v1/admin/login` | 管理员登录（获取 JWT Token） |
| `POST /api/v1/nodes/register-with-token` | 节点令牌注册 |

### 中间件执行顺序

```
1. GlobalExceptionHandler  (最外层，捕获所有未处理异常)
2. NodeAuthMiddleware       (X-Node-Secret 验证)
3. JwtMiddleware            (JWT Bearer Token 验证)
4. CORS                     (AdminCors Policy)
5. StaticFiles / SPA        (如果启用)
6. MapControllers           (API 路由)
7. MapFallbackToFile        (SPA 兜底路由)
```

> 中间件顺序在 [`Program.cs`](src/HysteriaAuth.Master/Program.cs:120-183) 中定义，顺序敏感不可调整。
