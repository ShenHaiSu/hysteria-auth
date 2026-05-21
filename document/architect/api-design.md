# API 接口设计

> **父文档**: [架构文档目录](README.md) | **关联**: [`../hysteria/hysteria-server-config.md`](../hysteria/hysteria-server-config.md) · [`../hysteria/hysteria-traffic-stats-api.md`](../hysteria/hysteria-traffic-stats-api.md) · [`edge-node-design.md`](edge-node-design.md) · [`security-design.md`](security-design.md)

---

## 1. 统一响应格式与错误处理

所有 API 响应遵循统一格式。成功和列表响应使用直接数据对象，错误响应使用标准错误结构。

### 1.1 统一错误响应格式

```
HTTP 状态码: 4xx / 5xx
Content-Type: application/json
```

```json
{
    "error": {
        "code": "invalid_credentials",
        "message": "用户名或密码错误",
        "requestId": "req_abc123def456"
    }
}
```

| 字段 | 类型 | 说明 |
|------|------|------|
| `error.code` | string | 机器可读的错误代码，供程序化处理 |
| `error.message` | string | 人类可读的错误描述 |
| `error.requestId` | string | 请求追踪ID（对应日志中的 `RequestId`） |

### 1.2 全局错误代码

| 错误代码 | HTTP 状态码 | 说明 |
|----------|-------------|------|
| `invalid_credentials` | 401 | 用户名或密码错误 |
| `token_expired` | 401 | JWT Token 已过期 |
| `token_invalid` | 401 | JWT Token 无效 |
| `unauthorized` | 401 | 未提供认证凭据 |
| `forbidden` | 403 | 权限不足 |
| `account_disabled` | 403 | 账号已禁用 |
| `account_expired` | 403 | 账号已过期 |
| `account_locked` | 403 | 账号已锁定（登录失败次数过多） |
| `traffic_exhausted` | 403 | 流量已用尽 |
| `node_not_allowed` | 403 | 不允许使用该节点 |
| `node_secret_invalid` | 403 | 节点密钥无效 |
| `not_found` | 404 | 资源不存在 |
| `conflict` | 409 | 资源冲突（如用户名重复） |
| `idempotency_conflict` | 409 | 幂等键冲突（重复上报） |
| `rate_limited` | 429 | 请求过于频繁 |
| `validation_error` | 422 | 请求参数校验失败 |
| `internal_error` | 500 | 服务器内部错误 |

---

## 2. 认证 API 双层设计

本项目采用**双层认证架构**：

