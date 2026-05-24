# 审计日志模块 — 开发落地指南

> **阶段**: 审计模块 | **预估工期**: 2-3 天 | **依赖**: Phase 1（核心基础：数据库、管理员认证、中间件管道）
>
> **来源文档**: [`document/architect/audit-module.md`](../architect/audit-module.md) · [`document/exchange/master-panel-api.md`](../exchange/master-panel-api.md) · [`document/develop/backend-development-spec.md`](../develop/backend-development-spec.md) · [`document/architect/database-design.md`](../architect/database-design.md) · [`document/architect/api-design.md`](../architect/api-design.md)

---

## 1. 阶段目标与范围

### 1.1 总体目标

实现完整的**管理员操作审计日志模块**，覆盖日志写入、多维查询、不可篡改保护。使所有管理员的关键操作均可追溯、可审计。

### 1.2 范围清单

| 序号 | 交付项 | 说明 |
|------|--------|------|
| A.1 | 审计日志仓储层 | `IAuditLogRepository` + `AuditLogRepository` |
| A.2 | 审计日志 DTO | `AuditLogDto`、`AuditLogListResponse`、`AuditLogQuery` |
| A.3 | 审计日志服务 | `AuditService`（写入 + 分页查询） |
| A.4 | 审计日志查询 API | `GET /api/v1/admin/audit-logs`（Controller 端点） |
| A.5 | 审计上下文中间件 | `AuditContextMiddleware`（注入 AdminId + ClientIp） |
| A.6 | Service 层审计写入 | 在所有 Service 关键操作中植入审计日志写入 |
| A.7 | 不可篡改保护验证 | `AppDbContext` 拦截 Update/Delete 操作 |
| A.8 | DI 注册与管道集成 | `Program.cs` 注册所有新增服务 + 中间件 |

---

## 2. 阶段启动前置检查

> 审计模块基于 Phase 1 已完成的数据库和基础架构。以下为启动前的验证清单。

