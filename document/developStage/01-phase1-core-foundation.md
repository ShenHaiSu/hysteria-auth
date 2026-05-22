# Phase 1: 核心基础

> **阶段**: Phase 1 | **预估工期**: 2-3 周 | **依赖**: 无（项目起始阶段）
>
> **来源文档**: [`document/develop/backend-development-spec.md`](../develop/backend-development-spec.md) · [`document/architect/overview.md`](../architect/overview.md) · [`../architect/system-architecture.md`](../architect/system-architecture.md) · [`../architect/database-design.md`](../architect/database-design.md) · [`../architect/api-design.md`](../architect/api-design.md) · [`../architect/edge-node-design.md`](../architect/edge-node-design.md)

---

## 1. 阶段目标与范围

### 1.1 总体目标

搭建项目骨架，完成**主服务器（Master Server）**的全部数据库模型、用户/管理员 CRUD、认证 API（双层）、Edge Agent 认证代理（含协议转换）、以及全局异常处理中间件。Phase 1 完成后，应能跑通如下最简链路：

```
Hysteria Client → Hysteria Server → Edge Agent (AuthProxy) → Master Server (AuthController)
                     (QUIC)            (POST /auth)              (POST /api/v1/auth/hysteria)
```

### 1.2 范围清单

| 序号 | 交付项 | 说明 |
|------|--------|------|
| 1.1 | 解决方案与项目结构初始化 | `HysteriaAuth.sln`、`HysteriaAuth.Master`、`HysteriaAuth.Agent` |
| 1.2 | 全部 9 张表的 EF Core 实体 + DbContext | `User`, `Node`, `TrafficRecord`, `AuthLog`, `Session`, `NodeStatus`, `NodeTraffic`, `Admin`, `AdminAuditLog` |
| 1.3 | 数据库迁移 (Initial Migration) | EF Core Migration + SQLite 数据库 |
| 1.4 | 用户 CRUD API | `POST/GET/PUT/DELETE /api/v1/users/*` |
| 1.5 | 管理员登录 + CRUD API | `POST /api/v1/admin/login` + 管理员 CRUD |
| 1.6 | Hysteria 认证 API（主服务器侧） | `POST /api/v1/auth/hysteria` + 6 步认证检查 |
| 1.7 | Edge Agent 认证代理（含协议转换） | `POST /auth`（Hysteria 原生 → 内部协议转换）|
| 1.8 | JWT 认证中间件 | 管理员 Token 签发 + 验证 |
| 1.9 | 节点密钥认证中间件 | `X-Node-Secret` 验证 |
| 1.10 | 全局异常处理中间件 | 统一错误格式 + `requestId` 追踪 |
| 1.11 | 主服务器配置 (`appsettings.json`) | 所有配置节就位 |
| 1.12 | Edge Agent 最小配置 (`agent.json`) | 认证代理 + 基础配置 |

---

## 2. 阶段启动前置检查

> **本阶段是 Phase 1，无前置阶段。** 以下为项目启动时的环境就绪检查清单。

| 检查项 | 验证方式 | 通过标准 |
|--------|----------|----------|
| .NET 8.0 SDK 已安装 | `dotnet --version` | 输出版本号 ≥ 8.0.x |
| EF Core CLI 已安装 | `dotnet ef --version` | 输出版本号 |
| SQLite 可用 | `dotnet ef database update` 不报错 | 数据库文件自动创建 |
| IDE 就绪 | 打开 `HysteriaAuth.sln` | 项目加载无错误 |
| `document/develop/backend-development-spec.md` 已阅读 | 人工确认 | 全部开发人员已知悉全局规范 |

---

## 3. 具体任务清单

### 第 1 周：项目骨架 + 数据库 + 用户/管理员 CRUD

#### 3.1 项目初始化