| 层级 | 端点 | 通信方 | 协议 |
|------|------|--------|------|
| **第一层**（原生协议） | `POST /auth` (Edge Agent 本地) | Hysteria Server → Edge Agent | [Hysteria 原生 HTTP Auth](../hysteria/hysteria-server-config.md#91-http-验证本项目的核心集成方式) |
| **第二层**（内部协议） | `POST /api/v1/auth/hysteria` (主服务器) | Edge Agent → 主服务器 | 项目自定义协议 |

### 2.1 第一层：Hysteria 原生认证请求

当客户端连接时，Hysteria 服务端向 Edge Agent 发送 `POST` 请求（遵循[官方协议](../hysteria/hysteria-server-config.md#91-http-验证本项目的核心集成方式)）：

```
POST /auth
Content-Type: application/json
```

**请求体（Hysteria 原生格式）：**

```json
{
    "addr": "123.123.123.123:44556",
    "auth": "user_password_string",
    "tx": 52428800
}
```

| 字段 | 类型 | 说明 |
|------|------|------|
| `addr` | string | 客户端地址和端口 |
| `auth` | string | 客户端提交的密码（Base64 编码后的认证串） |
| `tx` | uint64 | 客户端期望的发送速率（字节/秒，服务端视角 = 客户端下载速率） |

**Edge Agent 返回给 Hysteria（HTTP 200）：**

```json
{
    "ok": true,
    "id": "username_identifier"
}
```

> **关键点**: Hysteria 原生协议不区分 `username` 和 `password`，仅通过 `auth` 字段传递认证凭据。Edge Agent 的 `AuthProxy` 负责解析凭据，提取用户名和密码后再调用主服务器 API。

### 2.2 第二层：主服务器内部认证 API

Edge Agent 完成协议转换后，向主服务器发起认证请求：

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

**成功响应（HTTP 200）：**

```json
{
    "success": true,
    "userId": 12345,
    "message": "Authentication successful",
    "remainingTraffic": 1073741824,
    "expiresAt": "2025-12-31T23:59:59Z"
}
```

**失败响应（HTTP 401/403）：**

```json
{
    "error": {
        "code": "invalid_credentials",
        "message": "用户名或密码错误",
        "requestId": "req_a1b2c3d4"
    }
}
```

**失败原因枚举：**

| 原因代码 | 说明 | HTTP 状态码 |
|----------|------|-------------|
| `invalid_credentials` | 用户名或密码错误 | 401 |
| `account_disabled` | 账号已禁用 | 403 |
| `account_expired` | 账号已过期 | 403 |
| `traffic_exhausted` | 流量已用尽 | 403 |
| `node_not_allowed` | 不允许使用该节点 | 403 |
| `internal_error` | 服务器内部错误 | 500 |

### 2.3 协议转换对照

Edge Agent 在 `POST /auth` 和 `POST /api/v1/auth/hysteria` 之间进行协议转换：

| 对比维度 | Hysteria 原生协议 | 项目内部 API |
|----------|-------------------|--------------|
| 认证字段 | `auth` (密码) | `username` + `password` |
| 地址字段 | `addr` | `clientIp` |
| 节点标识 | 无 | `nodeId`（由 Edge Agent 配置提供） |
| 速率信息 | `tx`（字节/秒） | 含在请求体中 |
| 成功响应 | `{"ok": true, "id": "..."}` | `{"success": true, "userId": ..., ...}` |

---

## 3. 用户管理 API

所有用户管理 API 需要管理员认证（`Authorization: Bearer {admin_token}`）。

### 3.1 创建用户

```
POST /api/v1/users
Content-Type: application/json
Authorization: Bearer {admin_token}
```

**请求体：**

```json
{
    "username": "user123",
    "password": "password123",
    "email": "user@example.com",
    "totalTrafficBytes": 10737418240,
    "isActive": true,
    "expiresAt": "2025-12-31T23:59:59Z",
    "allowedNodes": ["node-01", "node-02"],
    "remark": "测试用户"
}
```

**响应（HTTP 201）：**

```json
{
    "id": 1,
    "username": "user123",
    "email": "user@example.com",
    "totalTrafficBytes": 10737418240,
    "usedTrafficBytes": 0,
    "isActive": true,
    "createdAt": "2025-01-01T00:00:00Z",
    "expiresAt": "2025-12-31T23:59:59Z",
    "allowedNodes": ["node-01", "node-02"]
}
```

### 3.2 获取用户列表

```
GET /api/v1/users?page=1&pageSize=20&search=&isActive=true&nodeId=
Authorization: Bearer {admin_token}
```

| 查询参数 | 类型 | 默认值 | 说明 |
|----------|------|--------|------|
| `page` | int | `1` | 页码 |
| `pageSize` | int | `20` | 每页条数（最大 100） |
| `search` | string | — | 搜索用户名或邮箱 |
| `isActive` | bool/null | — | 按激活状态筛选（null=全部） |
| `nodeId` | string | — | 按允许节点筛选 |

**响应（HTTP 200）：**

```json
{
    "total": 100,
    "page": 1,
    "pageSize": 20,
    "items": [
        {
            "id": 1,
            "username": "user123",
            "email": "user@example.com",
            "totalTrafficBytes": 10737418240,
            "usedTrafficBytes": 1073741824,
            "isActive": true,
            "createdAt": "2025-01-01T00:00:00Z",
            "expiresAt": "2025-12-31T23:59:59Z"
        }
    ]
}
```

### 3.3 获取用户详情

```
GET /api/v1/users/{userId}
Authorization: Bearer {admin_token}
```

### 3.4 更新用户

```
PUT /api/v1/users/{userId}
Content-Type: application/json
Authorization: Bearer {admin_token}
```

> 支持部分更新，仅需传递要修改的字段。

### 3.5 删除用户

```
DELETE /api/v1/users/{userId}
Authorization: Bearer {admin_token}
```

> 删除为软删除模式，实际将 `IsActive` 设为 `false`。该用户的历史流量数据和认证日志保留不删除。

### 3.6 重置用户流量

```
POST /api/v1/users/{userId}/reset-traffic
Authorization: Bearer {admin_token}
```

> 将 `UsedTrafficBytes` 重置为 `0`，不影响历史 `TrafficRecords`。

### 3.7 获取用户流量统计

```
GET /api/v1/users/{userId}/traffic-stats?period=month
Authorization: Bearer {admin_token}
```

| 查询参数 | 类型 | 说明 |
|----------|------|------|
| `period` | string | 统计周期：`day`、`week`、`month`、`all` |

---

## 4. 节点管理 API

### 4.0 预注册节点（管理员操作）

> 管理员在主服务器上预注册一个边缘节点，生成预注册令牌。边缘节点使用该令牌完成首次注册。

```
POST /api/v1/admin/nodes/pre-register
Content-Type: application/json
Authorization: Bearer {admin_token}
```

**请求体：**

```json
{
    "name": "东京节点",
    "location": "Tokyo, Japan",
    "port": 443,
    "trafficStatsPort": 9999
}
```

**响应（HTTP 201）：**

```json
{
    "provisionToken": "prov_a1b2c3d4e5f6g7h8i9j0",
    "masterServerUrl": "https://master.example.com",
    "expiresAt": "2025-12-31T23:59:59Z",
    "startupCommand": "./edge-agent --provision-token=prov_a1b2c3d4e5f6g7h8i9j0 --master-url=https://master.example.com"
}
```

| 字段 | 类型 | 说明 |
|------|------|------|
| `provisionToken` | string | 预注册令牌（128-bit 随机字符串），边缘节点首次注册时使用 |
| `masterServerUrl` | string | 主服务器地址 |
| `expiresAt` | datetime | 令牌过期时间（默认 7 天） |
| `startupCommand` | string | 一键启动命令（供管理员直接复制使用） |

### 4.1 边缘节点首次注册（使用预注册令牌）

> 边缘节点启动时携带预注册令牌，主服务器验证后下发完整配置。

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
    "agentVersion": "1.0.0"
}
```

**响应（HTTP 200）：**

```json
{
    "nodeId": "edge-node-01",
    "nodeSecret": "随机生成的 256-bit 密钥",
    "trafficStatsSecret": "随机生成的 trafficStats 密钥",
    "config": {
        "authProxyPort": 8080,
        "healthCheckPort": 8081
    }
}
```

> **注册成功后**：主服务器将 `ProvisionToken` 清零，`ProvisionStatus` 设为 `provisioned`。同一令牌不可重复使用。

### 4.2 获取节点配置（边缘节点心跳前拉取）

> 边缘节点在每次心跳前可调用此接口，确保本地配置与主服务器同步。

```
GET /api/v1/nodes/{nodeId}/config
X-Node-Secret: {node_secret}
```

**响应（HTTP 200）：**

```json
{
    "nodeId": "edge-node-01",
    "nodeSecret": "当前有效密钥",
    "isActive": true,
    "config": {
        "authProxyPort": 8080,
        "healthCheckPort": 8081,
        "trafficStatsPort": 9999,
        "collectIntervalSeconds": 30
    }
}
```

### 4.3 注册节点（旧版，保留兼容）

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

### 4.2 节点心跳/状态上报（含流量数据）

> 此接口同时承载系统状态上报和用户流量数据上报。Edge Agent 将本地 Hysteria 流量采集结果合并到心跳请求中。

```
POST /api/v1/nodes/{nodeId}/heartbeat
Content-Type: application/json
X-Node-Secret: {node_secret}
```

**请求体：**

```json
{
    "nodeId": "edge-node-01",
    "cpuUsagePercent": 45.2,
    "memoryUsagePercent": 62.5,
    "memoryUsedMb": 2048,
    "memoryTotalMb": 4096,
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

| 流量字段 | 类型 | 说明 |
|----------|------|------|
| `userTraffic` | object | 从 Hysteria `GET /traffic?clear=1` 采集的各用户流量（[API 参考](../hysteria/hysteria-traffic-stats-api.md#get-traffic--查询用户流量)） |
| `userTraffic.{user}.tx` | int64 | 服务端发送字节数（= 用户下载流量）→ 记录为 `BytesOut` |
| `userTraffic.{user}.rx` | int64 | 服务端接收字节数（= 用户上传流量）→ 记录为 `BytesIn` |
| `onlineUsers` | object | 从 Hysteria `GET /online` 采集的在线用户连接数（[API 参考](../hysteria/hysteria-traffic-stats-api.md#get-online--查询在线用户)） |

### 4.3 获取节点列表

```
GET /api/v1/nodes?isActive=true
Authorization: Bearer {admin_token}
```

### 4.4 获取节点详情

```
GET /api/v1/nodes/{nodeId}
Authorization: Bearer {admin_token}
```

### 4.5 获取节点历史状态

```
GET /api/v1/nodes/{nodeId}/status-history?hours=24
Authorization: Bearer {admin_token}
```

---

## 5. 管理 API

### 5.1 管理员登录

```
POST /api/v1/admin/login
Content-Type: application/json
```

**请求体：**

```json
{
    "username": "admin",
    "password": "admin_password"
}
```

**响应（HTTP 200）：**

```json
{
    "token": "eyJhbGciOiJIUzI1NiIs...",
    "expiresAt": "2025-01-02T00:00:00Z",
    "admin": {
        "id": 1,
        "username": "admin",
        "role": "super_admin"
    }
}
```

> **登录失败处理**：连续失败 5 次锁定 15 分钟（可配置）。成功登录后重置失败计数器。

### 5.2 系统概览

```
GET /api/v1/admin/dashboard
Authorization: Bearer {admin_token}
```

**响应（HTTP 200）：**

```json
{
    "totalUsers": 1000,
    "activeUsers": 850,
    "totalNodes": 10,
    "activeNodes": 8,
    "onlineUsersNow": 342,
    "totalTrafficToday": 1099511627776,
    "totalTrafficThisMonth": 32985348833280
}
```

### 5.3 踢用户下线

```
POST /api/v1/admin/kick-user
Content-Type: application/json
Authorization: Bearer {admin_token}
```

**请求体：**

```json
{
    "username": "user123",
    "nodeId": "edge-node-01"
}
```

> 主服务器通过 Edge Agent 调用 Hysteria [`POST /kick`](../hysteria/hysteria-traffic-stats-api.md#post-kick--踢用户下线) 接口。由于 Hysteria 客户端内置重连逻辑，建议同时通过 `PUT /api/v1/users/{userId}` 将用户 `isActive` 设为 `false`。

### 5.4 创建管理员

```
POST /api/v1/admin/admins
Content-Type: application/json
Authorization: Bearer {admin_token}
```

> 仅 `super_admin` 角色可调用。

**请求体：**

```json
{
    "username": "new_admin",
    "password": "secure_password",
    "role": "admin"
}
```

### 5.5 获取管理员列表

```
GET /api/v1/admin/admins
Authorization: Bearer {admin_token}
```

> 仅 `super_admin` 角色可调用。

### 5.6 更新管理员

```
PUT /api/v1/admin/admins/{adminId}
Content-Type: application/json
Authorization: Bearer {admin_token}
```

> 仅 `super_admin` 角色可调用。支持修改角色、激活/禁用、重置密码。

### 5.7 获取审计日志

```
GET /api/v1/admin/audit-logs?page=1&pageSize=50&adminId=&action=&targetType=&startTime=&endTime=
Authorization: Bearer {admin_token}
```

> 仅 `super_admin` 和 `admin` 角色可调用。

---

## 6. 健康检查 API

### 6.1 主服务器健康检查

```
GET /health
```

无需认证，供负载均衡器和监控系统使用。

**响应（HTTP 200）：**

```json
{
    "status": "healthy",
    "timestamp": "2025-01-01T12:00:00Z",
    "version": "1.0.0",
    "uptime": "72h15m30s",
    "checks": {
        "database": "ok",
        "disk_space": "ok"
    }
}
```

| 状态值 | 说明 |
|--------|------|
| `healthy` | 所有检查通过 |
| `degraded` | 部分检查警告（如磁盘空间不足） |
| `unhealthy` | 关键检查失败（如数据库不可达） |

### 6.2 Edge Agent 健康检查

Edge Agent 本地暴露健康检查端点，供外部监控或主服务器按需探活：

```
GET /health
```

**响应（HTTP 200）：**

```json
{
    "status": "healthy",
    "nodeId": "edge-node-01",
    "timestamp": "2025-01-01T12:00:00Z",
    "checks": {
        "hysteria_reachable": true,
        "master_reachable": true,
        "traffic_collector": "running",
        "system_monitor": "running"
    }
}
```
