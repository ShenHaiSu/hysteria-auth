# Hysteria 认证后端系统 — 后端开发规范

> **文档版本**: v1.0 | **制定日期**: 2026-05-21 | **适用范围**: 主服务器 (Master Server) + 边缘节点 (Edge Agent) 全部后端开发
>
> **来源**: 本规范提取自 [`document/architect/`](../architect/README.md) 全部架构设计文档，所有后端开发人员**必须严格遵守**。

---

## 目录

1. [时间戳规范](#1-时间戳规范)
2. [通用编码规范](#2-通用编码规范)
3. [API 规范](#3-api-规范)
4. [数据库规范](#4-数据库规范)
5. [认证流程规范](#5-认证流程规范)
6. [安全规范](#6-安全规范)
7. [流量统计规范](#7-流量统计规范)
8. [并发控制与数据一致性规范](#8-并发控制与数据一致性规范)
9. [异常处理与降级规范](#9-异常处理与降级规范)
10. [日志与监控规范](#10-日志与监控规范)
11. [测试规范](#11-测试规范)
12. [配置规范](#12-配置规范)
13. [部署规范](#13-部署规范)

---

## 1. 时间戳规范

> **这是全局强制规范，没有任何例外。**

### 1.1 核心规则

| 规则 | 说明 |
|------|------|
| **存储格式** | 所有时间戳在数据库中、请求体中、响应体中**必须**使用 ISO 8601 UTC 格式 |
| **格式模板** | `yyyy-MM-ddTHH:mm:ssZ`（如 `2025-12-31T23:59:59Z`） |
| **精度** | 秒级精度即可；如需毫秒级，使用 `yyyy-MM-ddTHH:mm:ss.fffZ` |
| **时区** | 始终为 UTC（`Z` 后缀），**禁止**使用 `+08:00` 等偏移表示 |

### 1.2 各层要求

| 层 | 要求 |
|----|------|
| **数据库** | 所有 `DATETIME` 字段存储 UTC 时间。写入前由应用层转换为 UTC，读取时不自动转换。 |
| **C# 代码** | 始终使用 [`DateTime.UtcNow`](https://learn.microsoft.com/en-us/dotnet/api/system.datetime.utcnow) 获取当前时间，**禁止**使用 [`DateTime.Now`](https://learn.microsoft.com/en-us/dotnet/api/system.datetime.now)。 |
| **请求体 (JSON)** | 所有时间戳字段必须是 ISO 8601 UTC 字符串，如 `"expiresAt": "2025-12-31T23:59:59Z"` |
| **响应体 (JSON)** | 所有时间戳字段必须是 ISO 8601 UTC 字符串，如 `"createdAt": "2025-01-01T00:00:00Z"` |
| **日志** | 日志时间戳使用 UTC，格式 `yyyy-MM-ddTHH:mm:ss.fffZ` |

### 1.3 序列化配置

ASP.NET Core JSON 序列化必须配置为 UTC：

```csharp
// Program.cs
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // 将所有 DateTime 序列化为 ISO 8601 UTC
        options.JsonSerializerOptions.Converters.Add(
            new DateTimeConverterUtc());
    });
```

或者使用 [`System.Text.Json`](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/) 的默认行为（默认即为 ISO 8601），配合全局设置：

```csharp
// 确保 DateTime 类型处理时 Kind 为 Utc
// 在 DbContext 中配置
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // 所有 DateTime 属性存储为 UTC
    // SQLite 不存储时区信息，应用层负责保证写入的是 UTC
}
```

### 1.4 当地时间的处理

当业务逻辑需要使用当地时间（如管理界面展示、用户过期判断等）时：

1. **后端始终以 UTC 处理和存储**
2. **仅在前端/客户端**根据设备所在时区进行偏移显示
3. **如果后端必须计算当地时间**，使用 [`TimeZoneInfo.ConvertTimeFromUtc()`](https://learn.microsoft.com/en-us/dotnet/api/system.timezoneinfo.converttimefromutc) 并明确指定目标时区

```csharp
// ✅ 正确：仅在需要时转换
var localTime = TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, targetTimeZone);

// ❌ 错误：直接使用 DateTime.Now
var now = DateTime.Now; // 禁止
```

### 1.5 时间戳字段清单（所有含时间字段）

以下数据库表中的所有时间字段，以及 API 中对应的所有时间字段，均须遵守本规范：

| 表 | 时间字段 |
|----|----------|
| [`Users`](#42-用户表-users) | [`CreatedAt`](#42-用户表-users), [`UpdatedAt`](#42-用户表-users), [`ExpiresAt`](#42-用户表-users) |
| [`TrafficRecords`](#43-流量记录表-trafficrecords) | [`RecordedAt`](#43-流量记录表-trafficrecords) |
| [`AuthLogs`](#44-认证日志表-authlogs) | [`AuthTime`](#44-认证日志表-authlogs) |
| [`Sessions`](#45-会话表-sessions) | [`StartedAt`](#45-会话表-sessions), [`EndedAt`](#45-会话表-sessions) |
| [`Nodes`](#46-节点表-nodes) | [`CreatedAt`](#46-节点表-nodes), [`LastHeartbeat`](#46-节点表-nodes) |
| [`NodeStatus`](#47-节点状态表-nodestatus) | [`ReportedAt`](#47-节点状态表-nodestatus) |
| [`NodeTraffic`](#48-节点流量统计表-nodetraffic) | [`RecordedAt`](#48-节点流量统计表-nodetraffic) |
| [`Admins`](#49-管理员表-admins) | [`CreatedAt`](#49-管理员表-admins), [`LastLoginAt`](#49-管理员表-admins), [`LockedUntil`](#49-管理员表-admins) |
| [`AdminAuditLogs`](#410-管理员操作审计日志表-adminauditlogs) | [`CreatedAt`](#410-管理员操作审计日志表-adminauditlogs) |

---

## 2. 通用编码规范

### 2.1 技术栈

| 组件 | 技术选型 |
|------|----------|
| 开发语言 | C# (.NET 8.0) |
| Web 框架 | ASP.NET Core |
| 数据库 | SQLite |
| ORM | Entity Framework Core |
| 日志 | [`Microsoft.Extensions.Logging`](https://learn.microsoft.com/en-us/dotnet/core/extensions/logging) + 文件日志 |
| 配置管理 | JSON 配置文件 (`appsettings.json`, `agent.json`) |

### 2.2 项目结构

```
src/
├── HysteriaAuth.Master/           # 主服务器项目
│   ├── Controllers/               # 控制器（仅负责路由、参数校验、响应格式化）
│   ├── Services/                  # 业务逻辑服务（事务管理、业务编排）
│   ├── Repositories/              # 数据访问仓储（封装 EF Core 操作）
│   ├── Models/
│   │   ├── Entities/              # EF Core 数据库实体
│   │   ├── DTOs/                  # API 数据传输对象
│   │   └── ViewModel/             # 视图模型
│   ├── Data/
│   │   ├── AppDbContext.cs        # EF Core 数据库上下文
│   │   └── Migrations/            # EF Core 迁移文件
│   ├── Middleware/                 # 中间件（认证、异常、限流、审计）
│   ├── Config/                    # 强类型配置类
│   ├── appsettings.json
│   └── Program.cs
│
└── HysteriaAuth.Agent/            # 边缘节点 Agent 项目
    ├── Services/                  # 系统监控、认证代理、流量采集、状态上报、初始化
    ├── Models/                    # 本地模型
    ├── Config/
    │   └── agent.json
    ├── Middleware/
    └── Program.cs
```

### 2.3 分层职责

| 层 | 职责 |
|----|------|
| **Controllers** | HTTP 请求路由、输入参数校验、响应格式化。**不包含业务逻辑。** |
| **Services** | 业务逻辑编排、事务管理。所有核心业务在此层实现。 |
| **Repositories** | 数据访问抽象。每个实体对应一个仓储接口和实现。**不包含业务逻辑。** |
| **Middleware** | 横切关注点：JWT 认证、节点密钥认证、全局异常处理、速率限制、审计日志。 |

### 2.4 命名约定

| 元素 | 命名规则 | 示例 |
|------|----------|------|
| 控制器 | `{Entity}Controller` | [`AuthController.cs`](#23-分层职责), [`UsersController.cs`](#23-分层职责) |
| 服务 | `{Entity}Service` | [`AuthService.cs`](#23-分层职责), [`UserService.cs`](#23-分层职责) |
| 仓储接口 | `I{Entity}Repository` | [`IUserRepository.cs`](#23-分层职责) |
| 实体 | 单数 PascalCase | [`User.cs`](#23-分层职责), [`TrafficRecord.cs`](#23-分层职责) |
| DTO | `{Entity}{Action}Dto` 或 `{Action}Request` | [`UserDto.cs`](#23-分层职责), [`AuthRequest.cs`](#23-分层职责) |
| 数据库表 | 复数 PascalCase | `Users`, `TrafficRecords` |
| 数据库列 | PascalCase | `UsedTrafficBytes`, `CreatedAt` |

---

## 3. API 规范

> 完整 API 定义参见 [`document/architect/api-design.md`](../architect/api-design.md)

### 3.1 统一响应格式

#### 3.1.1 成功响应

成功和列表响应**直接返回数据对象**，不包裹在额外结构中：

```json
{
    "id": 1,
    "username": "user123",
    "createdAt": "2025-01-01T00:00:00Z"
}
```

列表响应：

```json
{
    "total": 100,
    "page": 1,
    "pageSize": 20,
    "items": [...]
}
```

#### 3.1.2 错误响应（强制格式）

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
| `error.code` | string | 机器可读的错误代码 |
| `error.message` | string | 人类可读的错误描述 |
| `error.requestId` | string | 请求追踪 ID，与日志中 [`RequestId`] 对应 |

### 3.2 全局错误代码（完整清单）

每个错误代码**必须**使用指定的 HTTP 状态码，严禁混用。

| 错误代码 | HTTP 状态码 | 说明 | 使用场景 |
|----------|-------------|------|----------|
| `invalid_credentials` | 401 | 用户名或密码错误 | 认证失败 |
| `token_expired` | 401 | JWT Token 已过期 | Token 过期 |
| `token_invalid` | 401 | JWT Token 无效 | Token 签名或格式错误 |
| `unauthorized` | 401 | 未提供认证凭据 | 请求缺少 Authorization 头 |
| `forbidden` | 403 | 权限不足 | 角色无权限执行操作 |
| `account_disabled` | 403 | 账号已禁用 | `IsActive = false` |
| `account_expired` | 403 | 账号已过期 | `ExpiresAt < UtcNow` |
| `account_locked` | 403 | 账号已锁定 | 管理员登录失败次数过多 |
| `traffic_exhausted` | 403 | 流量已用尽 | `UsedTrafficBytes >= TotalTrafficBytes` |
| `node_not_allowed` | 403 | 不允许使用该节点 | 节点不在用户白名单中 |
| `node_secret_invalid` | 403 | 节点密钥无效 | `X-Node-Secret` 不正确 |
| `not_found` | 404 | 资源不存在 | 用户/节点/会话不存在 |
| `conflict` | 409 | 资源冲突 | 用户名重复等 |
| `idempotency_conflict` | 409 | 幂等键冲突 | 重复上报 |
| `rate_limited` | 429 | 请求过于频繁 | 超过速率限制 |
| `validation_error` | 422 | 请求参数校验失败 | 请求体格式或字段不合法 |
| `internal_error` | 500 | 服务器内部错误 | 未分类异常兜底 |

### 3.3 全局异常处理中间件

**强制要求**：所有未捕获异常必须由 [`ExceptionMiddleware`] 统一拦截，按上述格式返回错误响应，并生成 [`RequestId`] 贯穿整个请求生命周期。

```csharp
// ExceptionMiddleware 核心职责：
// 1. 捕获所有未处理异常
// 2. 记录完整错误日志（含 RequestId）
// 3. 返回统一错误响应格式
// 4. 区分业务异常（自定义异常类，含错误代码）和系统异常（统一返回 internal_error）
```

### 3.4 认证机制

| 认证类型 | 请求头 | 用途 |
|----------|--------|------|
| 管理员认证 | `Authorization: Bearer {admin_token}` | 用户管理、节点管理、管理功能 API |
| 节点认证 | `X-Node-Secret: {node_secret}` | 认证转发、节点心跳、节点注册 API |

### 3.5 速率限制

| 端点分组 | 限制规则 | 说明 |
|----------|----------|------|
| [`/api/v1/auth/*`] | 100 次/分钟/IP | Edge Agent 汇聚多用户请求 |
| [`/api/v1/admin/login`] | 10 次/分钟/IP | 防暴力破解 |
| [`/api/v1/admin/*`] 其他 | 60 次/分钟/Token | 管理员操作 |
| [`/api/v1/nodes/*/heartbeat`] | **不限制** | 心跳需及时处理 |
| [`/health`] | **不限制** | 监控系统高频调用 |

> 实现：使用 ASP.NET Core 内置 [`RateLimiter`](https://learn.microsoft.com/en-us/aspnet/core/performance/rate-limit) 中间件，固定窗口算法。

### 3.6 CORS 配置

- **仅对管理 API 路由启用 CORS**（`/api/v1/admin/*`、`/api/v1/users/*`）
- **节点通信 API 不启用 CORS**（`/api/v1/auth/*`、`/api/v1/nodes/*`），因为无浏览器场景

```json
{
    "Cors": {
        "AllowedOrigins": ["https://admin.example.com"],
        "AllowedMethods": ["GET", "POST", "PUT", "DELETE"],
        "AllowedHeaders": ["Authorization", "Content-Type"],
        "ExposeHeaders": ["X-Request-Id"],
        "MaxAgeSeconds": 3600
    }
}
```

### 3.7 请求体大小

- 所有 API 请求体最大 **1MB**，防止大 payload 攻击
- 在 ASP.NET Core 中通过 [`IHttpMaxRequestBodySizeFeature`] 或全局中间件配置

---

## 4. 数据库规范

> 完整数据库设计参见 [`document/architect/database-design.md`](../architect/database-design.md)

### 4.1 通用约束

| 规则 | 说明 |
|------|------|
| 引擎 | SQLite，通过 EF Core 访问 |
| **所有时间字段** | 存储 UTC，参见 [§1 时间戳规范](#1-时间戳规范) |
| 主键 | [`Id`] 列，`BIGINT` 自增（除 [`Nodes`] 表使用 `VARCHAR(64)` UUID） |
| 密码 | BCrypt 加密存储，Work Factor = 12 |
| 流量字段 | 所有流量字段以 **字节 (Bytes)** 为单位，`BIGINT` 类型 |
| 软删除 | 不物理删除，使用 `IsActive` 字段标记 |
| 审计日志 | 不可物理删除 |

### 4.2 用户表 (Users)

| 字段 | 类型 | 约束 | 说明 |
|------|------|------|------|
| `Id` | BIGINT | PK, AUTO_INCREMENT | 用户唯一标识 |
| `Username` | VARCHAR(64) | UNIQUE, NOT NULL | 登录用户名 |
| `Password` | VARCHAR(256) | NOT NULL | 密码（BCrypt 加密，Work Factor = 12） |
| `Email` | VARCHAR(128) | NULL | 邮箱地址 |
| `TotalTrafficBytes` | BIGINT | NOT NULL, DEFAULT 0 | 总流量配额（字节） |
| `UsedTrafficBytes` | BIGINT | NOT NULL, DEFAULT 0 | 已使用流量（字节） |
| `IsActive` | BOOLEAN | NOT NULL, DEFAULT 1 | 是否激活 |
| `CreatedAt` | DATETIME | NOT NULL | 创建时间（UTC） |
| `UpdatedAt` | DATETIME | NOT NULL | 更新时间（UTC） |
| `ExpiresAt` | DATETIME | NULL | 过期时间（NULL=永不过期）（UTC） |
| `AllowedNodes` | TEXT | NULL | 允许的节点ID列表（JSON数组，NULL=全部节点） |
| `Remark` | TEXT | NULL | 备注信息 |

**额外约束**：

- 实体须包含 `[Timestamp] byte[] RowVersion` 属性，用于乐观并发控制（[§8.1](#81-usersusedtrafficbytes-并发扣减)）
- 软删除：删除用户时将 `IsActive` 设为 `false`，历史数据保留

### 4.3 流量记录表 (TrafficRecords)

| 字段 | 类型 | 约束 | 说明 |
|------|------|------|------|
| `Id` | BIGINT | PK, AUTO_INCREMENT | 记录ID |
| `UserId` | BIGINT | FK, NOT NULL | 用户ID |
| `BytesIn` | BIGINT | NOT NULL | 入站流量/用户上传（字节）。对应 Hysteria `rx` |
| `BytesOut` | BIGINT | NOT NULL | 出站流量/用户下载（字节）。对应 Hysteria `tx` |
| `NodeId` | VARCHAR(64) | NOT NULL | 节点ID |
| `IdempotencyKey` | VARCHAR(128) | **UNIQUE**, NOT NULL | 幂等键，格式 `{nodeId}_{username}_{timestamp_rounded}` |
| `RecordedAt` | DATETIME | NOT NULL | 记录时间（UTC） |

> **流量方向映射（关键）**：Hysteria 的 `tx` = 服务端发出 = 用户下载 → 记入 `BytesOut`；Hysteria 的 `rx` = 服务端收到 = 用户上传 → 记入 `BytesIn`。**方向映射错误将导致流量统计完全错乱。**

### 4.4 认证日志表 (AuthLogs)

| 字段 | 类型 | 约束 | 说明 |
|------|------|------|------|
| `Id` | BIGINT | PK, AUTO_INCREMENT | 日志ID |
| `UserId` | BIGINT | FK, NULL | 用户ID（认证失败时为 NULL） |
| `Username` | VARCHAR(64) | NOT NULL | 尝试认证的用户名 |
| `NodeId` | VARCHAR(64) | NOT NULL | 认证节点ID |
| `ClientIp` | VARCHAR(45) | NOT NULL | 客户端IP |
| `Success` | BOOLEAN | NOT NULL | 是否成功 |
| `Reason` | VARCHAR(256) | NULL | 失败原因代码（使用 [§3.2 错误代码](#32-全局错误代码完整清单) 值） |
| `AuthTime` | DATETIME | NOT NULL | 认证时间（UTC） |

### 4.5 会话表 (Sessions)

| 字段 | 类型 | 约束 | 说明 |
|------|------|------|------|
| `Id` | BIGINT | PK, AUTO_INCREMENT | 会话ID |
| `UserId` | BIGINT | FK, NOT NULL | 用户ID |
| `NodeId` | VARCHAR(64) | NOT NULL | 节点ID |
| `SessionKey` | VARCHAR(128) | UNIQUE, NOT NULL | 会话密钥 |
| `StartedAt` | DATETIME | NOT NULL | 开始时间（UTC） |
| `EndedAt` | DATETIME | NULL | 结束时间（UTC） |
| `BytesIn` | BIGINT | NOT NULL, DEFAULT 0 | 入站流量 |
| `BytesOut` | BIGINT | NOT NULL, DEFAULT 0 | 出站流量 |
| `Status` | VARCHAR(16) | NOT NULL, DEFAULT 'active' | 状态：`active`、`idle`、`closed` |

> 会话状态机：`active` → `idle`（30s 无流量）→ `active`（恢复）或 `closed`（连续 3 次不在线）。`closed` 会话保留 7 天后清理。

### 4.6 节点表 (Nodes)

| 字段 | 类型 | 约束 | 说明 |
|------|------|------|------|
| `Id` | VARCHAR(64) | PK | 节点唯一标识（UUID） |
| `Name` | VARCHAR(128) | NOT NULL | 节点名称 |
| `IpAddress` | VARCHAR(45) | NOT NULL | 节点IP地址 |
| `Port` | INT | NOT NULL | Hysteria 服务端口 |
| `SecretKey` | VARCHAR(256) | NOT NULL | 节点密钥（AES-256-GCM 加密存储） |
| `SecretVersion` | INT | NOT NULL, DEFAULT 1 | 密钥版本号，用于密钥轮换 |
| `IsActive` | BOOLEAN | NOT NULL, DEFAULT 1 | 是否激活；心跳超时 90s 自动置为 `false` |
| `CreatedAt` | DATETIME | NOT NULL | 创建时间（UTC） |
| `LastHeartbeat` | DATETIME | NULL | 最后心跳时间（UTC） |
| `Location` | VARCHAR(128) | NULL | 节点位置描述 |
| `TrafficStatsPort` | INT | NULL | Hysteria trafficStats API 端口 |
| `TrafficStatsSecret` | VARCHAR(256) | NULL | trafficStats API 密钥（AES-256-GCM 加密存储） |
| `ProvisionToken` | VARCHAR(128) | UNIQUE, NULL | 预注册令牌；注册成功后清零 |
| `ProvisionStatus` | VARCHAR(16) | NOT NULL, DEFAULT 'pending' | `pending` / `provisioned` / `revoked` |

> 密钥多版本共存：轮换期间同时接受旧版本和新版本的 `SecretKey`，确保零停机切换。

### 4.7 节点状态表 (NodeStatus)

| 字段 | 类型 | 约束 | 说明 |
|------|------|------|------|
| `Id` | BIGINT | PK, AUTO_INCREMENT | 状态记录ID |
| `NodeId` | VARCHAR(64) | FK, NOT NULL | 节点ID |
| `CpuUsagePercent` | FLOAT | NOT NULL | CPU 使用率（%） |
| `MemoryUsagePercent` | FLOAT | NOT NULL | 内存使用率（%） |
| `MemoryUsedMb` | FLOAT | NOT NULL | 已用内存（MB） |
| `MemoryTotalMb` | FLOAT | NOT NULL | 总内存（MB） |
| `NetworkInBytes` | BIGINT | NOT NULL | 累计网络流入（字节） |
| `NetworkOutBytes` | BIGINT | NOT NULL | 累计网络流出（字节） |
| `NetworkInMbps` | FLOAT | NOT NULL | 当前网络流入速率（Mbps） |
| `NetworkOutMbps` | FLOAT | NOT NULL | 当前网络流出速率（Mbps） |
| `ActiveConnections` | INT | NOT NULL | 当前活跃连接数 |
| `ReportedAt` | DATETIME | NOT NULL | 上报时间（UTC） |

### 4.8 节点流量统计表 (NodeTraffic)

| 字段 | 类型 | 约束 | 说明 |
|------|------|------|------|
| `Id` | BIGINT | PK, AUTO_INCREMENT | 统计ID |
| `NodeId` | VARCHAR(64) | FK, NOT NULL | 节点ID |
| `TotalBytesIn` | BIGINT | NOT NULL | 总入站流量 |
| `TotalBytesOut` | BIGINT | NOT NULL | 总出站流量 |
| `ActiveUsers` | INT | NOT NULL | 活跃用户数 |
| `RecordedAt` | DATETIME | NOT NULL | 统计时间（UTC） |

### 4.9 管理员表 (Admins)

| 字段 | 类型 | 约束 | 说明 |
|------|------|------|------|
| `Id` | BIGINT | PK, AUTO_INCREMENT | 管理员唯一标识 |
| `Username` | VARCHAR(64) | UNIQUE, NOT NULL | 管理员用户名 |
| `Password` | VARCHAR(256) | NOT NULL | 密码（BCrypt 加密，Work Factor = 12） |
| `Role` | VARCHAR(32) | NOT NULL, DEFAULT 'admin' | 角色：`super_admin`、`admin`、`readonly` |
| `IsActive` | BOOLEAN | NOT NULL, DEFAULT 1 | 是否激活 |
| `CreatedAt` | DATETIME | NOT NULL | 创建时间（UTC） |
| `LastLoginAt` | DATETIME | NULL | 最后登录时间（UTC） |
| `FailedLoginAttempts` | INT | NOT NULL, DEFAULT 0 | 连续登录失败次数 |
| `LockedUntil` | DATETIME | NULL | 账号锁定到期时间（NULL=未锁定）（UTC） |

**角色权限矩阵**：

| 操作 | `super_admin` | `admin` | `readonly` |
|------|---------------|---------|------------|
| 管理管理员 (CRUD) | ✅ | ❌ | ❌ |
| 管理用户 (CRUD) | ✅ | ✅ | ❌ |
| 管理节点 | ✅ | ✅ | ❌ |
| 查看所有数据 | ✅ | ✅ | ✅ |
| 查看审计日志 | ✅ | ✅ | ❌ |
| 踢用户下线 | ✅ | ✅ | ❌ |

### 4.10 管理员操作审计日志表 (AdminAuditLogs)

| 字段 | 类型 | 约束 | 说明 |
|------|------|------|------|
| `Id` | BIGINT | PK, AUTO_INCREMENT | 日志ID |
| `AdminId` | BIGINT | FK, NOT NULL | 操作管理员ID |
| `Action` | VARCHAR(64) | NOT NULL | 操作类型：`create`、`update`、`delete`、`login`、`logout`、`kick_user` |
| `TargetType` | VARCHAR(32) | NOT NULL | 目标类型：`user`、`node`、`admin`、`system` |
| `TargetId` | VARCHAR(64) | NULL | 操作目标ID |
| `Detail` | TEXT | NULL | 操作详情（JSON格式，含变更前后对比） |
| `ClientIp` | VARCHAR(45) | NOT NULL | 操作来源IP |
| `CreatedAt` | DATETIME | NOT NULL | 操作时间（UTC） |

> **强制要求**：该表记录不可物理删除，不可修改。所有管理员写操作必须记录到此表。

### 4.11 数据保留策略

| 数据类型 | 保留期限 | 清理方式 |
|----------|----------|----------|
| `TrafficRecords` | 90 天 | 后台定时任务每日清理 |
| `AuthLogs` | 90 天 | 后台定时任务每日清理 |
| `NodeStatus` | 30 天 | 后台定时任务每日清理 |
| `NodeTraffic` | 90 天 | 后台定时任务每日清理 |
| `AdminAuditLogs` | 365 天 | 后台定时任务每日清理 |
| `Sessions` (closed) | 7 天 | 后台定时任务每日清理 |
| 数据库备份 | 30 天 | 备份脚本自动清理 |

---

## 5. 认证流程规范

### 5.1 双层认证架构

```
层级 1: Hysteria Server → Edge Agent (POST /auth)
       Hysteria 原生 HTTP Auth 协议
       
层级 2: Edge Agent → 主服务器 (POST /api/v1/auth/hysteria)
       项目内部协议
```

### 5.2 Edge Agent 协议转换（核心逻辑）

```csharp
// 协议转换规则 — 必须严格实现：
// Hysteria 原生                 →    项目内部
//   auth (单一 Base64 凭据)     →    username + password（解析分离）
//   addr ("IP:Port")            →    clientIp（仅 IP 部分）
//   tx (速率)                   →    包含在请求体中
//   无节点标识                   →    nodeId（Agent 配置注入）
// 响应转换（反向）：
//   项目内部 success + userId   →    Hysteria 原生 ok + id
```

### 5.3 认证检查顺序

认证时的检查**必须按以下顺序**执行，不允许跳过或重排：

```
1. 用户存在？              → 否 → invalid_credentials
2. 密码正确？              → 否 → invalid_credentials
3. 账号激活 (IsActive)？   → 否 → account_disabled
4. 账号过期 (ExpiresAt)？  → 是 → account_expired
5. 流量充足？              → 否 → traffic_exhausted
6. 节点允许？              → 否 → node_not_allowed
   ↓
   认证成功 → 记录 AuthLog
```

### 5.4 管理员登录保护

| 策略 | 值 |
|------|-----|
| 最大失败次数 | 5 次 |
| 锁定时长 | 15 分钟（可配置） |
| 成功登录后 | 重置 `FailedLoginAttempts` 为 0 |
| 锁定状态 | `LockedUntil > UtcNow` 时返回 `account_locked` |

### 5.5 JWT Token

| 配置 | 说明 |
|------|------|
| 算法 | HMAC-SHA256 |
| 过期时间 | 默认 1440 分钟（24 小时），可配置 |
| 刷新窗口 | 过期前 5 分钟可刷新，无需重新登录 |
| 密钥 | ≥ 256-bit (32 字符)，存储在 `appsettings.json` 或环境变量 |

---

## 6. 安全规范

### 6.1 密码安全

| 策略 | 值 |
|------|-----|
| 算法 | BCrypt |
| Work Factor | **12**（不允许低于此值） |
| 适用范围 | 用户密码 + 管理员密码 |

### 6.2 通信安全

| 要求 | 说明 |
|------|------|
| HTTPS | **生产环境强制执行** |
| 节点通信 | 使用 `X-Node-Secret` 请求头传递密钥 |
| trafficStats | 绑定 `127.0.0.1` + 设置 secret，禁止公网暴露 |
| 认证代理 | Edge Agent 认证代理绑定 `127.0.0.1` |

### 6.3 数据安全

| 要求 | 说明 |
|------|------|
| SQL 注入 | EF Core 参数化查询（不可拼接 SQL） |
| 敏感数据 | 密钥使用 AES-256-GCM 加密存储 |
| 日志脱敏 | **严禁**在日志中输出密码、密钥等敏感信息 |
| 审计追踪 | 管理员操作写入 `AdminAuditLogs`，不可删除 |

### 6.4 密钥管理

| 密钥类型 | 生成方式 | 长度 | 存储 |
|----------|----------|------|------|
| JWT Secret | 管理员配置 | ≥ 256-bit (32 字符) | `appsettings.json` 或环境变量 |
| 节点 SecretKey | Edge Agent 首次启动时随机生成 | 256-bit (64 hex) | `Nodes` 表（AES加密） + `agent.json` |
| trafficStats Secret | 管理员配置 | ≥ 128-bit (16 字符) | `Nodes` 表（AES加密） + `hysteria.yaml` |

**密钥轮换**：多版本并行，旧密钥在确认新密钥生效后淘汰。零停机。

---

## 7. 流量统计规范

> 完整流量统计设计参见 [`document/architect/traffic-statistics.md`](../architect/traffic-statistics.md)

### 7.1 采集方案

采用**方案一（Edge Agent 内部采集）**，禁止使用主服务器直连方式：

```
Hysteria Server (:9999) ← Edge Agent ← 主服务器
       (本地回环)        (合并心跳上报)
```

### 7.2 采集流程

```
定时器触发 (每 30s)
  → GET /traffic?clear=1  (采集流量，Hysteria 自动清零)
  → GET /online            (采集在线用户)
  → 合并系统监控数据
  → POST /api/v1/nodes/{nodeId}/heartbeat
```

### 7.3 流量方向映射（强制正确实现）

| Hysteria API | 视角 | 含义 | 数据库字段 |
|-------------|------|------|-----------|
| `tx` | 服务端发出 | **用户下载** | `BytesOut` |
| `rx` | 服务端收到 | **用户上传** | `BytesIn` |
| 合计 | — | 用户总流量 | `Users.UsedTrafficBytes` (+= tx + rx) |

> ⚠️ **方向映射错误将直接导致流量统计完全颠倒，这是不可接受的生产事故。**

### 7.4 流量检查策略

- **认证时**：仅校验 `UsedTrafficBytes < TotalTrafficBytes`，**不预扣减**
- **流量扣减**：通过定时采集上报异步完成
- **超额处理**：主服务器检测到超额后，通过 Edge Agent 调用 Hysteria `POST /kick` 踢用户下线

---

## 8. 并发控制与数据一致性规范

> 完整规范参见 [`document/architect/traffic-statistics.md` §6](../architect/traffic-statistics.md#6-并发控制与数据一致性)

### 8.1 `Users.UsedTrafficBytes` 并发扣减

**策略**：乐观并发控制（Optimistic Concurrency）+ 最多 3 次重试

```csharp
// User 实体必须包含：
[Timestamp]
public byte[] RowVersion { get; set; }

// 流量扣减方法模板（必须遵从此实现模式）：
public async Task UpdateUsedTrafficAsync(long userId, long bytesToAdd, int retryCount = 3)
{
    for (int i = 0; i < retryCount; i++)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            user.UsedTrafficBytes += bytesToAdd;
            await _context.SaveChangesAsync();
            return;
        }
        catch (DbUpdateConcurrencyException)
        {
            if (i == retryCount - 1) throw;
            await Task.Delay(Random.Shared.Next(10, 50));
            _context.ChangeTracker.Clear(); // 重新加载实体
        }
    }
}
```

### 8.2 流量数据幂等性

**幂等键格式**：`{nodeId}_{username}_{timestamp_rounded_to_interval}`

- 示例：`edge-node-01_user123_2025-01-01T12:00:00Z`
- `TrafficRecords.IdempotencyKey` 设置 **数据库 UNIQUE 约束**
- 主服务器在处理心跳时必须先检查幂等键是否存在，已存在则跳过

### 8.3 心跳数据原子性

**整个心跳处理包裹在一个数据库事务中**，任一子操作失败则整体回滚：

```
BEGIN TRANSACTION
    INSERT NodeStatus         (系统状态)
    FOR EACH user IN userTraffic:
        IF NOT idempotency_duplicate:
            INSERT TrafficRecords
            UPDATE Users.UsedTrafficBytes  (带乐观并发重试)
    INSERT NodeTraffic         (节点流量汇总)
    UPDATE Nodes.LastHeartbeat
    UPDATE Sessions             (在线状态)
COMMIT
```

---

## 9. 异常处理与降级规范

### 9.1 网络异常处理

| 场景 | 处理策略 |
|------|----------|
| 主服务器不可达 | Edge Agent 使用本地缓存进行认证 |
| 缓存过期且主服务器不可达 | 拒绝新连接，允许已连接用户继续使用 |
| 数据库异常 | 返回 500 错误，记录 Error 级别日志 |
| 节点心跳超时（90 秒） | 主服务器将 `Nodes.IsActive` 设为 `false` |
| Hysteria trafficStats API 不可达 | Edge Agent 跳过本次采集，记录 Warning 日志，下次重试 |
| 流量数据上报失败 | 缓存到本地，下次上报时合并补报 |
| 节点密钥无效 | Edge Agent 记录 Error 日志，持续重试注册，不清除已有密钥 |
| 数据库锁冲突 | 乐观并发重试（最多 3 次），超限后记录 Error 并返回 500 |

### 9.2 降级策略

```csharp
// 认证降级模板（Edge Agent 中实现）：
public async Task<AuthResult> AuthenticateAsync(AuthRequest request)
{
    try
    {
        return await _masterClient.AuthenticateAsync(request);
    }
    catch (Exception ex) when (_cache.IsEnabled)
    {
        _logger.LogWarning(ex, "主服务器不可达，使用缓存认证");
        return _cache.Authenticate(request.Username, request.Password);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "认证失败");
        return AuthResult.Fail("internal_error");
    }
}
```

### 9.3 边缘节点离线处理

```
心跳超时 (90s)  →  IsActive = false  →  拒绝对该节点的新认证请求
节点恢复心跳    →  IsActive = true   →  恢复认证请求正常处理
```

### 9.4 注册重试策略（Edge Agent）

| 参数 | 值 |
|------|-----|
| 初始间隔 | 2s |
| 最大间隔 | 60s（指数退避） |
| 最大重试次数 | 5 |
| 全部失败后 | `exit(1)`，由 systemd `Restart=always` 重启 |

---

## 10. 日志与监控规范

### 10.1 日志级别使用

| 级别 | 使用场景 |
|------|----------|
| `Trace` | 请求/响应详情、SQL 参数 |
| `Debug` | 缓存命中/未命中、幂等检查结果 |
| `Information` | 服务启动/停止、流量采集完成、节点注册成功 |
| `Warning` | 主服务器响应慢、流量采集跳过、心跳超时 |
| `Error` | 认证失败、数据库错误、API 不可达 |
| `Critical` | 服务崩溃、数据库损坏 |

> **禁止**：日志中记录密码、密钥等敏感信息。

### 10.2 SLO 指标（必须达标）

| 指标 | 目标 (SLO) |
|------|-----------|
| 认证请求 P50 延迟 | < 50ms |
| 认证请求 P99 延迟 | < 500ms |
| 心跳处理 P99 延迟 | < 100ms |
| 主服务器可用性 | ≥ 99.9% |
| Edge Agent 可用性 | ≥ 99.5% |
| 流量数据上报延迟 | < 60s（2 个采集周期内） |
| 数据库事务成功率 | ≥ 99.99% |
| API 错误率（5xx） | < 0.1% |

---

## 11. 测试规范

### 11.1 测试框架

| 层级 | 框架/工具 |
|------|-----------|
| 单元测试 | xUnit + Moq + FluentAssertions |
| 集成测试 | xUnit + Testcontainers + EF Core InMemory |
| E2E 测试 | xUnit + 本地 Hysteria 实例 |

### 11.2 最低覆盖率要求

| 模块 | 目标覆盖率 |
|------|-----------|
| `AuthService` | ≥ 90% |
| `UserService` | ≥ 85% |
| `NodeService` | ≥ 85% |
| `TrafficService` | ≥ 90% |
| `AdminService` | ≥ 85% |
| Edge Agent `AuthProxy` | ≥ 80% |
| Edge Agent `TrafficCollector` | ≥ 80% |

### 11.3 必测场景

- 认证成功/各种失败原因（密码错/禁用/过期/流量耗尽/节点白名单）
- Hysteria 原生协议 → 内部协议转换
- 并发流量扣减（两个线程同时增量，最终值正确）
- 幂等检查（重复上报仅计入一次）
- 心跳事务（部分失败时整体回滚）
- 端到端认证链路（Hysteria Client → Edge Agent → 主服务器）

---

## 12. 配置规范

### 12.1 主服务器配置 (appsettings.json)

```json
{
    "ConnectionStrings": {
        "DefaultConnection": "Data Source=/var/lib/hysteria-auth/hysteria-auth.db"
    },
    "Jwt": {
        "Secret": "your-super-secret-key-at-least-32-chars",
        "Issuer": "hysteria-auth-master",
        "Audience": "hysteria-auth-admin",
        "ExpirationMinutes": 1440,
        "RefreshWindowMinutes": 5
    },
    "Auth": {
        "CacheEnabled": true,
        "CacheExpirationMinutes": 5,
        "MaxAuthAttemptsPerMinute": 10,
        "LockoutDurationMinutes": 15
    },
    "Node": {
        "HeartbeatTimeoutSeconds": 90,
        "StatusReportIntervalSeconds": 30,
        "KeyRotationEnabled": true
    },
    "Traffic": {
        "CollectIntervalSeconds": 30,
        "TrafficDataRetentionDays": 90,
        "ConcurrentUpdateRetryCount": 3,
        "IdempotencyEnabled": true
    },
    "Admin": {
        "MaxFailedLoginAttempts": 5,
        "LockoutDurationMinutes": 15,
        "AuditLogRetentionDays": 365
    },
    "RateLimit": {
        "AuthPerMinute": 100,
        "AdminLoginPerMinute": 10,
        "AdminApiPerMinute": 60
    },
    "Cors": {
        "AllowedOrigins": ["https://admin.example.com"],
        "AllowedMethods": ["GET", "POST", "PUT", "DELETE"],
        "AllowedHeaders": ["Authorization", "Content-Type"],
        "MaxAgeSeconds": 3600
    },
    "Backup": {
        "AutoBackupEnabled": true,
        "BackupIntervalHours": 24,
        "BackupDirectory": "/var/backups/hysteria-auth",
        "RetentionDays": 30
    },
    "Logging": {
        "LogLevel": {
            "Default": "Information",
            "Microsoft": "Warning",
            "HysteriaAuth": "Debug"
        },
        "File": "/var/log/hysteria-auth/master.log"
    }
}
```

### 12.2 Edge Agent 配置 (agent.json)

```json
{
    "NodeId": "edge-node-01",
    "NodeName": "Tokyo Edge Node",
    "MasterServerUrl": "https://master.example.com",
    "NodeSecret": "your-node-secret-key",
    "AgentVersion": "1.0.0",
    "Init": {
        "RegistrationRetryMax": 5,
        "RegistrationRetryInitialSeconds": 2,
        "RegistrationRetryMaxSeconds": 60
    },
    "AuthProxy": {
        "ListenAddress": "127.0.0.1",
        "ListenPort": 8080
    },
    "TrafficStats": {
        "ListenAddress": "127.0.0.1",
        "ListenPort": 9999,
        "Secret": "your_traffic_stats_secret",
        "CollectIntervalSeconds": 30
    },
    "Monitor": {
        "IntervalSeconds": 10,
        "NetworkInterfaces": ["eth0"]
    },
    "Reporter": {
        "IntervalSeconds": 30,
        "RetryCount": 3,
        "RetryDelaySeconds": 5
    },
    "Cache": {
        "Enabled": true,
        "MaxSize": 1000,
        "ExpirationMinutes": 5
    },
    "HealthCheck": {
        "Enabled": true,
        "ListenAddress": "127.0.0.1",
        "ListenPort": 8081
    },
    "Logging": {
        "LogLevel": "Information",
        "File": "/var/log/hysteria-auth/agent.log"
    }
}
```

---

## 13. 部署规范

### 13.1 系统要求

| 组件 | 要求 |
|------|------|
| 操作系统 | Ubuntu 20.04+ / Debian 11+ |
| 运行时 | .NET 8.0 Runtime |
| 主服务器 RAM | ≥ 1GB |
| Edge Agent RAM | ≥ 256MB |
| 主服务器磁盘 | ≥ 10GB |

### 13.2 systemd 服务

**主服务器**：用户 `www-data`，`Type=notify`，`Restart=always`

**Edge Agent**：用户 `root`，`Type=notify`，`Restart=always`，`After=hysteria-server.service`

### 13.3 数据库备份

| 参数 | 值 |
|------|-----|
| 间隔 | 24 小时 |
| 方式 | SQLite `.backup` 命令 |
| 目录 | `/var/backups/hysteria-auth/` |
| 保留 | 30 天 |
| 命名 | `hysteria-auth-{yyyy-MM-dd-HHmmss}.db` |

### 13.4 Hysteria 服务端关键配置

```yaml
auth:
  type: http
  http:
    url: http://127.0.0.1:8080/auth     # ← 必须指向 Edge Agent 认证代理

trafficStats:
  listen: 127.0.0.1:9999                # ← 仅本地回环，禁止公网暴露
  secret: your_traffic_stats_secret     # ← 与 agent.json 中一致
```

---

> **本规范是后端开发的唯一权威依据。任何与本文档不一致的实现将被视为缺陷。**