- [ ] **1.1.1** 创建解决方案 `HysteriaAuth.sln`
- [ ] **1.1.2** 创建项目 `src/HysteriaAuth.Master/`（ASP.NET Core Web API）
- [ ] **1.1.3** 创建项目 `src/HysteriaAuth.Agent/`（ASP.NET Core Web API，仅 `AuthProxy` 监听）
- [ ] **1.1.4** 安装 NuGet 包：
  - Master: `Microsoft.EntityFrameworkCore.Sqlite`、`Microsoft.EntityFrameworkCore.Design`、`Microsoft.AspNetCore.Authentication.JwtBearer`、`BCrypt.Net-Next`
  - Agent: 基础 ASP.NET Core（无 EF Core）
- [ ] **1.1.5** 创建目录结构（按 [`project-structure-configuration.md`](../architect/project-structure-configuration.md)）

#### 3.2 数据库模型

- [ ] **1.2.1** 创建全部 9 个 Entity（[`database-design.md`](../architect/database-design.md) §2.1~§2.9）：

| Entity | 文件 | 关键字段 |
|--------|------|----------|
| `User` | `Models/Entities/User.cs` | `Id`, `Username`, `Password`, `TotalTrafficBytes`, `UsedTrafficBytes`, `IsActive`, `ExpiresAt`, `AllowedNodes`, `RowVersion` |
| `Node` | `Models/Entities/Node.cs` | `Id`, `Name`, `SecretKey`, `SecretVersion`, `IsActive`, `ProvisionToken`, `ProvisionStatus` |
| `TrafficRecord` | `Models/Entities/TrafficRecord.cs` | `UserId`, `BytesIn`, `BytesOut`, `NodeId`, `IdempotencyKey` |
| `AuthLog` | `Models/Entities/AuthLog.cs` | `UserId`, `Username`, `NodeId`, `ClientIp`, `Success`, `Reason` |
| `Session` | `Models/Entities/Session.cs` | `UserId`, `NodeId`, `SessionKey`, `StartedAt`, `Status` |
| `NodeStatus` | `Models/Entities/NodeStatus.cs` | `NodeId`, `CpuUsagePercent`, `MemoryUsagePercent`, `NetworkInMbps` |
| `NodeTraffic` | `Models/Entities/NodeTraffic.cs` | `NodeId`, `TotalBytesIn`, `TotalBytesOut` |
| `Admin` | `Models/Entities/Admin.cs` | `Id`, `Username`, `Password`, `Role`, `FailedLoginAttempts`, `LockedUntil` |
| `AdminAuditLog` | `Models/Entities/AdminAuditLog.cs` | `AdminId`, `Action`, `TargetType`, `TargetId`, `Detail`, `ClientIp` |

- [ ] **1.2.2** 创建 `Data/AppDbContext.cs`，配置全部 9 张表的关系和约束
- [ ] **1.2.3** 在 `OnModelCreating` 中配置：
  - `User.Username` UNIQUE 索引
  - `TrafficRecord.IdempotencyKey` UNIQUE 索引
  - `Session.SessionKey` UNIQUE 索引
  - `Admin.Username` UNIQUE 索引
  - `AdminAuditLog` 只读约束（不提供更新/删除的 DbSet 操作）
- [ ] **1.2.4** 创建 EF Core Initial Migration：`dotnet ef migrations add InitialCreate`
- [ ] **1.2.5** 应用迁移：`dotnet ef database update`，验证数据库文件生成

#### 3.3 用户管理

- [ ] **1.3.1** 创建 `Repositories/IUserRepository.cs` 和实现
  - CRUD 方法：`GetByIdAsync`, `GetByUsernameAsync`, `GetAllAsync` (分页), `AddAsync`, `UpdateAsync`, `SoftDeleteAsync`
  - `CheckUsernameExistsAsync`
