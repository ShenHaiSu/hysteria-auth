# Phase 3: 流量统计

> **阶段**: Phase 3 | **预估工期**: 1-2 周 | **依赖**: Phase 2（节点管理）
>
> **来源文档**: [`document/architect/traffic-statistics.md`](../architect/traffic-statistics.md) · [`../architect/edge-node-design.md`](../architect/edge-node-design.md) §5 · [`../architect/api-design.md`](../architect/api-design.md) §4.3.2 · [`../architect/database-design.md`](../architect/database-design.md) §2.2, §2.4 · [`../develop/backend-development-spec.md`](../develop/backend-development-spec.md) §7, §8

---

## 1. 阶段目标与范围

### 1.1 总体目标

在 Phase 2 节点管理体系之上，建立完整的**流量统计系统**：Edge Agent 从本地 Hysteria 采集各用户流量 → 合并到心跳上报 → 主服务器幂等扣减 → 会话生命周期管理 → 并发控制。这是整个系统最核心、最容易出错的部分。

Phase 3 完成后，系统能精确追踪每个用户的流量使用情况，在超额时自动踢用户下线。

### 1.2 范围清单

| 序号 | 交付项 | 说明 |
|------|--------|------|
| 3.1 | Edge Agent 流量采集模块 (`TrafficCollector`) | 定时调用 Hysteria `GET /traffic?clear=1` + `GET /online` |
| 3.2 | 心跳请求扩展：合并流量数据 | `userTraffic` + `onlineUsers` 字段填入心跳 |
| 3.3 | 主服务器流量扣减服务 (`TrafficService`) | 乐观并发控制 + 最多 3 次重试 |
| 3.4 | 幂等键机制 | `{nodeId}_{username}_{timestamp_rounded}` + UNIQUE 约束 |
| 3.5 | 扩展心跳事务：含流量扣减 | 在事务中添加 `TrafficRecords` 写入 + `Users.UsedTrafficBytes` 更新 |
| 3.6 | 流量方向映射 | `tx` → `BytesOut`（用户下载）, `rx` → `BytesIn`（用户上传） |
| 3.7 | 超额检测 + 踢用户下线 | 主服务器检测超额 → 通知 Edge Agent → 调用 Hysteria `POST /kick` |
| 3.8 | 会话生命周期管理 | `Sessions` 表：`active` → `idle` → `closed` 状态机 |
| 3.9 | 用户流量统计 API | `GET /api/v1/users/{userId}/traffic-stats?period=` |
| 3.10 | 数据保留策略 | 后台定时任务清理过期数据 |

---

## 2. 阶段启动前置检查

> Phase 3 启动前，必须对 Phase 2 的以下关键产物进行审查和验证。

| # | 检查项 | 验证内容 | 通过标准 |
|----|--------|----------|----------|
| P2.1 | 心跳上报链路畅通 | Edge Agent → Master 心跳正常 | `NodeStatus`/`NodeTraffic` 表有新记录 |
| P2.2 | 心跳事务结构正确 | 检查 `NodeService.ProcessHeartbeatAsync` 代码 | 有 `BeginTransactionAsync` + `CommitAsync` |
| P2.3 | `HeartbeatRequest` 包含 `userTraffic` 字段 | 检查 DTO 定义 | `Dictionary<string, TrafficEntry>` + `Dictionary<string, int>` |
| P2.4 | `TrafficRecord` Entity 的 `IdempotencyKey` 有 UNIQUE 约束 | 检查 Migration 或 `OnModelCreating` | UNIQUE INDEX |
| P2.5 | `User` Entity 包含 `[Timestamp] RowVersion` | 检查实体定义 | `[Timestamp] public byte[] RowVersion` |
| P2.6 | `Session` Entity 存在且包含全部 8 字段 | 检查实体 + Migration | `Id`, `UserId`, `NodeId`, `SessionKey`, `StartedAt`, `EndedAt`, `BytesIn`, `BytesOut`, `Status` |
| P2.7 | `User` 的 `TotalTrafficBytes` 和 `UsedTrafficBytes` 可正常读写 | 手动更新一个用户的流量字段 | DB 值正确变更 |
| P2.8 | `AuthService` 第 5 步流量检查正常工作 | 设置用户 `UsedTrafficBytes >= TotalTrafficBytes` → 认证 | 返回 `traffic_exhausted` |
| P2.9 | Hysteria `trafficStats` API 可访问（开发环境） | `curl -H 'Authorization: secret' http://127.0.0.1:9999/traffic` | 返回 JSON |
| P2.10 | ✅ 重新阅读 [`backend-development-spec.md`](../develop/backend-development-spec.md) §7（流量统计规范）全部 | — | — |
| P2.11 | ✅ 重新阅读 [`backend-development-spec.md`](../develop/backend-development-spec.md) §8（并发控制与数据一致性）全部 | — | — |

