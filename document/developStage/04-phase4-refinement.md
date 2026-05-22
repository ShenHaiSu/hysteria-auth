# Phase 4: 完善功能

> **阶段**: Phase 4 | **预估工期**: 1 周 | **依赖**: Phase 3（流量统计）
>
> **来源文档**: [`document/architect/resilience-monitoring.md`](../architect/resilience-monitoring.md) · [`../architect/security-design.md`](../architect/security-design.md) §4, §5 · [`../architect/deployment.md`](../architect/deployment.md) §4 · [`../architect/api-design.md`](../architect/api-design.md) §4.4.7 · [`../develop/backend-development-spec.md`](../develop/backend-development-spec.md) §3.5, §4.10, §9, §10

---

## 1. 阶段目标与范围

### 1.1 总体目标

在前三个阶段的核心功能基础上，补齐系统的**横切关注点**：认证缓存、日志体系完结、审计日志、异常降级增强、速率限制、CORS 配置、数据备份与恢复、系统监控面板 API。

Phase 4 完成后，系统达到**生产就绪**级别——不仅功能完整，还具备安全防护、性能优化、运维支持。

### 1.2 范围清单

| 序号 | 交付项 | 说明 |
|------|--------|------|
| 4.1 | Edge Agent 认证缓存 | 缓存认证结果，主服务器不可达时降级使用 |
| 4.2 | 管理员操作审计日志 | `AdminAuditLog` 表写入逻辑 + 审计中间件 |
| 4.3 | 审计日志查询 API | `GET /api/v1/admin/audit-logs` |
| 4.4 | 异常降级增强 | 缓存过期降级、流量上报失败缓存补报 |
| 4.5 | 速率限制中间件 | ASP.NET Core `RateLimiter`，按端点分组配置 |
| 4.6 | CORS 配置 | 仅管理 API 路由启用 |
| 4.7 | 数据备份与恢复 | SQLite `.backup` 命令定时备份 + 清理脚本 |
| 4.8 | 系统监控面板 API | `GET /api/v1/admin/dashboard` |
| 4.9 | SLO 指标埋点 | 认证延迟、心跳处理延迟、API 错误率统计 |
| 4.10 | 请求体大小限制 | 全局 1MB 限制 |

---

## 2. 阶段启动前置检查

> Phase 4 启动前，必须对 Phase 3 的以下关键产物进行审查和验证。

| # | 检查项 | 验证内容 | 通过标准 |
|----|--------|----------|----------|
| P3.1 | 流量方向映射正确 | 用户下载大文件 → 检查 DB | `BytesOut` 增长（非 `BytesIn`） |
| P3.2 | 幂等键生效 | 构造重复心跳 | 仅计入一次，日志显示"跳过" |
| P3.3 | 并发扣减正确 | 两个线程同时更新同一用户 | `UsedTrafficBytes` 最终值 = 两者之和 |
| P3.4 | 超额踢人工作 | 用户流量超额 | Hysteria 日志显示 `/kick` 被调用 |
| P3.5 | 会话状态机正确 | 观察 stages: active → idle → closed | 数据库记录符合预期 |
| P3.6 | 心跳事务完整 | 部分失败 → 整体回滚 | DB 中无部分写入数据 |
| P3.7 | `AdminAuditLog` 表存在但无数据 | 检查表结构和记录数 | 有表结构无记录 |
| P3.8 | CORS/速率限制尚未配置 | `Program.cs` 中无相关代码 | 确认待实现 |
| P3.9 | ✅ 阅读 [`backend-development-spec.md`](../develop/backend-development-spec.md) §3.5（速率限制）、§4.10（审计日志）、§9（异常处理与降级）、§10（日志与监控）、§11（测试规范） | — | — |

---

## 3. 具体任务清单

### 第 1~3 天：认证缓存 + 审计日志 + 降级增强

#### 3.1 Edge Agent 认证缓存

- [ ] **4.1.1** 在 `AuthProxy` 中实现内存缓存
  - 缓存结构：`ConcurrentDictionary<string, CachedAuthResult>`
  - 缓存 Key：`username`
  - 缓存内容：`{ Ok: true, Id: "username", CachedAt: UTC }`