- [ ] **1.3.2** 创建 `Services/UserService.cs`
  - 创建用户（BCrypt 加密密码，Work Factor = 12）
  - 更新用户（支持部分更新，仅传递要修改的字段）
  - 软删除用户（设置 `IsActive = false`）
  - 查询用户列表（分页、搜索、筛选）
  - 重置用户流量（`UsedTrafficBytes = 0`）
- [ ] **1.3.3** 创建 `Controllers/UsersController.cs`
  - `POST /api/v1/users` — 创建用户（返回 201）
  - `GET /api/v1/users?page=&pageSize=&search=&isActive=&nodeId=` — 列表
  - `GET /api/v1/users/{userId}` — 详情
  - `PUT /api/v1/users/{userId}` — 更新
  - `DELETE /api/v1/users/{userId}` — 软删除
  - `POST /api/v1/users/{userId}/reset-traffic` — 重置流量
- [ ] **1.3.4** 创建 `Models/DTOs/UserDto.cs`、`Models/DTOs/CreateUserRequest.cs`、`Models/DTOs/UpdateUserRequest.cs`
- [ ] **1.3.5** 验证：通过 curl/Postman 测试完整 CRUD 链路

#### 3.4 管理员管理

- [ ] **1.4.1** 创建 `Repositories/IAdminRepository.cs` 和实现
  - `GetByUsernameAsync`, `GetByIdAsync`, `GetAllAsync`, `AddAsync`, `UpdateAsync`
  - `UpdateLoginAttemptsAsync` (原子更新 `FailedLoginAttempts` + `LockedUntil`)
- [ ] **1.4.2** 创建 `Services/AdminService.cs`
  - 管理员登录：验证密码 → 检查锁定 → 更新 `LastLoginAt` / 重置 `FailedLoginAttempts` → 生成 JWT Token
  - 登录失败处理：`FailedLoginAttempts++`，达到 5 次后设置 `LockedUntil = UtcNow + 15min`
  - 创建管理员（仅 `super_admin`）
  - 角色权限校验
- [ ] **1.4.3** 创建 `Services/JwtService.cs`
  - 签发 Token：HMAC-SHA256，过期时间 1440 分钟（可配置）
  - 验证 Token
  - 刷新 Token（过期前 5 分钟窗口）
- [ ] **1.4.4** 创建 `Controllers/AdminController.cs`
  - `POST /api/v1/admin/login` — 管理员登录
  - `POST /api/v1/admin/admins` — 创建管理员（仅 `super_admin`）
  - `GET /api/v1/admin/admins` — 管理员列表（仅 `super_admin`）
  - `PUT /api/v1/admin/admins/{adminId}` — 更新管理员（仅 `super_admin`）
- [ ] **1.4.5** 创建 `Models/DTOs/AdminDto.cs`、`Models/DTOs/LoginRequest.cs`、`Models/DTOs/LoginResponse.cs`
- [ ] **1.4.6** **种子数据**：在 `Program.cs` 或 Migration 中创建默认 `super_admin` 账号
- [ ] **1.4.7** 验证：测试登录、锁定机制（连续 5 次错误密码后锁 15 分钟）

### 第 2 周：认证 API + Edge Agent 认证代理 + 中间件

#### 3.5 认证 API（主服务器侧）

- [ ] **1.5.1** 创建 `Services/AuthService.cs`，实现 **6 步认证检查**（顺序不可变）：

```csharp
// 认证检查顺序（严格按此顺序，禁止重排）
1. 用户存在?              → 否 → 返回 invalid_credentials (401)
2. 密码正确?              → 否 → 返回 invalid_credentials (401)
3. 账号激活 (IsActive)?   → 否 → 返回 account_disabled (403)
4. 账号过期 (ExpiresAt)?  → 是 → 返回 account_expired (403)
5. 流量充足?              → 否 → 返回 traffic_exhausted (403)
   (UsedTrafficBytes < TotalTrafficBytes)
6. 节点允许?              → 否 → 返回 node_not_allowed (403)
   (AllowedNodes 为 NULL 或包含当前 nodeId)
   ↓
   认证成功 → 记录 AuthLog
```