---

## 3. 具体任务清单

### 第 1 周：流量采集 + 心跳扩展 + 流量扣减 + 幂等性

#### 3.1 Edge Agent 流量采集模块

- [ ] **3.1.1** 创建 `src/HysteriaAuth.Agent/Services/TrafficCollector.cs`
- [ ] **3.1.2** 实现 `GET /traffic?clear=1` 调用：
  ```csharp
  GET http://127.0.0.1:9999/traffic?clear=1
  Authorization: {trafficStatsSecret}
  ```
  - 获取各用户的累计流量
  - `?clear=1` 确保采集后 Hysteria 内部清零（增量采集的关键）
  - 返回格式：`{"user1": {"tx": 1024000, "rx": 512000}, "user2": {...}}`
- [ ] **3.1.3** 实现 `GET /online` 调用：
  ```csharp
  GET http://127.0.0.1:9999/online
  Authorization: {trafficStatsSecret}
  ```
  - 获取各用户的在线连接数
  - 返回格式：`{"user1": 2, "user2": 1}`
- [ ] **3.1.4** 实现异常处理：
  - Hysteria `trafficStats` API 不可达 → 跳过本次采集，记录 Warning 日志，下次重试
  - 连续失败 3 次 → 记录 Error 日志
- [ ] **3.1.5** 创建 `Models/TrafficInfo.cs`
  - `Dictionary<string, UserTrafficEntry> UserTraffic`
  - `Dictionary<string, int> OnlineUsers`
- [ ] **3.1.6** 定时器：每 `TrafficStats.CollectIntervalSeconds`（默认 30s）触发采集
- [ ] **3.1.7** 将采集结果传递给 `StatusReporter`，在下一次心跳时合并发送

#### 3.2 扩展心跳请求