- [ ] **4.1.2** 缓存过期时间：`Cache.ExpirationMinutes`（默认 5 分钟）
- [ ] **4.1.3** 缓存上限：`Cache.MaxSize`（默认 1000），超过时使用 LRU 策略淘汰
- [ ] **4.1.4** 实现**降级策略**（主服务器不可达时使用缓存认证）：

```csharp
// AuthProxy — 带缓存和降级的认证方法
public async Task<HysteriaAuthResponse> HandleAuthAsync(HysteriaAuthRequest request)
{
    var (username, password) = ParseAuthField(request.Auth);

    // 1. 检查缓存
    if (_cache.TryGet(username, out var cached) && !cached.IsExpired)
        return cached.Response;

    try
    {
        // 2. 正常路径：主服务器认证
        var result = await _masterClient.AuthenticateAsync(username, password, ...);
        if (result.Success)
            _cache.Set(username, result.ToHysteriaResponse());
        return result.ToHysteriaResponse();
    }
    catch (Exception ex) when (_cache.IsEnabled && _cache.TryGet(username, out var cached))
    {
        // 3. 降级路径：主服务器不可达 → 使用缓存（即使已过期）
        _logger.LogWarning(ex, "主服务器不可达，使用缓存认证: {Username}", username);
        return cached.Response;
    }
    catch (Exception ex)
    {
        // 4. 彻底失败
        _logger.LogError(ex, "认证失败: {Username}", username);
        return HysteriaAuthResponse.Fail();
    }
}
```

- [ ] **4.1.5** 缓存失效条件：
  - 正常过期（5 分钟后主动清除）
  - 管理员踢用户下线时主动清除缓存
  - 用户信息变更（密码修改、禁用）时主动清除缓存

#### 3.2 管理员操作审计日志

- [ ] **4.2.1** 创建 `Services/AuditService.cs`
  - `LogAsync(adminId, action, targetType, targetId, detail, clientIp)`
- [ ] **4.2.2** 在所有管理员写操作中写入审计日志：

| 操作 | `Action` | `TargetType` | `TargetId` |
|------|----------|-------------|------------|
| 创建用户 | `create` | `user` | 用户 ID |
| 更新用户 | `update` | `user` | 用户 ID |
| 删除用户（软删除） | `delete` | `user` | 用户 ID |
| 重置用户流量 | `update` | `user` | 用户 ID |
| 管理员登录 | `login` | `admin` | 管理员 ID |
| 创建管理员 | `create` | `admin` | 管理员 ID |
| 更新管理员 | `update` | `admin` | 管理员 ID |
| 踢用户下线 | `kick_user` | `user` | 用户名 |
| 预注册节点 | `create` | `node` | 节点 ID |
| 轮换节点密钥 | `update` | `node` | 节点 ID |

- [ ] **4.2.3** 审计日志 `Detail` 字段格式（JSON）：
  ```json
  {
      "before": { "isActive": true, "totalTrafficBytes": 10737418240 },
      "after": { "isActive": false, "totalTrafficBytes": 10737418240 },
      "changedFields": ["isActive"]
  }
  ```
- [ ] **4.2.4** 创建 `Middleware/AuditMiddleware.cs`（或使用 ActionFilter）
  - 自动从 `HttpContext.Items` 提取 `AdminId` 和 `ClientIp`
  - 对标注了 `[Auditable]` 特性的 Action 自动记录审计日志
- [ ] **4.2.5** **强制约束**：`AdminAuditLog` 表仅提供 `INSERT` 和 `SELECT`，不提供 `UPDATE`/`DELETE` API

#### 3.3 审计日志查询 API

- [ ] **4.3.1** 创建 `GET /api/v1/admin/audit-logs?page=1&pageSize=50&adminId=&action=&targetType=&startTime=&endTime=`
  - 认证：`Authorization: Bearer {admin_token}`
  - 权限：`super_admin` 和 `admin` 角色
  - 支持按管理员、操作类型、目标类型、时间范围筛选
  - 返回分页列表

#### 3.4 异常降级增强

- [ ] **4.4.1** 流量上报失败缓存补报：
  - Edge Agent 的上报失败时，将流量数据序列化到本地文件（`/var/lib/hysteria-auth/agent/pending_traffic.json`）
  - 下次上报成功时，将本地缓存的待补报数据合并发送
  - 补报数据携带原始时间戳（用于幂等键计算）