- [ ] **1.5.2** 创建 `Controllers/AuthController.cs`
  - `POST /api/v1/auth/hysteria` — 认证端点
  - 请求体验证：`username` + `password` + `nodeId` + `clientIp` 必填
- [ ] **1.5.3** 创建 `Repositories/IAuthLogRepository.cs`，认证成功后写入 `AuthLog`
- [ ] **1.5.4** 创建 `Models/DTOs/AuthRequest.cs`、`Models/DTOs/AuthResponse.cs`

#### 3.6 Edge Agent 认证代理（含协议转换）

- [ ] **1.6.1** 创建 `src/HysteriaAuth.Agent/Services/AuthProxy.cs`
  - 监听 `127.0.0.1:8080`（配置自 `agent.json`）
  - 接收 Hysteria 原生 `POST /auth` 请求
- [ ] **1.6.2** 实现 **协议转换**（核心逻辑）：

| 转换方向 | Hysteria 原生 | → | 项目内部 |
|----------|--------------|---|----------|
| `auth` 解析 | `"user123:password123"` (Base64) | → | `username = "user123"` + `password = "password123"` |
| `addr` 提取 | `"192.168.1.100:44556"` | → | `clientIp = "192.168.1.100"`（仅 IP 部分） |
| 节点标识 | (无) | → | `nodeId = agent.json 中的 NodeId` |
| 成功响应 | `{"ok": true, "id": "username"}` | ← | `{"success": true, "userId": 123}` |
| 失败响应 | HTTP 非 200 状态码 | ← | `{"error": {"code": "...", "message": "..."}}` |

- [ ] **1.6.3** 实现 `ParseAuthField(string auth)` 方法
  - Hysteria `auth` 字段是 Base64 编码的用户密码串，解码后按 `:` 分割
  - 若不含 `:` 分隔符，则将整个 `auth` 作为 username，password 为空
- [ ] **1.6.4** 实现 `ExtractIp(string addr)` 方法
  - 从 `"IP:Port"` 格式中提取 IP 部分
- [ ] **1.6.5** 实现向主服务器转发认证请求
  - 使用 `HttpClient` 调用 `POST {MasterServerUrl}/api/v1/auth/hysteria`
  - 附加请求头 `X-Node-Secret: {node_secret}`
- [ ] **1.6.6** 实现 Hysteria 原生格式响应转换
  - 成功 → `200 {"ok": true, "id": "username"}`
  - 失败 → 非 200 状态码（Hysteria 以此判断认证失败）
- [ ] **1.6.7** 创建 `src/HysteriaAuth.Agent/Models/` 下的模型类
  - `HysteriaAuthRequest.cs`（`addr`, `auth`, `tx`）
  - `HysteriaAuthResponse.cs`（`ok`, `id`）
  - `InternalAuthRequest.cs`（`username`, `password`, `nodeId`, `clientIp`）
  - `InternalAuthResponse.cs`（`success`, `userId`, `message`）
- [ ] **1.6.8** 创建 `Config/agent.json` 最小可用配置

#### 3.7 中间件

- [ ] **1.7.1** 创建 `Middleware/JwtMiddleware.cs`
  - 从 `Authorization: Bearer {token}` 解析 JWT
  - 验证签名和过期时间
  - 将 AdminId + Role 注入 `HttpContext.Items`
  - **仅应用于** `/api/v1/admin/*` 和 `/api/v1/users/*` 路由
- [ ] **1.7.2** 创建 `Middleware/NodeAuthMiddleware.cs`
  - 从 `X-Node-Secret` 请求头提取节点密钥
  - 查询数据库验证密钥有效性
  - 将 NodeId 注入 `HttpContext.Items`
  - **仅应用于** `/api/v1/auth/*` 和 `/api/v1/nodes/*` 路由