- [ ] **3.2.1** 更新 `StatusReporter`，在心跳请求中填充流量数据：
  ```json
  {
      // ... 系统监控数据 ... (Phase 2 已有)
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
- [ ] **3.2.2** 创建 `Models/DTOs/TrafficEntry.cs`（`tx`, `rx`）

#### 3.3 主服务器流量扣减服务

- [ ] **3.3.1** 创建 `Services/TrafficService.cs`
- [ ] **3.3.2** 实现 **`UpdateUsedTrafficAsync`**（核心方法，带乐观并发重试）：

```csharp
public async Task UpdateUsedTrafficAsync(long userId, long bytesToAdd, int retryCount = 3)
{
    for (int i = 0; i < retryCount; i++)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            user.UsedTrafficBytes += bytesToAdd;
            await _context.SaveChangesAsync();
            return; // 成功
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

- [ ] **3.3.3** 创建 `Repositories/ITrafficRepository.cs`
  - `AddTrafficRecordAsync`
  - `CheckIdempotencyKeyExistsAsync`
  - `GetUserTrafficStatsAsync` (按周期统计)

#### 3.4 幂等键机制

- [ ] **3.4.1** 实现幂等键生成规则：
  ```
  IdempotencyKey = {nodeId}_{username}_{timestamp_rounded_to_interval}
  示例: edge-node-01_user123_2025-01-01T12:00:00Z
  ```
  - `timestamp_rounded_to_interval` = 上报时间戳向下取整到采集间隔（如 30s）
  - 同一节点、同一用户、同一采集周期内的多次上报生成相同幂等键
- [ ] **3.4.2** 在主服务器心跳处理中使用幂等键：
  ```csharp
  var idempotencyKey = $"{nodeId}_{username}_{reportedAt:yyyy-MM-ddTHH:mm:00Z}";
  var exists = await _trafficRepo.CheckIdempotencyKeyExistsAsync(idempotencyKey);
  if (exists)
  {
      _logger.LogDebug("跳过重复流量数据: {Key}", idempotencyKey);
      continue; // 跳过
  }
  ```
- [ ] **3.4.3** **双重保障**：
  - 应用层检查（避免不必要的事务开销）
  - 数据库 UNIQUE 约束（兜底，即使应用层检查遗漏也能阻止重复写入）

#### 3.5 扩展心跳事务：含流量扣减

- [ ] **3.5.1** 扩展 `NodeService.ProcessHeartbeatAsync`，在现有事务中添加流量处理：

```
BEGIN TRANSACTION
    -- Phase 2 原有逻辑：
    INSERT NodeStatus         (系统状态)
    INSERT NodeTraffic        (节点流量汇总)
    UPDATE Nodes.LastHeartbeat
    
    -- Phase 3 新增逻辑：
    FOR EACH user IN userTraffic:
        IF NOT idempotency_duplicate:
            INSERT TrafficRecords   (含 IdempotencyKey)
            UPDATE Users.UsedTrafficBytes  (带乐观并发重试)
            UPDATE Sessions              (流量更新 + 状态)
    
    -- 汇总 NodeTraffic 数据
    UPDATE NodeTraffic SET TotalBytesIn/Out, ActiveUsers
COMMIT
```

- [ ] **3.5.2** 流量方向映射（**必须正确**）：

| Hysteria 字段 | 含义（服务端视角） | 用户视角 | 数据库字段 |
|--------------|--------------------|----------|-----------|
| `tx` | 服务端发送 | **用户下载** | `TrafficRecords.BytesOut` |
| `rx` | 服务端接收 | **用户上传** | `TrafficRecords.BytesIn` |
| `tx + rx` | 服务端总流量 | 用户总流量 | `Users.UsedTrafficBytes += tx + rx` |

- [ ] **3.5.3** `NodeTraffic` 汇总：
  - `TotalBytesOut` = 所有用户 `tx` 之和
  - `TotalBytesIn` = 所有用户 `rx` 之和
  - `ActiveUsers` = `onlineUsers` 的 key 数量

### 第 2 周：超额处理 + 会话管理 + 统计 API + 数据保留

#### 3.6 超额检测与踢用户下线

- [ ] **3.6.1** 在流量扣减后检查：
  ```csharp
  if (user.UsedTrafficBytes >= user.TotalTrafficBytes)
  {
      // 标记超额
      await _kickService.KickUserAsync(username, nodeId);
      _logger.LogWarning("用户 {Username} 流量超额，踢下线", username);
  }
  ```
- [ ] **3.6.2** 创建 `Services/KickService.cs`
  - 向 Edge Agent 发送踢人请求：`POST /api/v1/nodes/{nodeId}/kick-user`
  - Edge Agent 调用 Hysteria `POST /kick`（payload: `{"username": "user123"}`）
- [ ] **3.6.3** 创建 Edge Agent 的踢人端点：
  ```csharp
  // AuthProxy 中新增
  POST /kick-user (主服务器调用，需要 X-Node-Secret 认证)
  → 转发到 Hysteria POST http://127.0.0.1:9999/kick
  ```
- [ ] **3.6.4** 创建管理员踢人 API：`POST /api/v1/admin/kick-user`
  - 建议同时将用户 `isActive` 设为 `false`（防止 Hysteria 客户端自动重连）
- [ ] **3.6.5** 创建 `Models/DTOs/KickUserRequest.cs`

#### 3.7 会话生命周期管理

- [ ] **3.7.1** 实现会话状态机：

```
                    ┌─────────┐
    用户首次在线 →  │ active   │ ← 在线且持续有流量
                    └────┬─────┘
                         │ 30s 无流量数据
                         ↓
                    ┌─────────┐
                    │  idle    │ ← 暂时无流量但未离线
                    └────┬─────┘
                    ┌────┴─────┐
        恢复流量 →  │           │ 连续 3 次不在线
         active     │           │ (90s 无 online 数据)
                    │           ↓
                    │     ┌─────────┐
                    └───→ │ closed   │ → 7 天后清理
                          └─────────┘
```

- [ ] **3.7.2** 在心跳处理中实现会话更新：
  - 用户首次出现在 `onlineUsers` 中 → `INSERT Sessions (status = "active")`
  - 用户仍在 `onlineUsers` 中 → 更新 `Sessions.BytesIn/BytesOut`
  - 用户从 `onlineUsers` 消失 → 设置 `status = "idle"`（非立即关闭）
  - 连续 3 次心跳（90s）不在 `onlineUsers` → `status = "closed"`, `EndedAt = now`
  - `idle` → `active`：重新出现在 `onlineUsers` 中 → 恢复 `status = "active"`
- [ ] **3.7.3** 创建 `Repositories/ISessionRepository.cs`

#### 3.8 用户流量统计 API

- [ ] **3.8.1** 创建 `GET /api/v1/users/{userId}/traffic-stats?period=day|week|month|all`
  - `day`: 最近 24h，按小时聚合
  - `week`: 最近 7 天，按天聚合
  - `month`: 最近 30 天，按天聚合
  - `all`: 所有数据，按天聚合
- [ ] **3.8.2** 返回格式：
  ```json
  {
      "userId": 1,
      "period": "month",
      "totalBytesIn": 10737418240,
      "totalBytesOut": 21474836480,
      "dataPoints": [
          { "date": "2025-01-01", "bytesIn": 1073741824, "bytesOut": 2147483648 },
          { "date": "2025-01-02", "bytesIn": 536870912, "bytesOut": 1073741824 }
      ]
  }
  ```
- [ ] **3.8.3** 在 `TrafficRepository` 中实现聚合查询

#### 3.9 数据保留策略

- [ ] **3.9.1** 创建 `Services/DataRetentionService.cs`（`BackgroundService`）
- [ ] **3.9.2** 实现清理任务（每日执行一次）：

| 表 | 保留期限 | 清理条件 |
|----|----------|----------|
| `TrafficRecords` | 90 天 | `RecordedAt < UtcNow - 90d` |
| `AuthLogs` | 90 天 | `AuthTime < UtcNow - 90d` |
| `NodeStatus` | 30 天 | `ReportedAt < UtcNow - 30d` |
| `NodeTraffic` | 90 天 | `RecordedAt < UtcNow - 90d` |
| `AdminAuditLogs` | 365 天 | `CreatedAt < UtcNow - 365d` |
| `Sessions` (closed) | 7 天 | `Status = "closed" AND EndedAt < UtcNow - 7d` |

- [ ] **3.9.3** 在 `Program.cs` 中注册为 Hosted Service

---

## 4. 应遵守的规范

| 规范来源 | 条款 | Phase 3 适用要点 |
|----------|------|-----------------|
| [流量统计规范](../develop/backend-development-spec.md#7-流量统计规范) | §7.1 | 方案一（Edge Agent 内部采集），**禁止**使用主服务器直连方式 |
| [流量统计规范](../develop/backend-development-spec.md#7-流量统计规范) | §7.2 | 采集流程：定时器 → `/traffic?clear=1` → `/online` → 合并 → 心跳 |
| [流量统计规范](../develop/backend-development-spec.md#7-流量统计规范) | §7.3 | **流量方向映射**：`tx` → `BytesOut`, `rx` → `BytesIn` |
| [流量统计规范](../develop/backend-development-spec.md#7-流量统计规范) | §7.4 | 认证时仅校验不预扣减，超额后踢人 |
| [并发控制规范](../develop/backend-development-spec.md#8-并发控制与数据一致性) | §8.1 | 乐观并发控制 + 最多 3 次重试 |
| [并发控制规范](../develop/backend-development-spec.md#8-并发控制与数据一致性) | §8.2 | 幂等键格式 `{nodeId}_{username}_{timestamp_rounded}` + 数据库 UNIQUE |
| [并发控制规范](../develop/backend-development-spec.md#8-并发控制与数据一致性) | §8.3 | 整个心跳处理在一个事务中 |
| [数据库规范](../develop/backend-development-spec.md#4-数据库规范) | §4.3 | `TrafficRecords` 表全字段 + 幂等键 UNIQUE |
| [数据库规范](../develop/backend-development-spec.md#4-数据库规范) | §4.5 | `Sessions` 表全字段 + 状态机 |
| [数据库规范](../develop/backend-development-spec.md#4-数据库规范) | §4.11 | 数据保留策略（见 §3.9） |
| [异常处理规范](../develop/backend-development-spec.md#9-异常处理与降级规范) | §9.1 | Hysteria trafficStats 不可达 → Agent 跳过 + Warning |
| [异常处理规范](../develop/backend-development-spec.md#9-异常处理与降级规范) | §9.1 | 流量数据上报失败 → 缓存到本地，下次合并补报 |
| [异常处理规范](../develop/backend-development-spec.md#9-异常处理与降级规范) | §9.1 | 数据库锁冲突 → 乐观并发重试，超限后 Error + 500 |
| [日志规范](../develop/backend-development-spec.md#10-日志与监控规范) | §10.1 | 流量采集完成 → Information；跳过采集 → Warning；数据库错误 → Error |
| [测试规范](../develop/backend-development-spec.md#11-测试规范) | §11.2 | `TrafficService` ≥ 90% 覆盖率 |
| [配置规范](../develop/backend-development-spec.md#12-配置规范) | §12.1 | `Traffic.CollectIntervalSeconds: 30`、`Traffic.TrafficDataRetentionDays: 90`、`Traffic.ConcurrentUpdateRetryCount: 3`、`Traffic.IdempotencyEnabled: true` |
| [配置规范](../develop/backend-development-spec.md#12-配置规范) | §12.2 | `TrafficStats.ListenAddress`, `TrafficStats.ListenPort`, `TrafficStats.Secret`, `TrafficStats.CollectIntervalSeconds` |

---

## 5. 应特别注意的事项

### 5.1 ⚠️ 流量方向映射（最容易出生产事故）

这是整个系统最关键的映射关系，一旦搞反，所有用户的流量统计将完全颠倒：

| Hysteria | 视角 | 用户视角 | DB 字段 |
|----------|------|----------|---------|
| `tx` (**T**ransmit out) | 服务端 **发出** | 用户 **下载** | `BytesOut` |
| `rx` (**R**eceive in) | 服务端 **接收** | 用户 **上传** | `BytesIn` |

> 记忆口诀：**服务端的 tx 是用户的下载 → 记入 BytesOut；服务端的 rx 是用户的上传 → 记入 BytesIn**

**验证方法**：在开发环境中，用一个测试用户下载一个大文件，然后检查 `TrafficRecords.BytesOut` 是否增长。若 `BytesIn` 增长而 `BytesOut` 不变，则方向映射错误。

### 5.2 `?clear=1` 的语义

- `GET /traffic?clear=1` 在**返回数据的同时**将 Hysteria 内部计数器清零
- 这意味着每次采集得到的都是**增量数据**，直接累加到 `UsedTrafficBytes` 即可
- ❌ 不能使用 `GET /traffic`（不带 `clear=1`），否则每次获取的是累计值，导致重复计数
- 该操作是 Hysteria 内部原子操作，不存在读后清零的竞争窗口

### 5.3 幂等键设计的精度

- 幂等键中的时间戳必须**向下取整到采集间隔**（如 30s）
- 示例：`2025-01-01T12:00:35Z` → 取整为 `2025-01-01T12:00:00Z`
- 这确保同一采集周期内的重试生成相同幂等键
- 不同采集周期的数据一定有不同的幂等键（因为时间戳不同）

### 5.4 乐观并发控制的重试策略

- 使用 `[Timestamp] byte[] RowVersion` 实现乐观并发
- `DbUpdateConcurrencyException` 被捕获后：
  1. 随机等待 10-50ms（避免两只老虎同时重试）
  2. `ChangeTracker.Clear()` 清空追踪（避免使用旧实体）
  3. 重新加载实体并重试
- 最多重试 3 次（可配置），超过后抛出异常 → 异常中间件返回 500
- ❌ 禁止使用"先读再写"的无锁模式（会丢失并发更新）

### 5.5 事务边界

心跳事务必须是**完整的、原子的**：
- 写入 `NodeStatus`、`NodeTraffic`、`TrafficRecords`、更新 `Users.UsedTrafficBytes`、更新 `Sessions`、更新 `Nodes.LastHeartbeat`
- 任何一个子操作失败 → **整体回滚**
- ❌ 不能部分成功（如流量记录已写入但 `UsedTrafficBytes` 未更新）

### 5.6 会话状态机边界

- `idle` 不等于 `closed`：用户可能只是暂时没有流量，不应立即关闭会话
- 从 `idle` 恢复到 `active`：不创建新会话，复用已有会话记录
- 连续 3 次采集（90s）不在线才关闭：防止因网络抖动导致的误关闭
- `closed` 会话的 `EndedAt` 记录实际离线时间

### 5.7 Hysteria trafficStats API 安全

- `trafficStats.listen` 必须绑定 `127.0.0.1`（仅本地回环）
- ❌ 禁止绑定 `0.0.0.0` 或公网 IP
- `trafficStats.secret` 必须在 hysteria.yaml 和 agent.json 中一致
- Edge Agent 必须以 `Authorization: {secret}` 请求头访问 trafficStats API

### 5.8 踢人后的重连问题

- Hysteria 客户端有内置的自动重连逻辑
- 仅调用 `/kick` 不足以阻止用户重新连接
- **建议策略**：踢人的同时通过 `PUT /api/v1/users/{userId}` 将用户 `isActive` 设为 `false`
- 认证时第 3 步会检查 `isActive`，返回 `account_disabled`

---

## 6. 关键代码模板与示例

### 6.1 流量采集（Edge Agent）

```csharp
// Services/TrafficCollector.cs
public class TrafficCollector : BackgroundService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AgentConfig _config;
    private readonly StatusReporter _reporter;
    private readonly ILogger<TrafficCollector> _logger;

    private TrafficData _lastTrafficData = new();
    private int _consecutiveFailures = 0;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CollectAsync();
                _consecutiveFailures = 0;
            }
            catch (HttpRequestException ex)
            {
                _consecutiveFailures++;
                _logger.LogWarning(ex, "流量采集失败 ({Consecutive}/3 连续失败)", _consecutiveFailures);
                if (_consecutiveFailures >= 3)
                    _logger.LogError("流量采集连续失败 3 次");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "流量采集异常");
            }

            await Task.Delay(
                TimeSpan.FromSeconds(_config.TrafficStats.CollectIntervalSeconds),
                stoppingToken);
        }
    }

    private async Task CollectAsync()
    {
        var client = _httpClientFactory.CreateClient("TrafficStats");
        client.BaseAddress = new Uri($"http://{_config.TrafficStats.ListenAddress}:{_config.TrafficStats.ListenPort}");

        // 1. 获取流量（采集后 Hysteria 内部清零）
        var traffic = await client.GetFromJsonAsync<Dictionary<string, TrafficEntry>>(
            $"/traffic?clear=1&secret={_config.TrafficStats.Secret}");

        // 2. 获取在线用户
        var online = await client.GetFromJsonAsync<Dictionary<string, int>>(
            $"/online?secret={_config.TrafficStats.Secret}");

        // 3. 缓存数据，等待下一次心跳合并
        _lastTrafficData = new TrafficData
        {
            UserTraffic = traffic ?? new(),
            OnlineUsers = online ?? new(),
            CollectedAt = DateTime.UtcNow
        };

        _reporter.SetTrafficData(_lastTrafficData);

        _logger.LogDebug("流量采集完成: {UserCount} 用户, {OnlineCount} 在线",
            traffic?.Count ?? 0, online?.Count ?? 0);
    }
}
```

### 6.2 扩展心跳处理（主服务器，含流量扣减）

```csharp
// Services/NodeService.cs — 扩展后的 ProcessHeartbeatAsync
public async Task ProcessHeartbeatAsync(HeartbeatRequest request)
{
    using var transaction = await _context.Database.BeginTransactionAsync();

    try
    {
        // === Phase 2 原有：系统状态 ===
        _context.NodeStatuses.Add(new NodeStatus
        {
            NodeId = request.NodeId,
            CpuUsagePercent = request.CpuUsagePercent,
            // ... 其他系统指标 ...
            ReportedAt = request.ReportedAt
        });

        // === Phase 3 新增：流量处理 ===
        long totalBytesIn = 0, totalBytesOut = 0;
        int activeUsers = 0;

        foreach (var (username, traffic) in request.UserTraffic ?? new())
        {
            // 2a. 生成幂等键
            var roundedTime = new DateTime(
                request.ReportedAt.Year,
                request.ReportedAt.Month,
                request.ReportedAt.Day,
                request.ReportedAt.Hour,
                request.ReportedAt.Minute,
                request.ReportedAt.Second - (request.ReportedAt.Second % 30), // 向下取整到 30s
                DateTimeKind.Utc);
            var idempotencyKey = $"{request.NodeId}_{username}_{roundedTime:yyyy-MM-ddTHH:mm:ssZ}";

            // 2b. 幂等检查
            var exists = await _context.TrafficRecords
                .AnyAsync(r => r.IdempotencyKey == idempotencyKey);
            if (exists)
            {
                _logger.LogDebug("跳过重复流量: {Key}", idempotencyKey);
                totalBytesIn += traffic.Rx;
                totalBytesOut += traffic.Tx;
                activeUsers++;
                continue;
            }

            // 2c. 获取用户
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == username);
            if (user == null) continue;

            // 2d. 写入流量记录
            _context.TrafficRecords.Add(new TrafficRecord
            {
                UserId = user.Id,
                BytesIn = traffic.Rx,   // Hysteria rx = 用户上传
                BytesOut = traffic.Tx,  // Hysteria tx = 用户下载
                NodeId = request.NodeId,
                IdempotencyKey = idempotencyKey,
                RecordedAt = request.ReportedAt
            });

            // 2e. 更新用户已用流量（乐观并发重试）
            await _trafficService.UpdateUsedTrafficAsync(
                user.Id, traffic.Rx + traffic.Tx);

            // 2f. 检查是否超额
            if (user.UsedTrafficBytes >= user.TotalTrafficBytes)
            {
                _logger.LogWarning("用户 {Username} 流量超额，触发踢下线", username);
                await _kickService.KickUserAsync(username, request.NodeId);
            }

            totalBytesIn += traffic.Rx;
            totalBytesOut += traffic.Tx;
            activeUsers++;
        }

        // === Phase 3 新增：会话更新 ===
        await UpdateSessionsAsync(request.NodeId, request.OnlineUsers ?? new());

        // === Phase 2 原有：节点流量汇总（Phase 3 填充真实值）===
        _context.NodeTraffics.Add(new NodeTraffic
        {
            NodeId = request.NodeId,
            TotalBytesIn = totalBytesIn,
            TotalBytesOut = totalBytesOut,
            ActiveUsers = activeUsers,
            RecordedAt = DateTime.UtcNow
        });

        // === Phase 2 原有：更新心跳时间 ===
        var node = await _nodeRepo.GetByIdAsync(request.NodeId);
        if (node != null)
        {
            node.LastHeartbeat = DateTime.UtcNow;
            node.IsActive = true;
        }

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
    }
    catch (DbUpdateConcurrencyException)
    {
        await transaction.RollbackAsync();
        _logger.LogError("流量扣减并发冲突，事务回滚: NodeId={NodeId}", request.NodeId);
        throw; // 让异常中间件返回 500
    }
    catch
    {
        await transaction.RollbackAsync();
        throw;
    }
}
```

### 6.3 会话状态管理

```csharp
// Services/TrafficService.cs 中的会话更新方法
private async Task UpdateSessionsAsync(string nodeId, Dictionary<string, int> onlineUsers)
{
    // 1. 获取该节点所有 active/idle 会话
    var activeSessions = await _context.Sessions
        .Where(s => s.NodeId == nodeId
            && (s.Status == "active" || s.Status == "idle"))
        .ToListAsync();

    foreach (var session in activeSessions)
    {
        var username = await GetUsernameByIdAsync(session.UserId);
        if (onlineUsers.ContainsKey(username))
        {
            // 用户仍在线 → 激活或恢复
            if (session.Status == "idle")
                _logger.LogInformation("会话恢复: {Username} idle → active", username);
            session.Status = "active";
            session.IdleCount = 0;
        }
        else
        {
            // 用户不在线 → 检查是否应关闭
            if (session.Status == "active")
            {
                session.Status = "idle";
                session.IdleCount = 1;
            }
            else if (session.Status == "idle")
            {
                session.IdleCount++;
                if (session.IdleCount >= 3) // 连续 3 次不在线 → 关闭
                {
                    session.Status = "closed";
                    session.EndedAt = DateTime.UtcNow;
                    _logger.LogInformation("会话关闭: UserId={UserId}", session.UserId);
                }
            }
        }
    }

    // 2. 新建会话：首次出现在在线列表中
    var existingUserIds = activeSessions.Select(s => s.UserId).ToHashSet();
    foreach (var (username, _) in onlineUsers)
    {
        var user = await _userRepo.GetByUsernameAsync(username);
        if (user == null || existingUserIds.Contains(user.Id)) continue;

        _context.Sessions.Add(new Session
        {
            UserId = user.Id,
            NodeId = nodeId,
            SessionKey = $"{nodeId}_{user.Id}_{DateTime.UtcNow:yyyyMMddHHmmss}",
            StartedAt = DateTime.UtcNow,
            Status = "active"
        });
    }
}
```

### 6.4 乐观并发流量更新

```csharp
// Services/TrafficService.cs
public async Task UpdateUsedTrafficAsync(long userId, long bytesToAdd, int retryCount = 3)
{
    for (int i = 0; i < retryCount; i++)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return;

            user.UsedTrafficBytes += bytesToAdd;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // 检查超额
            if (user.UsedTrafficBytes >= user.TotalTrafficBytes && user.TotalTrafficBytes > 0)
            {
                _logger.LogWarning("用户 {UserId} 流量超额: {Used}/{Total}",
                    userId, user.UsedTrafficBytes, user.TotalTrafficBytes);
                // 超额逻辑由调用方处理（见 §6.2 步骤 2f）
            }
            return;
        }
        catch (DbUpdateConcurrencyException)
        {
            if (i == retryCount - 1)
            {
                _logger.LogError("流量扣减轻试 {RetryCount} 次后仍失败: UserId={UserId}",
                    retryCount, userId);
                throw;
            }
            await Task.Delay(Random.Shared.Next(10, 50));
            _context.ChangeTracker.Clear();
        }
    }
}
```

### 6.5 踢用户下线

```csharp
// Services/KickService.cs - 主服务器侧
public async Task KickUserAsync(string username, string nodeId)
{
    // 通过 Edge Agent 调用 Hysteria POST /kick
    var payload = new { username };
    var response = await _httpClient.PostAsJsonAsync(
        $"{_masterUrl}/api/v1/nodes/{nodeId}/kick-user", payload);
    response.EnsureSuccessStatusCode();

    _logger.LogInformation("用户 {Username} 已从节点 {NodeId} 踢下线", username, nodeId);
}

// AuthProxy.cs - Edge Agent 侧新增端点
public async Task<IResult> KickUserAsync(KickUserRequest request)
{
    var client = _httpClientFactory.CreateClient("TrafficStats");
    var response = await client.PostAsJsonAsync(
        $"http://127.0.0.1:9999/kick?secret={_config.TrafficStats.Secret}",
        new { username = request.Username });
    return response.IsSuccessStatusCode ? Results.Ok() : Results.StatusCode(502);
}
```

---

## 7. 阶段完成标准

| 标准 | 验证方式 |
|------|----------|
| Edge Agent 成功调用 Hysteria `GET /traffic?clear=1` | Agent 日志显示采集到的流量数据 |
| 心跳请求包含 `userTraffic` 和 `onlineUsers` | 抓包或查看主服务器日志 |
| 流量记录正确写入 `TrafficRecords` 表 | 查询数据库 → 有记录，`BytesIn`/`BytesOut` 非零 |
| 流量方向映射正确 | 用户下载大文件 → `BytesOut` 增长（非 `BytesIn`） |
| 用户 `UsedTrafficBytes` 正确累加 | `UsedTrafficBytes` = 上次值 + `tx + rx` |
| 重复上报不重复计入（幂等性） | 手动构造重复心跳 → 数据库只有一条记录，日志显示"跳过" |
| 乐观并发控制正常 | 两个线程同时更新同一用户 → 最终值正确（两者之和） |
| 流量超额后用户被踢下线 | 设置用户 `TotalTrafficBytes=1` → 认证后产生流量 → 心跳后 `UsedTrafficBytes >= 1` → 踢人 |
| 会话状态机正常 | `active` → `idle` → `closed` 状态转换正确 |
| `closed` 会话 7 天后自动清理 | 手动设置 `EndedAt = 8 天前` → 等待清理任务执行 |
| 流量统计 API 返回正确聚合数据 | `GET /api/v1/users/{userId}/traffic-stats?period=day` → 正确的按小时数据 |
| 数据清理任务正常运行 | 查看日志 → 每日清理记录 |
| 代码通过 `dotnet build` | 退出码 0 |

---

## 8. 下一阶段 (Phase 4) 交接清单

| 交接项 | 说明 | 参考位置 |
|--------|------|----------|
| 流量采集延迟数据 | Phase 3 已采集 `CollectedAt` 时间戳，Phase 4 可用于 SLO 计算 | [`TrafficCollector.cs`](#) |
| 心跳处理耗时 | Phase 3 的心跳处理是最耗时的操作（含事务 + 并发重试），Phase 4 需纳入性能监控 | [`NodeService.ProcessHeartbeatAsync`](#) |
| Hysteria trafficStats API 不可达的降级 | Phase 3 已记录 Warning + 跳过，Phase 4 可添加"缓存到本地磁盘，恢复后补报"的兜底逻辑 | [`TrafficCollector.cs`](#) |
| 认证缓存 | Phase 1 的 Edge Agent 认证代理尚无本地缓存，Phase 4 需要实现 | [`AuthProxy.cs`](#) |
| 审计日志 | Phase 1 的 `AdminAuditLog` 表尚无写入逻辑，Phase 4 需要补齐 | [`AdminService.cs`](#) |
| SLO 指标 | Phase 4 需要基于 Phase 3 的流量数据采集延迟、心跳处理延迟来计算 SLO 达标率 | [`resilience-monitoring.md`](../architect/resilience-monitoring.md) |