| # | 检查项 | 验证内容 | 通过标准 |
|---|--------|----------|----------|
| 1 | `AdminAuditLog` 实体存在 | 检查 `Models/Entities/AdminAuditLog.cs` | 实体完整（9 字段 + 导航属性） |
| 2 | `AdminAuditLogs` 表已创建 | 检查数据库表结构 | Migration 包含该表，字段与设计一致 |
| 3 | `AppDbContext` 不可篡改保护就绪 | 检查 `SaveChanges` / `SaveChangesAsync` | 拦截 `Modified` + `Deleted` 状态 |
| 4 | JWT 中间件就绪 | 检查 `JwtMiddleware` | 从 Token 解析 `AdminId` 注入 `HttpContext.Items["AdminId"]` |
| 5 | 全局异常中间件就绪 | 检查 `ExceptionMiddleware` | 统一错误格式 + `RequestId` |
| 6 | `AdminService` 已实现 | 检查 `AdminService.cs` | `LoginAsync`、`CreateAdminAsync`、`UpdateAdminAsync` 已实现 |
| 7 | `UserService` 已实现 | 检查 `UserService.cs` | `CreateAsync`、`UpdateAsync`、`SoftDeleteAsync`、`ResetTrafficAsync` 已实现 |
| 8 | `NodeService` 已实现 | 检查 `NodeService.cs` | `PreRegisterNodeAsync`、`RotateSecretAsync` 已实现 |
| 9 | `KickService` 已实现 | 检查 `KickService.cs` | `AdminKickUserAsync` 已实现 |
| 10 | ✅ 阅读 [`audit-module.md`](../architect/audit-module.md) 全部内容 | — | — |
| 11 | ✅ 阅读 [`master-panel-api.md` §8.1](../exchange/master-panel-api.md#81-get-apiv1adminaudit-logs--获取审计日志) | — | — |

---

## 3. 具体任务清单

### 第 1 天：仓储层 + DTO + 服务层 + 中间件

#### 3.1 审计日志仓储层

- [ ] **A.1.1** 创建 `Repositories/IAuditLogRepository.cs`：

```csharp
namespace HysteriaAuth.Master.Repositories;

public interface IAuditLogRepository
{
    Task AddAsync(AdminAuditLog log);
    Task<(List<AdminAuditLog> Items, int Total)> GetPagedAsync(AuditLogQuery query);
}
```

- [ ] **A.1.2** 创建 `Repositories/AuditLogRepository.cs`：

```csharp
namespace HysteriaAuth.Master.Repositories;

public class AuditLogRepository : IAuditLogRepository
{
    private readonly AppDbContext _context;

    public AuditLogRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(AdminAuditLog log)
    {
        _context.AdminAuditLogs.Add(log);
        await _context.SaveChangesAsync();
    }

    public async Task<(List<AdminAuditLog> Items, int Total)> GetPagedAsync(AuditLogQuery query)
    {
        var q = _context.AdminAuditLogs
            .Include(a => a.Admin)
            .AsQueryable();

        if (query.AdminId.HasValue)
            q = q.Where(l => l.AdminId == query.AdminId.Value);
        if (!string.IsNullOrWhiteSpace(query.Action))
            q = q.Where(l => l.Action == query.Action);
        if (!string.IsNullOrWhiteSpace(query.TargetType))
            q = q.Where(l => l.TargetType == query.TargetType);
        if (query.StartTime.HasValue)
            q = q.Where(l => l.CreatedAt >= query.StartTime.Value);
        if (query.EndTime.HasValue)
            q = q.Where(l => l.CreatedAt < query.EndTime.Value);

        var total = await q.CountAsync();

        var items = await q
            .OrderByDescending(l => l.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        return (items, total);
    }
}
```

#### 3.2 审计日志 DTO

- [ ] **A.2.1** 创建 `Models/DTOs/AuditLogDto.cs`：

```csharp
namespace HysteriaAuth.Master.Models.DTOs;

public class AuditLogDto
{
    public long Id { get; set; }
    public long AdminId { get; set; }
    public string AdminName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string TargetType { get; set; } = string.Empty;
    public string? TargetId { get; set; }
    public string? Detail { get; set; }
    public string ClientIp { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class AuditLogListResponse
{
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public List<AuditLogDto> Items { get; set; } = new();
}

public class AuditLogQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public long? AdminId { get; set; }
    public string? Action { get; set; }
    public string? TargetType { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
}
```

#### 3.3 审计日志服务

- [ ] **A.3.1** 创建 `Services/AuditService.cs`：

```csharp
using System.Text.Json;
using HysteriaAuth.Master.Models.DTOs;
using HysteriaAuth.Master.Models.Entities;
using HysteriaAuth.Master.Repositories;

namespace HysteriaAuth.Master.Services;

public class AuditService
{
    private readonly IAuditLogRepository _auditLogRepo;
    private readonly ILogger<AuditService> _logger;

    public AuditService(
        IAuditLogRepository auditLogRepo,
        ILogger<AuditService> logger)
    {
        _auditLogRepo = auditLogRepo;
        _logger = logger;
    }

    public async Task LogAsync(
        long adminId,
        string action,
        string targetType,
        string? targetId,
        object? detail,
        string clientIp)
    {
        var log = new AdminAuditLog
        {
            AdminId = adminId,
            Action = action,
            TargetType = targetType,
            TargetId = targetId,
            Detail = detail != null ? JsonSerializer.Serialize(detail) : null,
            ClientIp = clientIp,
            CreatedAt = DateTime.UtcNow
        };

        await _auditLogRepo.AddAsync(log);

        _logger.LogInformation(
            "审计日志: AdminId={AdminId} Action={Action} Target={TargetType}:{TargetId} IP={ClientIp}",
            adminId, action, targetType, targetId, clientIp);
    }

    public async Task<AuditLogListResponse> GetPagedAsync(AuditLogQuery query)
    {
        // pageSize 最大限制 200
        if (query.PageSize > 200) query.PageSize = 200;

        var (items, total) = await _auditLogRepo.GetPagedAsync(query);

        return new AuditLogListResponse
        {
            Total = total,
            Page = query.Page,
            PageSize = query.PageSize,
            Items = items.Select(MapToDto).ToList()
        };
    }

    private static AuditLogDto MapToDto(AdminAuditLog log)
    {
        return new AuditLogDto
        {
            Id = log.Id,
            AdminId = log.AdminId,
            AdminName = log.Admin?.Username ?? "unknown",
            Action = log.Action,
            TargetType = log.TargetType,
            TargetId = log.TargetId,
            Detail = log.Detail,
            ClientIp = log.ClientIp,
            CreatedAt = log.CreatedAt
        };
    }
}
```

#### 3.4 审计上下文中间件

- [ ] **A.5.1** 创建 `Middleware/AuditContextMiddleware.cs`：

```csharp
namespace HysteriaAuth.Master.Middleware;

/// <summary>
/// 审计上下文中间件 — 将客户端的真实 IP 注入 HttpContext.Items。
/// AdminId 已在 JwtMiddleware 中注入。
/// JwtMiddleware 在 AuditContextMiddleware 之前执行。
/// </summary>
public class AuditContextMiddleware
{
    private readonly RequestDelegate _next;

    public AuditContextMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // 获取客户端 IP（优先取 X-Forwarded-For，否则用 RemoteIpAddress）
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        var clientIp = !string.IsNullOrWhiteSpace(forwardedFor)
            ? forwardedFor.Split(',')[0].Trim()
            : context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        context.Items["ClientIp"] = clientIp;

        await _next(context);
    }
}
```

### 第 2 天：Controller 端点 + Service 层审计写入

#### 3.5 Controller 审计日志查询端点

- [ ] **A.4.1** 在 `Controllers/AdminController.cs` 中新增审计日志端点：

```csharp
/// <summary>
/// 获取审计日志（super_admin 和 admin 可调用）。
/// </summary>
[HttpGet("audit-logs")]
public async Task<IActionResult> GetAuditLogs(
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 50,
    [FromQuery] long? adminId = null,
    [FromQuery] string? action = null,
    [FromQuery] string? targetType = null,
    [FromQuery] DateTime? startTime = null,
    [FromQuery] DateTime? endTime = null)
{
    // readonly 角色无查看权限
    var role = HttpContext.Items["Role"]?.ToString();
    if (role == "readonly")
        throw new ForbiddenException("forbidden", "只读管理员无权查看审计日志");

    var query = new AuditLogQuery
    {
        Page = page,
        PageSize = pageSize,
        AdminId = adminId,
        Action = action,
        TargetType = targetType,
        StartTime = startTime,
        EndTime = endTime
    };

    var result = await _auditService.GetPagedAsync(query);
    return Ok(result);
}
```

- [ ] **A.4.2** 在 `AdminController` 的构造函数中注入 `AuditService`（如果尚未注入）。

> **注意**：`AdminController` 现有构造函数注入 `AdminService` 和 `KickService`，现在需要追加 `AuditService`。

#### 3.6 Service 层审计日志写入

- [ ] **A.6.1** **UserService** 植入审计日志：

修改 `UserService.cs`，在以下方法中写入审计日志。Service 的方法签名需要扩展以接收 `adminId` 和 `clientIp` 参数：

| 方法 | Action | TargetType | TargetId | Detail |
|------|--------|------------|----------|--------|
| `CreateAsync` | `create` | `user` | `user.Id` | 创建时的用户数据 |
| `UpdateAsync` | `update` | `user` | `user.Id` | `{ before, after, changedFields }` |
| `SoftDeleteAsync` | `delete` | `user` | `user.Id` | `{ before, after }` |
| `ResetTrafficAsync` | `update` | `user` | `user.Id` | `{ before: { usedTrafficBytes }, after: { usedTrafficBytes: 0 } }` |

**修改模式**（以 `UpdateAsync` 为例）：

```csharp
public async Task<UserDto> UpdateAsync(long id, UpdateUserRequest request,
    long adminId, string clientIp)
{
    var user = await _userRepo.GetByIdAsync(id);
    if (user == null)
        throw new NotFoundException("用户不存在");

    // 记录变更前数据
    var before = MapToDto(user);

    // 更新操作
    if (request.Email != null) user.Email = request.Email;
    if (request.TotalTrafficBytes.HasValue) user.TotalTrafficBytes = request.TotalTrafficBytes.Value;
    if (request.IsActive.HasValue) user.IsActive = request.IsActive.Value;
    if (request.ExpiresAt != null) user.ExpiresAt = request.ExpiresAt;
    if (request.AllowedNodes != null)
        user.AllowedNodes = JsonSerializer.Serialize(request.AllowedNodes);
    if (request.Remark != null) user.Remark = request.Remark;
    if (!string.IsNullOrWhiteSpace(request.Password))
        user.Password = BCrypt.Net.BCrypt.HashPassword(request.Password, 12);

    await _userRepo.UpdateAsync(user);

    // 记录变更后数据
    var after = MapToDto(user);

    // 写入审计日志
    await _auditService.LogAsync(
        adminId: adminId,
        action: "update",
        targetType: "user",
        targetId: id.ToString(),
        detail: new
        {
            before = new
            {
                before.Email,
                before.IsActive,
                before.TotalTrafficBytes,
                before.UsedTrafficBytes,
                before.Remark
            },
            after = new
            {
                after.Email,
                after.IsActive,
                after.TotalTrafficBytes,
                after.UsedTrafficBytes,
                after.Remark
            },
            changedFields = GetChangedFields(before, after)
        },
        clientIp: clientIp
    );

    return after;
}
```

> **Controller 侧也需要修改**：从 `HttpContext.Items` 提取 `AdminId` 和 `ClientIp` 并传递给 Service。

```csharp
// UsersController.cs
[HttpPut("{userId:long}")]
public async Task<IActionResult> UpdateUser(long userId, [FromBody] UpdateUserRequest request)
{
    var adminId = (long)HttpContext.Items["AdminId"]!;
    var clientIp = HttpContext.Items["ClientIp"]?.ToString() ?? "unknown";

    var user = await _userService.UpdateAsync(userId, request, adminId, clientIp);
    return Ok(user);
}
```

- [ ] **A.6.2** **AdminService** 植入审计日志：

| 方法 | Action | TargetType | TargetId | Detail |
|------|--------|------------|----------|--------|
| `LoginAsync` | `login` | `admin` | `admin.Id` | `{ "username": "..." }` |
| `CreateAdminAsync` | `create` | `admin` | `admin.Id` | 创建时的管理员数据 |
| `UpdateAdminAsync` | `update` | `admin` | `admin.Id` | `{ before, after, changedFields }` |

> **注意**：`LoginAsync` 方法不需要 `adminId` 参数（此时管理员尚未登录），直接从登录成功后的 admin 实体获取。

- [ ] **A.6.3** **NodeService** 植入审计日志：

| 方法 | Action | TargetType | TargetId | Detail |
|------|--------|------------|----------|--------|
| `PreRegisterNodeAsync` | `create` | `node` | `node.Id` | `{ "name": "...", "port": ... }` |
| `RotateSecretAsync` | `rotate_secret` | `node` | `node.Id` | `{ "secretVersion": X → Y }` |

- [ ] **A.6.4** **KickService/AdminController** 植入审计日志：

| 操作 | Action | TargetType | TargetId | Detail |
|------|--------|------------|----------|--------|
| 踢用户下线 | `kick_user` | `user` | `username` | `{ "nodeId": "...", "username": "..." }` |

#### 3.7 DI 注册与 Program.cs 集成

- [ ] **A.8.1** 在 `Program.cs` 中注册仓储和服务：

```csharp
// Repository 注册
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();

// Service 注册
builder.Services.AddScoped<AuditService>();
```

- [ ] **A.8.2** 在 `Program.cs` 中间件管道中注册审计上下文中间件：

```csharp
// 中间件管道顺序（在 JwtAuth 之后，路由之前）
app.UseJwtAuth();      // 注入 AdminId
app.UseAuditContext(); // 注入 ClientIp（扩展方法）
```

> **中间件顺序要求**：`AuditContextMiddleware` 必须在 `JwtMiddleware` 之后执行（依赖 `AdminId`），且在 `MapControllers` 之前执行（确保 Controller 中可读取上下文）。

---

## 4. 应遵守的规范

| 规范来源 | 条款 | 审计模块适用要点 |
|----------|------|-----------------|
| [时间戳规范](../develop/backend-development-spec.md#1-时间戳规范) | §1.1~§1.5 | `AdminAuditLog.CreatedAt` 必须为 `DateTime.UtcNow`，ISO 8601 UTC 格式 |
| [通用编码规范](../develop/backend-development-spec.md#2-通用编码规范) | §2.3 | 严格分层：Controller 提取上下文 → Service 写入日志 → Repository 数据访问 |
| [通用编码规范](../develop/backend-development-spec.md#2-通用编码规范) | §2.4 | 命名：`IAuditLogRepository`、`AuditService`、`AuditLogDto` |
| [API 规范](../develop/backend-development-spec.md#3-api-规范) | §3.1.2 | 审计日志查询的错误使用统一格式 |
| [API 规范](../develop/backend-development-spec.md#3-api-规范) | §3.2 | 权限不足 → `forbidden` (403) |
| [数据库规范](../develop/backend-development-spec.md#4-数据库规范) | §4.10 | `AdminAuditLogs` 不可物理删除、不可修改 |
| [数据库规范](../develop/backend-development-spec.md#4-数据库规范) | §4.11 | 审计日志保留 365 天（`Admin.AuditLogRetentionDays`） |
| [安全规范](../develop/backend-development-spec.md#6-安全规范) | §6.3 | 日志脱敏：Detail 不记录密码、密钥等敏感信息 |
| [安全规范](../develop/backend-development-spec.md#6-安全规范) | §6.3 | 审计追踪：所有管理员操作记录到 `AdminAuditLogs` |
| [日志规范](../develop/backend-development-spec.md#10-日志与监控规范) | §10.1 | 审计日志写入 → Information 级别；越权访问审计日志 → Warning 级别 |

---

## 5. 应特别注意的事项

### 5.1 审计日志不可篡改的正确实现

`AppDbContext` 中的 `SaveChanges` 拦截已经在 Phase 1 实现：

```csharp
public override int SaveChanges()
{
    var auditLogEntries = ChangeTracker.Entries<AdminAuditLog>()
        .Where(e => e.State == EntityState.Modified || e.State == EntityState.Deleted);

    if (auditLogEntries.Any())
    {
        throw new InvalidOperationException("AdminAuditLogs 记录不可修改或删除。审计日志一旦写入即不可变更。");
    }

    return base.SaveChanges();
}
```

**需要验证**：
- ❌ 不能使用裸 SQL 语句（`context.Database.ExecuteSqlRaw`）绕过此检查
- ❌ `DataRetentionService` 清理过期日志时，需要直接操作 `DbContext` 的 `RemoveRange`（系统级操作不受此限制）

### 5.2 审计日志写入与业务操作的一致性

审计日志的写入时机直接影响到数据一致性：

```csharp
// ✅ 正确：业务操作成功后再写审计日志
await _userRepo.UpdateAsync(user);          // 1. 业务操作
await _auditService.LogAsync(...);          // 2. 审计日志
// 如果 2 失败 → 业务操作已提交，审计日志丢失（日志缺失）

// ⚠️ 权衡：如果将审计日志写入与业务操作放在同一事务中
// 则审计日志写入失败会导致业务回滚（可用性降低）
```

**建议策略**：
- 审计日志写入与业务操作**不在同一事务**中
- 业务操作成功后 → 写入审计日志（如果写入失败，记录 Error 日志，但不影响业务返回）
- 这样可以保证业务可用性高于审计完整性

### 5.3 Controller 与 Service 之间的上下文传递

审计日志需要的上下文信息（AdminId、ClientIp）在 Controller 层可获取，但 Service 层需要这些参数：

| 传递方式 | 优点 | 缺点 |
|----------|------|------|
| **方法参数传递（推荐）** | 显式、类型安全、易测试 | 修改方法签名、级联影响 |
| `IHttpContextAccessor` 注入 | 不需要改方法签名 | 隐蔽依赖、单元测试困难 |

**推荐使用方法参数传递**：

```csharp
// Controller
[HttpPost]
public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
{
    var adminId = (long)HttpContext.Items["AdminId"]!;
    var clientIp = HttpContext.Items["ClientIp"]?.ToString() ?? "unknown";
    var user = await _userService.CreateAsync(request, adminId, clientIp);
    return CreatedAtAction(nameof(GetUser), new { userId = user.Id }, user);
}

// Service
public async Task<UserDto> CreateAsync(CreateUserRequest request, long adminId, string clientIp)
{
    // ... 业务逻辑 ...
    await _auditService.LogAsync(adminId, "create", "user", user.Id.ToString(), detail, clientIp);
    return MapToDto(user);
}
```

### 5.4 Detail 字段的数据脱敏

审计日志的 `Detail` 字段**绝对不能**包含密码原文：

```json
// ❌ 错误：包含了密码
{
    "before": { "username": "user123", "password": "plaintext123" },
    "after": { "username": "user123", "password": "$2a$12$..." }
}

// ✅ 正确：排除密码字段
{
    "before": { "username": "user123", "email": "old@example.com", "isActive": true },
    "after": { "username": "user123", "email": "new@example.com", "isActive": false },
    "changedFields": ["email", "isActive"]
}
```

**DTO 中排除密码字段**（`UserDto` 和 `AdminDto` 已经不含密码，可直接用于审计日志的 Detail）：

```csharp
// ✅ UserDto 不包含 Password 字段，可直接用于 audit detail
public class UserDto
{
    public long Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? Email { get; set; }
    public long TotalTrafficBytes { get; set; }
    public long UsedTrafficBytes { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? AllowedNodes { get; set; }
    public string? Remark { get; set; }
    // ❌ 无 Password 字段
}
```

### 5.5 审计日志查询性能

审计日志可能积累大量数据（每天数百到数千条），需要注意：

| 优化点 | 说明 |
|--------|------|
| **索引** | `(AdminId, CreatedAt DESC)`、`Action`、`TargetType`、`CreatedAt` |
| **pageSize 上限** | 最大 200，防止客户端单次请求过大 |
| **深度分页** | 限制最大 100 页（或切换为游标分页） |
| **时间范围限制** | 不指定 `startTime`/`endTime` 时默认最近 30 天（可选） |
| **数据保留** | 365 天后自动清理，控制表大小 |

### 5.6 权限校验

审计日志查询的权限要求：

| 角色 | 权限 | 验证方式 |
|------|------|----------|
| `super_admin` | 可查询全部审计日志 | JWT 中间件通过 `Role` claim |
| `admin` | 可查询全部审计日志 | 同上 |
| `readonly` | **不可查询**（403 Forbidden） | Controller 中检查 `role` |

```csharp
// Controller 中的权限校验（或使用 [Authorize] 策略）
[HttpGet("audit-logs")]
public async Task<IActionResult> GetAuditLogs(...)
{
    var role = HttpContext.Items["Role"]?.ToString();
    if (role == "readonly")
        throw new ForbiddenException("forbidden", "权限不足，无法查看审计日志");
    // ... 查询逻辑 ...
}
```

---

## 6. 关键代码模板与示例

### 6.1 完整查询 API 实现

```csharp
// AdminController.cs — 完整实现
[HttpGet("audit-logs")]
public async Task<IActionResult> GetAuditLogs(
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 50,
    [FromQuery] long? adminId = null,
    [FromQuery] string? action = null,
    [FromQuery] string? targetType = null,
    [FromQuery] DateTime? startTime = null,
    [FromQuery] DateTime? endTime = null)
{
    // 权限检查：只读管理员不可查看审计日志
    var role = HttpContext.Items["Role"]?.ToString();
    if (role == "readonly")
        throw new ForbiddenException("forbidden", "权限不足");

    var query = new AuditLogQuery
    {
        Page = Math.Max(1, page),
        PageSize = Math.Clamp(pageSize, 1, 200),
        AdminId = adminId,
        Action = action,
        TargetType = targetType,
        StartTime = startTime,
        EndTime = endTime
    };

    var result = await _auditService.GetPagedAsync(query);
    return Ok(result);
}
```

### 6.2 审计日志写入 — UserService 完整示例

```csharp
// Services/UserService.cs — 带审计日志的 CreateAsync
public async Task<UserDto> CreateAsync(CreateUserRequest request, long adminId, string clientIp)
{
    if (await _userRepo.CheckUsernameExistsAsync(request.Username))
        throw new ConflictException("用户名已存在");

    var user = new User
    {
        Username = request.Username,
        Password = BCrypt.Net.BCrypt.HashPassword(request.Password, 12),
        Email = request.Email,
        TotalTrafficBytes = request.TotalTrafficBytes,
        IsActive = request.IsActive,
        ExpiresAt = request.ExpiresAt,
        AllowedNodes = request.AllowedNodes != null
            ? JsonSerializer.Serialize(request.AllowedNodes)
            : null,
        Remark = request.Remark,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    user = await _userRepo.AddAsync(user);
    var dto = MapToDto(user);

    // 审计日志
    await _auditService.LogAsync(
        adminId: adminId,
        action: "create",
        targetType: "user",
        targetId: user.Id.ToString(),
        detail: new
        {
            username = dto.Username,
            dto.Email,
            dto.TotalTrafficBytes,
            dto.IsActive,
            dto.Remark
        },
        clientIp: clientIp
    );

    return dto;
}
```

### 6.3 审计日志 DTO 映射

```csharp
// AdminAuditLog → AuditLogDto 映射复用
// 使用 AutoMapper（如果项目已引入）或手写映射

// 手写映射（推荐，简洁无依赖）
private static AuditLogDto MapToDto(AdminAuditLog log)
{
    return new AuditLogDto
    {
        Id = log.Id,
        AdminId = log.AdminId,
        AdminName = log.Admin?.Username ?? "unknown",
        Action = log.Action,
        TargetType = log.TargetType,
        TargetId = log.TargetId,
        Detail = log.Detail,
        ClientIp = log.ClientIp,
        CreatedAt = log.CreatedAt
    };
}
```

### 6.4 审计上下文中间件注册扩展方法

```csharp
// Middleware/AuditContextMiddleware.cs — 扩展方法
public static class AuditContextMiddlewareExtensions
{
    public static IApplicationBuilder UseAuditContext(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<AuditContextMiddleware>();
    }
}

// Program.cs 中使用
app.UseJwtAuth();
app.UseAuditContext();  // 新增
app.MapControllers();
```

---

## 7. 阶段完成标准

| 标准 | 验证方式 |
|------|----------|
| 审计日志仓储 `IAuditLogRepository` + 实现完整 | 代码审查：`AddAsync` + `GetPagedAsync` 实现 |
| 审计日志 DTO 完整 | 检查 `AuditLogDto`、`AuditLogListResponse`、`AuditLogQuery` |
| 审计日志服务 `AuditService` 完整 | 代码审查：`LogAsync` + `GetPagedAsync` 实现 |
| `GET /api/v1/admin/audit-logs` 端点可访问 | curl 返回 `200` + 分页格式数据（可空） |
| 审计日志查询 API 支持 5 个筛选参数 | curl 测试各参数组合 |
| `readonly` 角色查询审计日志返回 403 | 使用 readonly Token 测试 |
| 创建用户 → `AdminAuditLogs` 有记录 | `SELECT * FROM AdminAuditLogs WHERE Action='create'` |
| 更新用户 → `AdminAuditLogs` 有 `before/after` 记录 | Detail 字段包含 JSON |
| 删除用户 → `AdminAuditLogs` 有 `delete` 记录 | Action='delete', TargetType='user' |
| 管理员登录 → `AdminAuditLogs` 有 `login` 记录 | Action='login', TargetType='admin' |
| 审计日志**不可手动修改/删除** | 尝试 `PUT /api/v1/admin/audit-logs/1` → 404 或无此路由 |
| 审计日志**不可通过 EF Core 修改** | 代码审查 `AppDbContext.SaveChanges` 拦截逻辑 |
| 审计日志 Detail 字段**不含密码明文** | 审查所有写入点的 Detail 数据 |
| 中间件正确注入 `ClientIp` | `HttpContext.Items["ClientIp"]` 有值 |
| `Program.cs` 中注册了审计相关服务 | 代码审查 DI 注册 |
| `dotnet build` 通过 | 退出码 0 |

---

## 8. 下一阶段交接清单

| 交接项 | 说明 | 参考位置 |
|--------|------|----------|
| 审计日志完整实现 | 仓储 + 服务 + Controller + 中间件 + Service 层植入 | 本阶段全部文件 |
| 不可篡改保护 | `AppDbContext.SaveChanges` 拦截 + 代码审查 | [`audit-module.md` §2.3](../architect/audit-module.md#23-不可篡改保护) |
| 前端 API 对接 | 前端通过 `GET /api/v1/admin/audit-logs` 调用 | [`master-panel-api.md` §8.1](../exchange/master-panel-api.md#81-get-apiv1adminaudit-logs--获取审计日志) |
| Detail 格式说明 | 前端需 `JSON.parse()` 展示操作详情 | [`audit-module.md` §5.2](../architect/audit-module.md#52-detail-字段格式规范) |
| 权限限制 | `readonly` 角色不可查看审计日志，前端需根据角色隐藏入口 | [`audit-module.md` §10.2](../architect/audit-module.md#102-权限控制) |