- [ ] **1.7.3** 创建 `Middleware/ExceptionMiddleware.cs`
  - 捕获所有未处理异常
  - 生成 `requestId`（UUID 格式，如 `req_abc123def456`）
  - 区分业务异常（自定义异常类，含 `ErrorCode`）和系统异常
  - 业务异常 → 返回对应 HTTP 状态码 + 错误代码
  - 系统异常 → 返回 500 + `internal_error`
  - 记录完整错误日志（含 `requestId`）
- [ ] **1.7.4** 创建 `Models/DTOs/ErrorResponse.cs`

```csharp
public class ErrorResponse
{
    public ErrorDetail Error { get; set; }
}

public class ErrorDetail
{
    public string Code { get; set; }        // 如 "invalid_credentials"
    public string Message { get; set; }     // 人类可读描述
    public string RequestId { get; set; }   // 请求追踪 ID
}
```

- [ ] **1.7.5** 创建自定义异常类 `AppException`（含 `ErrorCode` + `HttpStatusCode`）
  - 派生异常：`NotFoundException`, `ConflictException`, `UnauthorizedException`, `ForbiddenException`, `ValidationException`

#### 3.8 配置与入口

- [ ] **1.8.1** 创建完整的 `appsettings.json`（包含 [`project-structure-configuration.md`](../architect/project-structure-configuration.md) §2 的全部配置节）
- [ ] **1.8.2** 创建 `Config/AppSettings.cs`、`Config/JwtSettings.cs`
- [ ] **1.8.3** 在 `Program.cs` 中注册：
  - DbContext + SQLite
  - 所有 Repository（DI）
  - 所有 Service（DI）
  - JWT Authentication
  - 中间件管道（Exception → Auth → 路由 → Controller）
  - JSON 序列化配置（`DateTime` → ISO 8601 UTC）

---

## 4. 应遵守的规范

> 以下为 Phase 1 特别需要关注的规范条款。全局规范见 [`00-README.md`](00-README.md)「全局强制规范」。