- [ ] **4.4.2** 缓存过期且主服务器不可达时的行为：
  - 拒绝**新连接**（返回认证失败）
  - 允许**已连接用户**继续使用（Hysteria 不会因单次认证超时断开已有连接）
- [ ] **4.4.3** 节点密钥无效时的处理（增强）：
  - 当前行为（Phase 2）：记录 Error，持续重试
  - 增强：增加管理员通知机制（如写入告警日志 + 标记节点 `ProvisionStatus = "revoked"`）

---

### 第 4~5 天：速率限制 + CORS + 备份 + Dashboard

#### 3.5 速率限制中间件

- [ ] **4.5.1** 在 `Program.cs` 中配置 ASP.NET Core 内置 [`RateLimiter`](https://learn.microsoft.com/en-us/aspnet/core/performance/rate-limit)：
  ```csharp
  builder.Services.AddRateLimiter(options =>
  {
      options.AddFixedWindowLimiter("AuthPolicy", config =>
      {
          config.PermitLimit = 100;
          config.Window = TimeSpan.FromMinutes(1);
          config.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
      });
      options.AddFixedWindowLimiter("AdminLoginPolicy", config =>
      {
          config.PermitLimit = 10;
          config.Window = TimeSpan.FromMinutes(1);
      });
      options.AddFixedWindowLimiter("AdminApiPolicy", config =>
      {
          config.PermitLimit = 60;
          config.Window = TimeSpan.FromMinutes(1);
      });
      options.RejectionStatusCode = 429;
  });
  ```

- [ ] **4.5.2** 按端点分组应用策略：

| 端点分组 | 策略名 | 限制 |
|----------|--------|------|
| `/api/v1/auth/*` | `AuthPolicy` | 100 次/分钟/IP |
| `/api/v1/admin/login` | `AdminLoginPolicy` | 10 次/分钟/IP |
| `/api/v1/admin/*` 其他 | `AdminApiPolicy` | 60 次/分钟/Token |
| `/api/v1/nodes/*/heartbeat` | 不限制 | — |
| `/health` | 不限制 | — |

- [ ] **4.5.3** 超出限制时返回统一错误格式：
  ```json
  {
      "error": {
          "code": "rate_limited",
          "message": "请求过于频繁，请稍后重试",
          "requestId": "req_abc123def456"
      }
  }
  ```

#### 3.6 CORS 配置

- [ ] **4.6.1** 在 `Program.cs` 中配置 CORS：
  - **仅对管理 API 路由启用**：`/api/v1/admin/*`、`/api/v1/users/*`
  - **节点通信 API 不启用**：`/api/v1/auth/*`、`/api/v1/nodes/*`（无浏览器场景）
- [ ] **4.6.2** CORS 配置项：
  ```csharp
  builder.Services.AddCors(options =>
  {
      options.AddPolicy("AdminCors", policy =>
      {
          policy.WithOrigins(config.Cors.AllowedOrigins)
                .WithMethods(config.Cors.AllowedMethods)
                .WithHeaders(config.Cors.AllowedHeaders)
                .WithExposedHeaders("X-Request-Id")
                .SetPreflightMaxAge(TimeSpan.FromSeconds(config.Cors.MaxAgeSeconds));
      });
  });
  ```

#### 3.7 数据备份与恢复

- [ ] **4.7.1** 创建 `scripts/backup-db.sh`
  ```bash
  # 使用 sqlite3 .backup 命令在线热备份
  sqlite3 "$DB_PATH" ".backup '$BACKUP_DIR/hysteria-auth-$(date +%Y-%m-%d-%H%M%S).db'"
  # 清理超过 30 天的旧备份
  find "$BACKUP_DIR" -name "hysteria-auth-*.db" -mtime +$RETENTION_DAYS -delete
  ```
- [ ] **4.7.2** 创建 `scripts/restore-db.sh`
  - 停止主服务 → 备份当前数据库 → 恢复指定备份 → 启动主服务
- [ ] **4.7.3** 实现**应用层自动备份**（`BackgroundService`）：
  - 每 `Backup.BackupIntervalHours`（默认 24h）执行一次
  - 可配置开关 `Backup.AutoBackupEnabled`
- [ ] **4.7.4** 创建 `Services/DatabaseBackupService.cs`

#### 3.8 系统监控面板 API

- [ ] **4.8.1** 创建 `GET /api/v1/admin/dashboard`
  - 返回聚合数据：
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
- [ ] **4.8.2** 实现各指标的计算查询：
  - `totalUsers`: `SELECT COUNT(*) FROM Users`
  - `activeUsers`: `SELECT COUNT(*) FROM Users WHERE IsActive = 1`
  - `totalNodes`: `SELECT COUNT(*) FROM Nodes`
  - `activeNodes`: `SELECT COUNT(*) FROM Nodes WHERE IsActive = 1`
  - `onlineUsersNow`: 从最新的 `NodeTraffic` 或 `Sessions` 汇总
  - `totalTrafficToday/TrafficThisMonth`: 从 `TrafficRecords` 聚合查询

#### 3.9 SLO 指标埋点

- [ ] **4.9.1** 在 `ExceptionMiddleware` 中添加请求计数器（按端点 + 状态码分组）
- [ ] **4.9.2** 在 `AuthService` 中记录认证请求延迟：
  ```csharp
  var sw = Stopwatch.StartNew();
  var result = await AuthenticateInternalAsync(request);
  sw.Stop();
  _metrics.RecordAuthLatency(sw.ElapsedMilliseconds, result.Success);
  ```
- [ ] **4.9.3** 在 `NodeService.ProcessHeartbeatAsync` 中记录心跳处理延迟
- [ ] **4.9.4** SLO 达标标准：
  - 认证 P50 < 50ms, P99 < 500ms
  - 心跳处理 P99 < 100ms
  - API 错误率（5xx）< 0.1%

#### 3.10 请求体大小限制

- [ ] **4.10.1** 在 `Program.cs` 中配置全局请求体大小限制：
  ```csharp
  builder.WebHost.ConfigureKestrel(options =>
  {
      options.Limits.MaxRequestBodySize = 1 * 1024 * 1024; // 1MB
  });
  ```
- [ ] **4.10.2** 超出限制时返回 `413 Payload Too Large`

---

## 4. 应遵守的规范

| 规范来源 | 条款 | Phase 4 适用要点 |
|----------|------|-----------------|
| [API 规范](../develop/backend-development-spec.md#3-api-规范) | §3.5 | 速率限制：100次/分钟(auth)、10次/分钟(login)、60次/分钟(admin)、心跳和健康检查不限 |
| [API 规范](../develop/backend-development-spec.md#3-api-规范) | §3.6 | CORS：仅管理 API 启用，节点通信 API 不启用 |
| [API 规范](../develop/backend-development-spec.md#3-api-规范) | §3.7 | 请求体大小限制 1MB |
| [数据库规范](../develop/backend-development-spec.md#4-数据库规范) | §4.10 | `AdminAuditLogs` 不可物理删除、不可修改 |
| [数据库规范](../develop/backend-development-spec.md#4-数据库规范) | §4.11 | 数据库备份保留 30 天 |
| [安全规范](../develop/backend-development-spec.md#6-安全规范) | §6.3 | 日志脱敏（密码/密钥不出现）、请求体大小限制 |
| [异常处理规范](../develop/backend-development-spec.md#9-异常处理与降级规范) | §9.1 | 全表：主服务器不可达、缓存过期、数据库异常、心跳超时、采集失败、上报失败、密钥无效、锁冲突 |
| [异常处理规范](../develop/backend-development-spec.md#9-异常处理与降级规范) | §9.2 | 降级策略模板（Phase 1 定义了框架，Phase 4 实现完整逻辑） |
| [日志规范](../develop/backend-development-spec.md#10-日志与监控规范) | §10.1 | 全级别定义（Trace → Critical） |
| [日志规范](../develop/backend-development-spec.md#10-日志与监控规范) | §10.2 | SLO 指标达标验证 |
| [部署规范](../develop/backend-development-spec.md#13-部署规范) | §13.3 | SQLite `.backup`、24h 间隔、30 天保留 |

---

## 5. 应特别注意的事项

### 5.1 缓存一致性问题

- 认证缓存在 Edge Agent 本地内存中，当用户信息在主服务器变更时：
  - 密码修改 → 缓存中的旧密码仍然"有效"（因为只缓存了认证结果而非密码）
  - `isActive = false` → 缓存中的结果是成功的，新连接可能被允许
  - **解决方案**：
    - 缓存过期时间 5 分钟足够短
    - 关键操作（踢人、禁用）主动清除该用户的缓存
    - 主服务器提供 `POST /api/v1/nodes/{nodeId}/cache/invalidate` 端点供管理员手动清除

### 5.2 审计日志的性能影响

- 每次管理员写操作都同步写入 `AdminAuditLog`，可能影响响应延迟
- **优化策略**：
  - 审计日志写入使用**同步调用**（保证不丢失）
  - 但可以异步写入变更前后对比的 `Detail` JSON（使用后台任务计算）
  - 审计日志表建立 `(AdminId, CreatedAt)` 和 `(Action, TargetType)` 索引

### 5.3 速率限制的 Key 选择

- 认证 API 速率限制以 **IP** 为 Key（因为 Edge Agent 汇聚多用户）
- 管理 API 速率限制以 **Token**（即 AdminId）为 Key
- 管理登录以 **IP** 为 Key（防止暴力破解）
- 注意：如果主服务器在 Nginx 反向代理后面，需要正确获取客户端 IP（`X-Forwarded-For` 或 `X-Real-IP`）

### 5.4 CORS 的精细控制

- ❌ 不能全局启用 CORS（`AllowAnyOrigin`）
- 必须精确配置 `AllowedOrigins`
- 节点通信 API（`/api/v1/auth/*`、`/api/v1/nodes/*`）完全不需要 CORS
- 暴露 `X-Request-Id` 响应头供前端调试

### 5.5 数据备份的时机

- SQLite `.backup` 命令是**在线热备份**，不会阻塞读写
- 建议在业务低峰期（如凌晨 3 点）执行
- 备份文件应存储到与数据库文件**不同的磁盘**（防止单点故障）
- 备份完成后的验证：检查文件大小是否合理（> 0 且接近源文件大小）

### 5.6 SLO 指标的可观测性

- Phase 4 的 SLO 埋点是内建的，但完整的监控体系（Prometheus/Grafana）是未来规划
- 当前阶段至少保证：
  - 日志中可 grep 出 P50/P99 延迟
  - API 错误率可通过日志统计
  - 为未来接入 Prometheus metrics 预留接口

### 5.7 Dashboard 性能

- Dashboard API 的聚合查询可能很重（特别是 `totalTrafficToday/ThisMonth`）
- 建议：
  - 对 `TrafficRecords.RecordedAt` 建索引
  - 考虑使用缓存（每分钟刷新一次）
  - 或使用独立的汇总表（`DailyTrafficSummary`）

---

## 6. 关键代码模板与示例

### 6.1 认证缓存实现

```csharp
// Services/AuthCache.cs — Edge Agent 侧
public class AuthCache
{
    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();
    private readonly int _maxSize;
    private readonly int _expirationMinutes;

    public bool IsEnabled { get; }

    public AuthCache(AgentConfig config)
    {
        IsEnabled = config.Cache.Enabled;
        _maxSize = config.Cache.MaxSize;
        _expirationMinutes = config.Cache.ExpirationMinutes;
    }

    public bool TryGet(string username, out CachedAuthResult result)
    {
        result = null;
        if (!IsEnabled) return false;

        if (_cache.TryGetValue(username, out var entry))
        {
            result = entry.Result;
            return true;
        }
        return false;
    }

    public void Set(string username, HysteriaAuthResponse response)
    {
        if (!IsEnabled) return;

        // LRU 淘汰：超过上限时清理最旧的条目
        if (_cache.Count >= _maxSize)
        {
            var oldest = _cache.OrderBy(kv => kv.Value.CachedAt).First();
            _cache.TryRemove(oldest.Key, out _);
        }

        _cache[username] = new CacheEntry
        {
            Result = new CachedAuthResult
            {
                Response = response,
                CachedAt = DateTime.UtcNow,
                IsExpired => DateTime.UtcNow - CachedAt > TimeSpan.FromMinutes(_expirationMinutes)
            },
            CachedAt = DateTime.UtcNow
        };
    }

    public void Invalidate(string username)
    {
        _cache.TryRemove(username, out _);
    }

    private class CacheEntry
    {
        public CachedAuthResult Result { get; init; }
        public DateTime CachedAt { get; init; }
    }
}
```

### 6.2 审计日志服务

```csharp
// Services/AuditService.cs
public class AuditService
{
    private readonly AppDbContext _context;
    private readonly ILogger<AuditService> _logger;

    public async Task LogAsync(
        long adminId,
        string action,
        string targetType,
        string targetId,
        object before,
        object after,
        string clientIp)
    {
        var detail = new
        {
            before,
            after,
            changedFields = GetChangedFields(before, after),
            timestamp = DateTime.UtcNow
        };

        _context.AdminAuditLogs.Add(new AdminAuditLog
        {
            AdminId = adminId,
            Action = action,
            TargetType = targetType,
            TargetId = targetId,
            Detail = JsonSerializer.Serialize(detail),
            ClientIp = clientIp,
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
        _logger.LogInformation(
            "审计日志: AdminId={AdminId} Action={Action} TargetType={TargetType} TargetId={TargetId}",
            adminId, action, targetType, targetId);
    }

    private List<string> GetChangedFields(object before, object after)
    {
        // 使用反射比较两个对象的差异字段
        // 简化版：比较 JSON 序列化后的差异
        var beforeJson = JsonSerializer.Serialize(before);
        var afterJson = JsonSerializer.Serialize(after);
        // ... 比较逻辑 ...
        return new List<string>();
    }
}
```

### 6.3 速率限制响应处理

```csharp
// Program.cs — 速率限制配置
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = 429;
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = 429;
        context.HttpContext.Response.ContentType = "application/json";

        var requestId = context.HttpContext.Items["RequestId"]?.ToString()
            ?? $"req_{Guid.NewGuid():N}"[..15];

        var error = new ErrorResponse
        {
            Error = new ErrorDetail
            {
                Code = "rate_limited",
                Message = "请求过于频繁，请稍后重试",
                RequestId = requestId
            }
        };

        await context.HttpContext.Response.WriteAsJsonAsync(error, cancellationToken);
    };

    // 策略定义
    options.AddFixedWindowLimiter("AuthPolicy", c =>
    {
        c.PermitLimit = 100;
        c.Window = TimeSpan.FromMinutes(1);
    });
    options.AddFixedWindowLimiter("AdminLoginPolicy", c =>
    {
        c.PermitLimit = 10;
        c.Window = TimeSpan.FromMinutes(1);
    });
    options.AddFixedWindowLimiter("AdminApiPolicy", c =>
    {
        c.PermitLimit = 60;
        c.Window = TimeSpan.FromMinutes(1);
    });
});

// 在 Controller 中使用
[EnableRateLimiting("AuthPolicy")]
[HttpPost("hysteria")]
public async Task<IActionResult> Authenticate(...) { ... }
```

### 6.4 数据库备份服务

```csharp
// Services/DatabaseBackupService.cs
public class DatabaseBackupService : BackgroundService
{
    private readonly IConfiguration _config;
    private readonly ILogger<DatabaseBackupService> _logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_config.GetValue<bool>("Backup:AutoBackupEnabled"))
        {
            _logger.LogInformation("自动备份已禁用");
            return;
        }

        var backupDir = _config.GetValue<string>("Backup:BackupDirectory");
        var dbPath = _config.GetConnectionString("DefaultConnection")
            ?.Replace("Data Source=", "");
        var retentionDays = _config.GetValue<int>("Backup:RetentionDays", 30);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                Directory.CreateDirectory(backupDir);

                var backupFile = Path.Combine(backupDir,
                    $"hysteria-auth-{DateTime.UtcNow:yyyy-MM-dd-HHmmss}.db");

                // SQLite 在线热备份
                using var connection = new SqliteConnection($"Data Source={dbPath}");
                await connection.OpenAsync(stoppingToken);

                using var backup = connection.CreateCommand();
                backup.CommandText = $"VACUUM INTO '{backupFile}'";
                await backup.ExecuteNonQueryAsync(stoppingToken);

                _logger.LogInformation("数据库备份完成: {BackupFile}", backupFile);

                // 清理过期备份
                foreach (var file in Directory.GetFiles(backupDir, "hysteria-auth-*.db"))
                {
                    if (File.GetCreationTimeUtc(file) < DateTime.UtcNow.AddDays(-retentionDays))
                    {
                        File.Delete(file);
                        _logger.LogDebug("清理过期备份: {File}", file);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "数据库备份失败");
            }

            var intervalHours = _config.GetValue<int>("Backup:BackupIntervalHours", 24);
            await Task.Delay(TimeSpan.FromHours(intervalHours), stoppingToken);
        }
    }
}
```

### 6.5 Dashboard 聚合查询

```csharp
// Services/AdminService.cs
public async Task<DashboardResponse> GetDashboardAsync()
{
    var now = DateTime.UtcNow;
    var todayStart = now.Date;
    var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

    return new DashboardResponse
    {
        TotalUsers = await _context.Users.CountAsync(),
        ActiveUsers = await _context.Users.CountAsync(u => u.IsActive),
        TotalNodes = await _context.Nodes.CountAsync(),
        ActiveNodes = await _context.Nodes.CountAsync(n => n.IsActive),
        OnlineUsersNow = await _context.Sessions.CountAsync(s => s.Status == "active"),
        TotalTrafficToday = await _context.TrafficRecords
            .Where(t => t.RecordedAt >= todayStart)
            .SumAsync(t => t.BytesIn + t.BytesOut),
        TotalTrafficThisMonth = await _context.TrafficRecords
            .Where(t => t.RecordedAt >= monthStart)
            .SumAsync(t => t.BytesIn + t.BytesOut)
    };
}
```

---

## 7. 阶段完成标准

| 标准 | 验证方式 |
|------|----------|
| 认证缓存命中率 > 0 | Agent 日志显示缓存命中（Debug 级别） |
| 主服务器不可达时缓存降级生效 | 停掉主服务器 → Agent 仍然返回认证成功（缓存内用户） |
| 管理员操作被记录到审计日志 | 创建/更新/删除用户 → `AdminAuditLogs` 表有新记录 |
| 审计日志不可修改/删除 | 尝试直接操作数据库 → 无 API 入口（代码审查确认） |
| 审计日志查询 API 可用 | `GET /api/v1/admin/audit-logs` → 分页数据返回 |
| 超出速率限制返回 429 | 对 `/api/v1/auth/*` 发送 > 100 次/分钟 → 429 |
| 管理 API CORS 头正确 | 浏览器跨域请求 → `Access-Control-Allow-Origin` 头存在 |
| 节点通信 API 无 CORS 头 | `curl -v /api/v1/auth/*` → 无 `Access-Control-*` 头 |
| 数据库自动备份正常 | 等待 24h → 备份目录有 `.db` 文件 |
| 过期备份自动清理 | 备份目录中无超过 30 天的文件 |
| Dashboard API 返回正确聚合数据 | `GET /api/v1/admin/dashboard` → 数据与实际统计一致 |
| 请求体超过 1MB 返回 413 | `curl -d @2MBfile` → 413 |
| API 错误率 < 0.1% | 查看日志统计 5xx 比例 |
| 代码通过 `dotnet build` | 退出码 0 |

---

## 8. 下一阶段 (Phase 5) 交接清单

| 交接项 | 说明 | 参考位置 |
|--------|------|----------|
| 全部功能清单 | Phase 1-4 完成后，全部 10 大核心功能已实现 | [`overview.md`](../architect/overview.md) §1.3 |
| 需要测试的服务列表 | `AuthService`, `UserService`, `NodeService`, `TrafficService`, `AdminService`, `AuditService`, `JwtService`, `AuthProxy`, `TrafficCollector` | 各自 `.cs` 文件 |
| 覆盖率目标 | 各服务 80%~90% 目标 | [`backend-development-spec.md`](../develop/backend-development-spec.md) §11.2 |
| 必测场景 | 认证各失败原因、协议转换、并发扣减、幂等检查、心跳事务回滚、E2E 认证链路 | [`resilience-monitoring.md`](../architect/resilience-monitoring.md) §3.4 |
| 部署脚本 | `deploy-master.sh`、`deploy-agent.sh`、`backup-db.sh`、`restore-db.sh` | 需创建或完善 |
| 配置文件 | `appsettings.json`（完整）、`agent.json`（完整） | Phase 5 需验证配置完整性 |
| systemd 服务文件 | `hysteria-auth-master.service`、`hysteria-auth-agent.service` | Phase 5 需创建 |
