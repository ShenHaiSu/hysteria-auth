# Master 主节点面板 API 接口文档

> **文档定位**：前端开发人员**核心参考文档**。涵盖 Web 管理控制台需要对接的所有 API 接口。
> **Base URL**: `https://{master-host}/api/v1`
> **认证方式**: `Authorization: Bearer {admin_token}`（JWT Token，通过登录接口获取）
> **相关文档**：[`agent-api-reference.md`](agent-api-reference.md) · [`master-internal-api.md`](master-internal-api.md)

---

## 目录

- [1. 通用说明](#1-通用说明)
  - [1.1 统一错误响应格式](#11-统一错误响应格式)
  - [1.2 全局错误代码表](#12-全局错误代码表)
  - [1.3 JWT Token 说明](#13-jwt-token-说明)
  - [1.4 分页响应格式](#14-分页响应格式)
- [2. 管理员认证](#2-管理员认证)
  - [2.1 管理员登录](#21-post-apiv1adminlogin--管理员登录)
- [3. 仪表盘](#3-仪表盘)
  - [3.1 系统概览](#31-get-apiv1admindashboard--系统概览)
- [4. 用户管理](#4-用户管理)
  - [4.1 创建用户](#41-post-apiv1users--创建用户)
  - [4.2 获取用户列表](#42-get-apiv1users--获取用户列表)
  - [4.3 获取用户详情](#43-get-apiv1usersuserid--获取用户详情)
  - [4.4 更新用户](#44-put-apiv1usersuserid--更新用户)
  - [4.5 删除用户（软删除）](#45-delete-apiv1usersuserid--删除用户)
  - [4.6 重置用户流量](#46-post-apiv1usersuseridreset-traffic--重置用户流量)
  - [4.7 获取用户流量统计](#47-get-apiv1usersuseridtraffic-stats--获取用户流量统计)
- [5. 节点管理](#5-节点管理)
  - [5.1 预注册节点](#51-post-apiv1adminnodespre-register--预注册节点)
  - [5.2 获取节点列表](#52-get-apiv1nodes--获取节点列表)
  - [5.3 获取节点详情](#53-get-apiv1nodesnodeid--获取节点详情)
  - [5.4 获取节点历史状态](#54-get-apiv1nodesnodeidstatus-history--获取节点历史状态)
  - [5.5 轮换节点密钥](#55-post-apiv1adminnodesnodeidrotate-secret--轮换节点密钥)
- [6. 在线用户管理](#6-在线用户管理)
  - [6.1 踢用户下线](#61-post-apiv1adminkick-user--踢用户下线)
- [7. 管理员管理（super_admin）](#7-管理员管理super_admin)
  - [7.1 创建管理员](#71-post-apiv1adminadmins--创建管理员)
  - [7.2 获取管理员列表](#72-get-apiv1adminadmins--获取管理员列表)
  - [7.3 更新管理员](#73-put-apiv1adminadminsadminid--更新管理员)
- [8. 审计日志](#8-审计日志)
  - [8.1 获取审计日志](#81-get-apiv1adminaudit-logs--获取审计日志)

---

## 1. 通用说明

### 1.1 统一错误响应格式

所有 API 错误响应采用统一结构：

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
| `error.code` | string | **机器可读**的错误代码，前端可据此做程序化处理（如跳转登录页、显示不同提示） |
| `error.message` | string | **人类可读**的错误描述，可直接展示给用户 |
| `error.requestId` | string | 请求追踪 ID，排查问题时提供给后端 |

> **注意**：成功响应**不包裹**在统一格式中，直接返回业务数据。

### 1.2 全局错误代码表

| 错误代码 | HTTP 状态码 | 说明 | 前端建议处理 |
|----------|-------------|------|-------------|
| `invalid_credentials` | 401 | 用户名或密码错误 | 提示用户重新输入 |
| `token_expired` | 401 | JWT Token 已过期 | 跳转登录页 |
| `token_invalid` | 401 | JWT Token 无效（篡改/格式错误） | 跳转登录页 |
| `unauthorized` | 401 | 未提供认证凭据 | 跳转登录页 |
| `forbidden` | 403 | 权限不足（非 super_admin 操作受限资源） | 提示"无权限" |
| `account_disabled` | 403 | 管理员账号已禁用 | 提示联系超级管理员 |
| `account_locked` | 403 | 管理员账号已锁定（登录失败次数过多） | 提示"X 分钟后再试" |
| `not_found` | 404 | 资源不存在 | 提示"数据不存在" |
| `conflict` | 409 | 资源冲突（如用户名已存在） | 提示"X 已存在" |
| `validation_error` | 422 | 请求参数校验失败 | 展示字段级错误 |
| `rate_limited` | 429 | 请求过于频繁 | 提示稍后再试 |
| `internal_error` | 500 | 服务器内部错误 | 提示"系统错误，请联系管理员" |

### 1.3 JWT Token 说明

- **获取方式**：通过 [`POST /api/v1/admin/login`](#21-post-apiv1adminlogin--管理员登录) 获取
- **默认有效期**：1440 分钟（24小时），由 [`appsettings.json`](src/HysteriaAuth.Master/appsettings.json:13) 中 `Jwt.ExpirationMinutes` 配置
- **刷新窗口**：过期前 5 分钟内可刷新（当前版本刷新机制待实现）
- **传递方式**：请求头 `Authorization: Bearer {token}`
- **Token 中携带的 Claims**：
  - `nameid` → 管理员 ID（`AdminId`）
  - `role` → 管理员角色（`super_admin` / `admin` / `readonly`）

### 1.4 分页响应格式

所有列表接口（用户列表、节点列表、管理员列表、审计日志）均返回以下结构：

```json
{
    "total": 100,
    "page": 1,
    "pageSize": 20,
    "items": [ /* 数据列表 */ ]
}
```

| 字段 | 类型 | 说明 |
|------|------|------|
| `total` | int | 总记录数 |
| `page` | int | 当前页码（从 1 开始） |
| `pageSize` | int | 每页条数 |
| `items` | array | 当前页数据列表 |

---

## 2. 管理员认证

### 2.1 `POST /api/v1/admin/login` — 管理员登录

> **无需认证**。这是前端获取 JWT Token 的唯一入口。

```
POST /api/v1/admin/login
Content-Type: application/json
```

**请求体：**

```json
{
    "username": "admin",
    "password": "admin123"
}
```

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `username` | string | ✅ | 管理员用户名 |
| `password` | string | ✅ | 管理员密码 |

**成功响应 (HTTP 200)：**

```json
{
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "expiresAt": "2025-01-02T00:00:00Z",
    "admin": {
        "id": 1,
        "username": "admin",
        "role": "super_admin",
        "isActive": true,
        "createdAt": "2025-01-01T00:00:00Z",
        "lastLoginAt": "2025-01-01T12:00:00Z"
    }
}
```

| 字段 | 类型 | 说明 |
|------|------|------|
| `token` | string | JWT Token，后续所有 API 请求需在 `Authorization` 头中携带 |
| `expiresAt` | string (ISO 8601) | Token 过期时间 |
| `admin.id` | long | 管理员 ID |
| `admin.username` | string | 管理员用户名 |
| `admin.role` | string | 角色：`super_admin` / `admin` / `readonly` |
| `admin.isActive` | bool | 是否激活 |
| `admin.createdAt` | string (ISO 8601) | 创建时间 |
| `admin.lastLoginAt` | string (ISO 8601) | 上次登录时间 |

**登录安全策略**（后端自动处理，前端无需额外逻辑）：
- 连续失败 **5** 次 → 账号锁定 **15** 分钟
- 成功登录后自动重置失败计数器

---

## 3. 仪表盘

### 3.1 `GET /api/v1/admin/dashboard` — 系统概览

> **认证**: 需要 JWT Token（所有角色均可）

```
GET /api/v1/admin/dashboard
Authorization: Bearer {admin_token}
```

**成功响应 (HTTP 200)：**

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

| 字段 | 类型 | 说明 |
|------|------|------|
| `totalUsers` | int | 用户总数 |
| `activeUsers` | int | 已激活用户数（`isActive = true`） |
| `totalNodes` | int | 节点总数 |
| `activeNodes` | int | 活跃节点数（最近有心跳） |
| `onlineUsersNow` | int | 当前在线用户数 |
| `totalTrafficToday` | long | 今日总流量（字节） |
| `totalTrafficThisMonth` | long | 本月总流量（字节） |

> **当前阶段说明**：仪表盘数据在 Phase 1 返回占位值（全为 0），后续阶段将接入实时数据。

---

## 4. 用户管理

### 4.1 `POST /api/v1/users` — 创建用户

> **认证**: 需要 JWT Token

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

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `username` | string | ✅ | 用户名（唯一，3-64 字符） |
| `password` | string | ✅ | 密码（BCrypt 加密存储） |
| `email` | string | ❌ | 邮箱地址 |
| `totalTrafficBytes` | long | ❌ | 总流量配额（**字节**）。例如 `10737418240` = 10GB，`0` = 不限 |
| `isActive` | bool | ❌ | 是否激活，默认 `true` |
| `expiresAt` | string (ISO 8601) | ❌ | 过期时间，`null` = 永不过期 |
| `allowedNodes` | string[] | ❌ | 允许使用的节点 ID 列表，`null`/`[]` = 全部节点 |
| `remark` | string | ❌ | 备注信息 |

**成功响应 (HTTP 201)：**

```json
{
    "id": 1,
    "username": "user123",
    "email": "user@example.com",
    "totalTrafficBytes": 10737418240,
    "usedTrafficBytes": 0,
    "isActive": true,
    "createdAt": "2025-01-01T00:00:00Z",
    "updatedAt": "2025-01-01T00:00:00Z",
    "expiresAt": "2025-12-31T23:59:59Z",
    "allowedNodes": "[\"node-01\",\"node-02\"]",
    "remark": "测试用户"
}
```

> **注意**: `allowedNodes` 在响应中为 **JSON 字符串**（数据库中存储为 TEXT），前端需 `JSON.parse()` 处理。

---

### 4.2 `GET /api/v1/users` — 获取用户列表

> **认证**: 需要 JWT Token

```
GET /api/v1/users?page=1&pageSize=20&search=&isActive=true&nodeId=
Authorization: Bearer {admin_token}
```

**查询参数：**

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `page` | int | `1` | 页码 |
| `pageSize` | int | `20` | 每页条数（最大 100） |
| `search` | string | — | 搜索关键词（模糊匹配用户名或邮箱） |
| `isActive` | bool / 不传 | — | 按激活状态筛选。`true`=仅激活，`false`=仅禁用，不传=全部 |
| `nodeId` | string | — | 按允许节点筛选（匹配 `allowedNodes` JSON 数组包含该节点 ID 的用户） |

**成功响应 (HTTP 200)：**

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
            "updatedAt": "2025-01-15T12:00:00Z",
            "expiresAt": "2025-12-31T23:59:59Z",
            "allowedNodes": "[\"node-01\",\"node-02\"]",
            "remark": "测试用户"
        }
    ]
}
```

---

### 4.3 `GET /api/v1/users/{userId}` — 获取用户详情

> **认证**: 需要 JWT Token

```
GET /api/v1/users/1
Authorization: Bearer {admin_token}
```

**路径参数：**

| 参数 | 类型 | 说明 |
|------|------|------|
| `userId` | long | 用户 ID |

**成功响应 (HTTP 200)：** 返回单个 [`UserDto`](src/HysteriaAuth.Master/Models/DTOs/UserDto.cs:3-16) 对象，字段同列表项。

---

### 4.4 `PUT /api/v1/users/{userId}` — 更新用户

> **认证**: 需要 JWT Token
> **支持部分更新**：仅需传递要修改的字段，未传递的字段保持不变。

```
PUT /api/v1/users/1
Content-Type: application/json
Authorization: Bearer {admin_token}
```

**请求体（所有字段可选）：**

```json
{
    "email": "newemail@example.com",
    "totalTrafficBytes": 21474836480,
    "isActive": false,
    "expiresAt": "2026-06-01T00:00:00Z",
    "allowedNodes": ["node-01"],
    "remark": "更新后的备注",
    "password": "newpassword123"
}
```

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `email` | string | ❌ | 邮箱 |
| `totalTrafficBytes` | long | ❌ | 总流量配额（字节），`0` = 不限 |
| `isActive` | bool | ❌ | 是否激活。设为 `false` 可达到**禁用用户**效果 |
| `expiresAt` | string (ISO 8601) | ❌ | 过期时间，`null` = 永不过期 |
| `allowedNodes` | string[] | ❌ | 允许节点列表 |
| `remark` | string | ❌ | 备注 |
| `password` | string | ❌ | 新密码（如需修改密码） |

**成功响应 (HTTP 200)：** 返回更新后的 [`UserDto`](src/HysteriaAuth.Master/Models/DTOs/UserDto.cs:3-16) 对象。

---

### 4.5 `DELETE /api/v1/users/{userId}` — 删除用户

> **认证**: 需要 JWT Token
> **软删除**：实际将 `isActive` 设为 `false`，历史流量数据和认证日志保留。

```
DELETE /api/v1/users/1
Authorization: Bearer {admin_token}
```

**成功响应 (HTTP 204 No Content)：** 无响应体。

---

### 4.6 `POST /api/v1/users/{userId}/reset-traffic` — 重置用户流量

> **认证**: 需要 JWT Token
> 将用户 `usedTrafficBytes` 重置为 `0`，不影响历史 `TrafficRecords`。

```
POST /api/v1/users/1/reset-traffic
Authorization: Bearer {admin_token}
```

**成功响应 (HTTP 200)：**

```json
{
    "message": "流量已重置"
}
```

---

### 4.7 `GET /api/v1/users/{userId}/traffic-stats` — 获取用户流量统计

> **认证**: 需要 JWT Token

```
GET /api/v1/users/1/traffic-stats?period=month
Authorization: Bearer {admin_token}
```

**查询参数：**

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `period` | string | `month` | 统计周期：`day` / `week` / `month` / `all` |

**成功响应 (HTTP 200)：**

```json
{
    "userId": 1,
    "period": "month",
    "totalBytesIn": 5368709120,
    "totalBytesOut": 10737418240,
    "dataPoints": [
        {
            "date": "2025-01-01T00:00:00Z",
            "bytesIn": 1073741824,
            "bytesOut": 2147483648
        },
        {
            "date": "2025-01-02T00:00:00Z",
            "bytesIn": 536870912,
            "bytesOut": 1073741824
        }
    ]
}
```

| 字段 | 类型 | 说明 |
|------|------|------|
| `userId` | long | 用户 ID |
| `period` | string | 统计周期 |
| `totalBytesIn` | long | 周期内总上传流量（字节） |
| `totalBytesOut` | long | 周期内总下载流量（字节） |
| `dataPoints` | array | 按日期聚合的流量数据点 |
| `dataPoints[].date` | string (ISO 8601) | 日期 |
| `dataPoints[].bytesIn` | long | 当日上传流量（字节） |
| `dataPoints[].bytesOut` | long | 当日下载流量（字节） |

> **流量方向说明**：`bytesIn` = 用户上传流量，`bytesOut` = 用户下载流量。前端展示时建议标注清楚。

---

## 5. 节点管理

### 5.1 `POST /api/v1/admin/nodes/pre-register` — 预注册节点

> **认证**: 需要 JWT Token
> 在主服务器上预注册一个边缘节点，生成**一次性预注册令牌**，供 Edge Agent 首次启动时使用。

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

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `name` | string | ✅ | 节点名称 |
| `location` | string | ❌ | 节点位置描述 |
| `port` | int | ❌ | Hysteria 服务端口，默认 `443` |
| `trafficStatsPort` | int | ❌ | Hysteria trafficStats API 端口，默认自动分配 |

**成功响应 (HTTP 201)：**

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
| `provisionToken` | string | **一次性**预注册令牌，Edge Agent 首次注册后即失效 |
| `masterServerUrl` | string | 主服务器地址（由 [`appsettings.json`](src/HysteriaAuth.Master/appsettings.json:5) 配置） |
| `expiresAt` | string (ISO 8601) | 令牌过期时间 |
| `startupCommand` | string | 一键启动命令，可直接复制到边缘节点执行 |

> **前端提示**：`startupCommand` 可直接展示给运维人员复制使用。令牌过期后需重新预注册。

---

### 5.2 `GET /api/v1/nodes` — 获取节点列表

> **认证**: 需要 JWT Token

```
GET /api/v1/nodes?page=1&pageSize=20&isActive=true
Authorization: Bearer {admin_token}
```

**查询参数：**

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `page` | int | `1` | 页码 |
| `pageSize` | int | `20` | 每页条数 |
| `isActive` | bool / 不传 | — | 按激活状态筛选。不传=全部 |

**成功响应 (HTTP 200)：**

```json
{
    "total": 10,
    "page": 1,
    "pageSize": 20,
    "items": [
        {
            "id": "edge-node-01",
            "name": "东京节点",
            "ipAddress": "10.0.0.1",
            "port": 443,
            "isActive": true,
            "createdAt": "2025-01-01T00:00:00Z",
            "lastHeartbeat": "2025-01-15T12:00:00Z",
            "location": "Tokyo, Japan",
            "trafficStatsPort": 9999,
            "provisionStatus": "provisioned"
        }
    ]
}
```

| 字段 | 类型 | 说明 |
|------|------|------|
| `id` | string | 节点唯一标识 |
| `name` | string | 节点名称 |
| `ipAddress` | string | 节点 IP 地址 |
| `port` | int | Hysteria 服务端口 |
| `isActive` | bool | 是否激活（管理员可控制） |
| `createdAt` | string (ISO 8601) | 创建时间 |
| `lastHeartbeat` | string (ISO 8601) / null | 最后心跳时间。`null` = 从未上报 |
| `location` | string | 节点位置描述 |
| `trafficStatsPort` | int | Hysteria trafficStats API 端口 |
| `provisionStatus` | string | 预注册状态：`pending`(待注册) / `provisioned`(已注册) |

> **判断节点在线**：`isActive == true` 且 `lastHeartbeat` 距当前 < 90 秒（可配）。

---

### 5.3 `GET /api/v1/nodes/{nodeId}` — 获取节点详情

> **认证**: 需要 JWT Token

```
GET /api/v1/nodes/edge-node-01
Authorization: Bearer {admin_token}
```

**成功响应 (HTTP 200)：**

```json
{
    "id": "edge-node-01",
    "name": "东京节点",
    "ipAddress": "10.0.0.1",
    "port": 443,
    "isActive": true,
    "createdAt": "2025-01-01T00:00:00Z",
    "lastHeartbeat": "2025-01-15T12:00:00Z",
    "location": "Tokyo, Japan",
    "trafficStatsPort": 9999,
    "provisionStatus": "provisioned",
    "secretVersion": 1,
    "trafficStatsSecret": "encrypted_secret_value"
}
```

> 除包含列表字段外，额外包含 `secretVersion`（密钥版本号）和 `trafficStatsSecret`（加密存储的密钥，仅供参考）。

---

### 5.4 `GET /api/v1/nodes/{nodeId}/status-history` — 获取节点历史状态

> **认证**: 需要 JWT Token
> 返回指定时间范围内的节点资源使用历史数据，用于绘制监控图表。

```
GET /api/v1/nodes/edge-node-01/status-history?hours=24
Authorization: Bearer {admin_token}
```

**查询参数：**

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `hours` | int | `24` | 查询最近 N 小时的历史数据 |

**成功响应 (HTTP 200)：**

```json
{
    "nodeId": "edge-node-01",
    "hours": 24,
    "items": [
        {
            "id": 1001,
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
            "reportedAt": "2025-01-01T12:00:00Z"
        }
    ]
}
```

| 字段 | 类型 | 说明 | 图表建议 |
|------|------|------|---------|
| `cpuUsagePercent` | float | CPU 使用率 (%) | 折线图 |
| `memoryUsagePercent` | float | 内存使用率 (%) | 折线图 |
| `memoryUsedMb` | float | 已用内存 (MB) | 面积图 |
| `memoryTotalMb` | float | 总内存 (MB) | 参考线 |
| `networkInBytes` | long | 累计网络流入（字节） | 累计值 |
| `networkOutBytes` | long | 累计网络流出（字节） | 累计值 |
| `networkInMbps` | float | 当前网络流入速率 (Mbps) | 折线图 |
| `networkOutMbps` | float | 当前网络流出速率 (Mbps) | 折线图 |
| `activeConnections` | int | 活跃连接数 | 折线图 |
| `reportedAt` | string (ISO 8601) | 上报时间 | X 轴时间 |

---

### 5.5 `POST /api/v1/admin/nodes/{nodeId}/rotate-secret` — 轮换节点密钥

> **认证**: 需要 JWT Token
> 生成新的节点通信密钥，递增 `SecretVersion`。旧密钥在过渡期内仍然有效。

```
POST /api/v1/admin/nodes/edge-node-01/rotate-secret
Authorization: Bearer {admin_token}
```

**成功响应 (HTTP 200)：**

```json
{
    "nodeId": "edge-node-01",
    "newSecret": "a1b2c3d4e5f6...",
    "newSecretVersion": 2
}
```

---

## 6. 在线用户管理

### 6.1 `POST /api/v1/admin/kick-user` — 踢用户下线

> **认证**: 需要 JWT Token
> 主服务器通过 Edge Agent 调用 Hysteria `/kick` 接口。**建议同时将用户 `isActive` 设为 `false`**，防止客户端自动重连。

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

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `username` | string | ✅ | 要踢下线的用户名 |
| `nodeId` | string | ✅ | 目标节点 ID（用户当前所在的边缘节点） |

**成功响应 (HTTP 200)：**

```json
{
    "message": "用户 user123 已踢下线"
}
```

> **前端交互建议**：踢人操作确认框中提示"该操作仅断开当前连接，客户端可能会自动重连。建议同时禁用该用户账号"。

---

## 7. 管理员管理（super_admin）

> 以下接口**仅 `super_admin` 角色可调用**。前端应根据 `admin.role` 判断是否展示管理入口。

### 7.1 `POST /api/v1/admin/admins` — 创建管理员

> **认证**: 需要 JWT Token（仅 `super_admin`）

```
POST /api/v1/admin/admins
Content-Type: application/json
Authorization: Bearer {admin_token}
```

**请求体：**

```json
{
    "username": "new_admin",
    "password": "secure_password",
    "role": "admin"
}
```

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `username` | string | ✅ | 管理员用户名（唯一） |
| `password` | string | ✅ | 管理员密码 |
| `role` | string | ❌ | 角色：`super_admin` / `admin` / `readonly`，默认 `admin` |

**成功响应 (HTTP 201)：**

```json
{
    "id": 2,
    "username": "new_admin",
    "role": "admin",
    "isActive": true,
    "createdAt": "2025-01-15T12:00:00Z",
    "lastLoginAt": null
}
```

---

### 7.2 `GET /api/v1/admin/admins` — 获取管理员列表

> **认证**: 需要 JWT Token（仅 `super_admin`）

```
GET /api/v1/admin/admins?page=1&pageSize=20
Authorization: Bearer {admin_token}
```

**成功响应 (HTTP 200)：** 分页列表，每项为 [`AdminDto`](src/HysteriaAuth.Master/Models/DTOs/AdminDto.cs:3-11)。

---

### 7.3 `PUT /api/v1/admin/admins/{adminId}` — 更新管理员

> **认证**: 需要 JWT Token（仅 `super_admin`）
> 支持修改角色、激活/禁用、重置密码。

```
PUT /api/v1/admin/admins/2
Content-Type: application/json
Authorization: Bearer {admin_token}
```

**请求体（所有字段可选）：**

```json
{
    "role": "readonly",
    "isActive": false,
    "password": "new_password"
}
```

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `role` | string | ❌ | 角色：`super_admin` / `admin` / `readonly` |
| `isActive` | bool | ❌ | 是否激活。设为 `false` 可**禁用管理员** |
| `password` | string | ❌ | 新密码 |

**成功响应 (HTTP 200)：** 返回更新后的 [`AdminDto`](src/HysteriaAuth.Master/Models/DTOs/AdminDto.cs:3-11)。

---

## 8. 审计日志

### 8.1 `GET /api/v1/admin/audit-logs` — 获取审计日志

> **认证**: 需要 JWT Token（`super_admin` 和 `admin` 可调用）

```
GET /api/v1/admin/audit-logs?page=1&pageSize=50&adminId=&action=&targetType=&startTime=&endTime=
Authorization: Bearer {admin_token}
```

**查询参数：**

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `page` | int | `1` | 页码 |
| `pageSize` | int | `50` | 每页条数 |
| `adminId` | long | — | 按操作管理员 ID 筛选 |
| `action` | string | — | 按操作类型筛选：`create` / `update` / `delete` / `login` / `logout` / `kick_user` 等 |
| `targetType` | string | — | 按操作目标类型筛选：`user` / `node` / `admin` / `system` |
| `startTime` | string (ISO 8601) | — | 开始时间 |
| `endTime` | string (ISO 8601) | — | 结束时间 |

**成功响应 (HTTP 200)：**

```json
{
    "total": 500,
    "page": 1,
    "pageSize": 50,
    "items": [
        {
            "id": 1001,
            "adminId": 1,
            "adminName": "admin",
            "action": "create",
            "targetType": "user",
            "targetId": "42",
            "detail": "{\"username\":\"user123\",\"totalTrafficBytes\":10737418240}",
            "clientIp": "192.168.1.100",
            "createdAt": "2025-01-15T12:00:00Z"
        }
    ]
}
```

| 字段 | 类型 | 说明 |
|------|------|------|
| `id` | long | 日志 ID |
| `adminId` | long | 操作管理员 ID |
| `adminName` | string | 操作管理员用户名 |
| `action` | string | 操作类型 |
| `targetType` | string | 操作目标类型 |
| `targetId` | string | 操作目标 ID |
| `detail` | string | 操作详情（**JSON 字符串**，含变更前后对比） |
| `clientIp` | string | 操作来源 IP |
| `createdAt` | string (ISO 8601) | 操作时间 |

> **前端展示建议**：`detail` 为 JSON 字符串，可格式化展示变更详情。审计日志**不可删除**。

---

## 附录 A：HTTP 状态码速查

| 状态码 | 含义 | 常见场景 |
|--------|------|---------|
| `200` | 成功 | GET/PUT 操作成功，POST 非创建操作成功 |
| `201` | 已创建 | POST 创建资源成功 |
| `204` | 无内容 | DELETE 操作成功 |
| `400` | 请求错误 | 请求体格式错误 |
| `401` | 未认证 | Token 缺失/无效/过期 |
| `403` | 无权限 | 角色权限不足 |
| `404` | 未找到 | 资源不存在 |
| `409` | 冲突 | 用户名已存在等 |
| `422` | 校验失败 | 参数不合法 |
| `429` | 频率限制 | 请求过快 |
| `500` | 服务器错误 | 内部异常 |

## 附录 B：认证头格式

所有需要认证的请求必须携带：

```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

- 格式：`Bearer` + 空格 + Token
- Token 通过 [`POST /api/v1/admin/login`](#21-post-apiv1adminlogin--管理员登录) 获取
- Token 过期后需重新登录