| 规范来源 | 条款 | Phase 1 适用要点 |
|----------|------|-----------------|
| [时间戳规范](../develop/backend-development-spec.md#1-时间戳规范) | §1.1~§1.5 | 所有 Entity 的 `CreatedAt`、`UpdatedAt`、`ExpiresAt`、`AuthTime`、`LockedUntil` 等字段必须 ISO 8601 UTC |
| [通用编码规范](../develop/backend-development-spec.md#2-通用编码规范) | §2.3 | 严格分层：Controllers 不含业务逻辑，Services 不含数据访问细节 |
| [API 规范](../develop/backend-development-spec.md#3-api-规范) | §3.1.2 | 所有错误必须返回 `{"error": {"code": "...", "message": "...", "requestId": "..."}}` |
| [API 规范](../develop/backend-development-spec.md#3-api-规范) | §3.2 | 错误代码与 HTTP 状态码对应关系不可混用 |
| [数据库规范](../develop/backend-development-spec.md#4-数据库规范) | §4.1 | BCrypt Work Factor = 12、流量字段 BIGINT/字节单位、软删除用 `IsActive` |
| [认证流程规范](../develop/backend-development-spec.md#5-认证流程规范) | §5.3 | 认证检查顺序不可重排 |
| [认证流程规范](../develop/backend-development-spec.md#5-认证流程规范) | §5.4 | 管理员登录锁定：5 次/15 分钟 |
| [认证流程规范](../develop/backend-development-spec.md#5-认证流程规范) | §5.5 | JWT: HMAC-SHA256、密钥 ≥ 256-bit |
| [安全规范](../develop/backend-development-spec.md#6-安全规范) | §6.1 | BCrypt Work Factor = 12 |
| [日志规范](../develop/backend-development-spec.md#10-日志与监控规范) | §10.1 | 禁止日志中输出密码、密钥 |

---

## 5. 应特别注意的事项

### 5.1 时间戳（最容易出问题）

- ❌ **绝对禁止**：`DateTime.Now`
- ✅ **必须使用**：`DateTime.UtcNow`
- 数据库中存储 UTC，读取时不自动转换时区
- JSON 序列化输出格式必须为 `"2025-12-31T23:59:59Z"`
- **检查点**：在 `Program.cs` 中验证 JSON 序列化配置不会将 DateTime 转为本地时区

### 5.2 密码安全

- BCrypt Work Factor **必须**为 12，不允许更低
- 验证密码使用 `BCrypt.Net.BCrypt.Verify(password, hash)`
- 密码字段长度 `VARCHAR(256)` 足够容纳 BCrypt 哈希（约 60 字符）

### 5.3 认证检查顺序

- [`AuthService`](#35-认证api主服务器侧) 中的 6 步检查**顺序不可变**
- 前两步（用户存在、密码正确）统一返回 `invalid_credentials`（安全考虑，不暴露具体是用户名错误还是密码错误）
- 后续检查按序返回对应错误代码

### 5.4 协议转换的正确性（Phase 1 最核心的逻辑）

- Hysteria 的 `auth` 字段是 Base64，必须解码后再按 `:` 分割
- 注意 `auth` 可能不含 `:`，此时整个 `auth` 作为 username
- `addr` 可能为 IPv4 (`1.2.3.4:12345`) 或 IPv6 (`[::1]:12345`)，`ExtractIp` 方法需兼容
- 响应转换：Hysteria 要求成功返回 `200 {"ok": true, "id": "..."}`，失败返回任何非 200

### 5.5 异常中间件的 requestId

- `requestId` 必须在中间件管道的最早阶段生成（不晚于 ExceptionMiddleware）
- 建议格式：`req_` + 12 位随机 hex（如 `req_a1b2c3d4e5f6`）
- 该 `requestId` 同时用于日志关联和错误响应返回

### 5.6 数据库约束

- `TrafficRecord.IdempotencyKey` 的 UNIQUE 约束：虽然 Phase 1 不处理流量，但表结构必须创建
- `Node.ProvisionToken` UNIQUE 约束：Phase 1 可以留空（NULL），Phase 2 会用到
- `AdminAuditLog` 只读：Phase 1 虽然尚未写审计日志，但 DbContext 不应暴露对 `AdminAuditLogs` 的 `Update`/`Delete` 入口

### 5.7 种子数据

- 必须提供默认 `super_admin` 账号的创建机制
- 建议通过配置读取初始密码（默认 "admin123"），首次登录后强制修改
- BCrypt 加密后存储

---

## 6. 关键代码模板与示例

### 6.1 User Entity

```csharp
// Models/Entities/User.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HysteriaAuth.Master.Models.Entities;

public class User
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    [Required, MaxLength(64)]
    public string Username { get; set; } = string.Empty;

    [Required, MaxLength(256)]
    public string Password { get; set; } = string.Empty;

    [MaxLength(128)]
    public string? Email { get; set; }

    public long TotalTrafficBytes { get; set; }

    public long UsedTrafficBytes { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ExpiresAt { get; set; }

    /// <summary>允许的节点ID列表（JSON数组，NULL=全部节点）</summary>
    public string? AllowedNodes { get; set; }

    public string? Remark { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
```

### 6.2 认证检查顺序实现

```csharp
// Services/AuthService.cs — 核心认证逻辑
public async Task<AuthResult> AuthenticateAsync(AuthRequest request)
{
    // Step 1: 用户存在?
    var user = await _userRepo.GetByUsernameAsync(request.Username);
    if (user == null)
        return AuthResult.Fail("invalid_credentials", 401);

    // Step 2: 密码正确?
    if (!BCrypt.Net.BCrypt.Verify(request.Password, user.Password))
        return AuthResult.Fail("invalid_credentials", 401);

    // Step 3: 账号激活?
    if (!user.IsActive)
        return AuthResult.Fail("account_disabled", 403);

    // Step 4: 账号过期?
    if (user.ExpiresAt.HasValue && user.ExpiresAt.Value < DateTime.UtcNow)
        return AuthResult.Fail("account_expired", 403);

    // Step 5: 流量充足?
    if (user.UsedTrafficBytes >= user.TotalTrafficBytes)
        return AuthResult.Fail("traffic_exhausted", 403);

    // Step 6: 节点允许?
    if (!string.IsNullOrEmpty(user.AllowedNodes))
    {
        var allowedNodes = JsonSerializer.Deserialize<List<string>>(user.AllowedNodes);
        if (allowedNodes != null && !allowedNodes.Contains(request.NodeId))
            return AuthResult.Fail("node_not_allowed", 403);
    }

    // 认证成功 → 记录日志
    await _authLogRepo.AddAsync(new AuthLog
    {
        UserId = user.Id,
        Username = user.Username,
        NodeId = request.NodeId,
        ClientIp = request.ClientIp,
        Success = true,
        AuthTime = DateTime.UtcNow
    });

    return AuthResult.Success(user.Id, user.TotalTrafficBytes - user.UsedTrafficBytes, user.ExpiresAt);
}
```

### 6.3 Edge Agent 协议转换

```csharp
// Services/AuthProxy.cs
public async Task<HysteriaAuthResponse> HandleAuthAsync(HysteriaAuthRequest request)
{
    // 1. 解析 auth 字段（Base64 编码的凭据串）
    var (username, password) = ParseAuthField(request.Auth);

    // 2. 构造内部 API 请求
    var internalRequest = new InternalAuthRequest
    {
        Username = username,
        Password = password,
        NodeId = _config.NodeId,
        ClientIp = ExtractIp(request.Addr)
    };

    // 3. 转发到主服务器
    var requestMsg = new HttpRequestMessage(HttpMethod.Post,
        $"{_config.MasterServerUrl}/api/v1/auth/hysteria");
    requestMsg.Headers.Add("X-Node-Secret", _config.NodeSecret);
    requestMsg.Content = JsonContent.Create(internalRequest);

    var response = await _httpClient.SendAsync(requestMsg);

    // 4. 转换响应为 Hysteria 原生格式
    if (response.IsSuccessStatusCode)
    {
        var body = await response.Content.ReadFromJsonAsync<InternalAuthResponse>();
        return new HysteriaAuthResponse { Ok = true, Id = username };
    }

    return new HysteriaAuthResponse { Ok = false, Id = "" };
}

private (string username, string password) ParseAuthField(string auth)
{
    // Base64 解码
    var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(auth));
    // 按 : 分割
    var parts = decoded.Split(':', 2);
    return parts.Length == 2
        ? (parts[0], parts[1])
        : (decoded, "");
}

private string ExtractIp(string addr)
{
    // 处理 IPv4: "1.2.3.4:12345" → "1.2.3.4"
    // 处理 IPv6: "[::1]:12345" → "::1"
    var lastColon = addr.LastIndexOf(':');
    if (lastColon < 0) return addr;

    var ipPart = addr[..lastColon];
    if (ipPart.StartsWith("[") && ipPart.EndsWith("]"))
        return ipPart[1..^1];
    return ipPart;
}
```

### 6.4 全局异常中间件

```csharp
// Middleware/ExceptionMiddleware.cs
public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var requestId = $"req_{Guid.NewGuid():N}"[..15]; // req_ + 12 hex
        context.Items["RequestId"] = requestId;
        context.Response.Headers["X-Request-Id"] = requestId;

        try
        {
            await _next(context);
        }
        catch (AppException ex)
        {
            // 业务异常 — 使用预定义的错误代码和状态码
            context.Response.StatusCode = ex.StatusCode;
            context.Response.ContentType = "application/json";
            var error = new ErrorResponse
            {
                Error = new ErrorDetail
                {
                    Code = ex.ErrorCode,
                    Message = ex.Message,
                    RequestId = requestId
                }
            };
            await context.Response.WriteAsJsonAsync(error);
            _logger.LogWarning(ex, "业务异常: {ErrorCode} | RequestId: {RequestId}",
                ex.ErrorCode, requestId);
        }
        catch (Exception ex)
        {
            // 系统异常 — 统一返回 internal_error
            _logger.LogError(ex, "未处理异常 | RequestId: {RequestId}", requestId);
            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";
            var error = new ErrorResponse
            {
                Error = new ErrorDetail
                {
                    Code = "internal_error",
                    Message = "服务器内部错误",
                    RequestId = requestId
                }
            };
            await context.Response.WriteAsJsonAsync(error);
        }
    }
}
```

---

## 7. 阶段完成标准

| 标准 | 验证方式 |
|------|----------|
| 全部 9 张表的 EF Core Migration 成功应用 | `dotnet ef database update` 无错误 |
| 用户 CRUD 全部端点可正常调用 | curl/Postman 测试 6 个端点 |
| 管理员可登录并获取 JWT Token | `POST /api/v1/admin/login` 返回有效 Token |
| JWT 中间件保护的管理端点在无 Token 时返回 401 | curl 不带 Token → 401 |
| 认证 API 在正确凭据下返回成功 | `POST /api/v1/auth/hysteria` 返回 `{"success": true, "userId": ...}` |
| 认证 API 在错误密码下返回 `invalid_credentials` | 错误密码 → 401 + `invalid_credentials` |
| 认证 API 在禁用账号下返回 `account_disabled` | `IsActive=false` 的用户 → 403 + `account_disabled` |
| Edge Agent 认证代理正确转换协议 | Hysteria 原生 `POST /auth` → Edge Agent → Master Server → 正确 Hysteria 响应 |
| 异常中间件在所有未捕获异常时返回统一错误格式 | 触发异常 → `{"error": {"code": "...", "message": "...", "requestId": "..."}}` |
| 日志中不包含密码/密钥 | grep 日志文件无敏感信息 |
| 代码通过 `dotnet build` 无错误 | `dotnet build` 退出码 0 |

---

## 8. 下一阶段 (Phase 2) 交接清单

Phase 1 完成后，以下是 Phase 2 开发人员需要了解的关键信息：

| 交接项 | 说明 | 参考位置 |
|--------|------|----------|
| 全部 Entity 模型 | 9 张表的完整定义和关系 | [`database-design.md`](../architect/database-design.md) |
| Node Entity 的预留字段 | `SecretKey`、`SecretVersion`、`ProvisionToken`、`ProvisionStatus`、`TrafficStatsPort`、`TrafficStatsSecret` — Phase 2 需要填充 | [`Node.cs`](#) |
| Node 认证方式 | 通过 `X-Node-Secret` 请求头 + `NodeAuthMiddleware` 验证 | [`NodeAuthMiddleware.cs`](#) |
| Edge Agent 当前能力 | Phase 1 仅实现了 `AuthProxy`（认证代理），尚无 `Initializer`、`TrafficCollector`、`StatusReporter`、`SystemMonitor` | [`edge-node-design.md`](../architect/edge-node-design.md) |
| 配置文件 | `agent.json` 当前只有最小配置，Phase 2 需要扩展 `Init`、`Monitor`、`Reporter`、`HealthCheck` 等配置节 | [`agent.json`](#) |
| JWT 配置 | `JwtSettings` 和 `JwtService` 已就绪，`NodeService` 可直接使用 | [`JwtService.cs`](#) |
| AppException 体系 | 自定义异常类已定义，Phase 2 新增功能应复用 | [`ExceptionMiddleware.cs`](#) |
