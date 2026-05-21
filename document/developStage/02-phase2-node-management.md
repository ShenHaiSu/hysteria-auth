# Phase 2: 节点管理

> **阶段**: Phase 2 | **预估工期**: 1-2 周 | **依赖**: Phase 1（核心基础）
>
> **来源文档**: [`document/architect/edge-node-design.md`](../architect/edge-node-design.md) · [`../architect/api-design.md`](../architect/api-design.md) §4.0~4.5 · [`../architect/system-architecture.md`](../architect/system-architecture.md) §3 · [`../architect/security-design.md`](../architect/security-design.md) §6 · [`../architect/deployment.md`](../architect/deployment.md) · [`../develop/backend-development-spec.md`](../develop/backend-development-spec.md) §9.4

---

## 1. 阶段目标与范围

### 1.1 总体目标

在 Phase 1 的基础上，建立完整的**节点生命周期管理体系**：节点预注册 → 注册 → 配置同步 → 心跳监控 → 密钥轮换 → 离线检测。同时为 Edge Agent 补齐系统监控、初始化注册、状态上报、健康检查等模块。

Phase 2 完成后，边缘节点应能从零启动、自动注册到主服务器、定时上报系统状态、并具备健康探活能力。

### 1.2 范围清单

| 序号 | 交付项 | 说明 |
|------|--------|------|
| 2.1 | 节点预注册 API（管理员侧） | `POST /api/v1/admin/nodes/pre-register` |
| 2.2 | 节点注册 API（含令牌和旧版两种方式） | `POST /api/v1/nodes/register-with-token` + `POST /api/v1/nodes/register` |
| 2.3 | 节点配置同步 API | `GET /api/v1/nodes/{nodeId}/config` |
| 2.4 | 节点列表/详情/历史状态 API | `GET /api/v1/nodes*` |
| 2.5 | 节点心跳 API（**仅系统状态部分，不含流量**） | `POST /api/v1/nodes/{nodeId}/heartbeat` |
| 2.6 | Edge Agent 初始化注册模块 (`Initializer`) | 启动时的令牌比对、注册、配置重建 |
| 2.7 | Edge Agent 系统监控模块 (`SystemMonitor`) | CPU/内存/网络采集（`/proc` 文件系统） |
| 2.8 | Edge Agent 状态上报模块 (`StatusReporter`) | 定时心跳 + 系统状态上报 |
| 2.9 | Edge Agent 健康检查端点 | `GET /health` |
| 2.10 | 节点密钥轮换机制 | `SecretVersion` 多版本并存 |
| 2.11 | 节点离线检测 | 心跳超时 90s → `IsActive = false` |
| 2.12 | 后台定时任务：超时检测 | `BackgroundService` 定期扫描心跳超时节点 |

---

## 2. 阶段启动前置检查

> Phase 2 启动前，必须对 Phase 1 的以下关键产物进行审查和验证。

| # | 检查项 | 验证内容 | 通过标准 |
|----|--------|----------|----------|
| P1.1 | JWT 中间件工作正常 | 带有效 Token 访问 `/api/v1/users` | 200 + 数据返回 |
| P1.2 | JWT 中间件拒绝无效/过期 Token | 带无效 Token 访问 | 401 + `token_invalid` / `token_expired` |
| P1.3 | 节点密钥中间件就绪 | 带有效 `X-Node-Secret` 访问（可临时用 curl） | 中间件能正确解析并注入 NodeId |
| P1.4 | `Node` Entity 完整 | 检查 `SecretKey`、`SecretVersion`、`ProvisionToken`、`ProvisionStatus` 字段存在 | EF Core Migration 包含这些列 |
| P1.5 | `NodeStatus` Entity 完整 | 检查全部 11 个字段 | Migration 包含全字段 |
| P1.6 | `NodeTraffic` Entity 完整 | 检查 5 个字段 | Migration 包含全字段 |
| P1.7 | `AppException` 体系可正常抛出和捕获 | 手动抛一个 `NotFoundException` → 全局异常中间件捕获 | 返回 404 + `not_found` |
| P1.8 | Edge Agent 的 `AuthProxy` 可正常启动 | 启动 Agent 程序 → 监听 8080 | `curl http://127.0.0.1:8080/auth` 返回 405/400（验证端口监听） |
| P1.9 | 数据库种子数据存在 | 查询 `Admins` 表 | 存在至少一条 `super_admin` 记录 |
| P1.10 | ✅ 阅读 [`backend-development-spec.md`](../develop/backend-development-spec.md) §4.6~4.8（节点表、节点状态表、节点流量统计表） | — | — |
| P1.11 | ✅ 阅读 [`backend-development-spec.md`](../develop/backend-development-spec.md) §6.4（密钥管理） | — | — |

---

## 3. 具体任务清单

### 第 1 周：节点注册 + 配置同步 + 初始化模块 + 系统监控

#### 3.1 节点预注册（管理员侧）

- [ ] **2.1.1** 创建 `Services/NodeService.cs` 中的 `PreRegisterNodeAsync` 方法
  - 生成 UUID `nodeId`
  - 生成 128-bit 随机 `ProvisionToken`（32 hex）
  - 设置 `ProvisionStatus = "pending"`，`ExpiresAt = UtcNow + 7 天`
  - 插入 `Nodes` 表
  - 返回 `provisionToken` + `masterServerUrl` + `expiresAt` + `startupCommand`
- [ ] **2.1.2** 创建 `Controllers/NodesController.cs` 中的 `PreRegisterNode` 动作
  - 路由：`POST /api/v1/admin/nodes/pre-register`
  - 认证：`Authorization: Bearer {admin_token}`
  - 请求体：`name`, `location`, `port`, `trafficStatsPort`
  - 响应：201 + 令牌信息
- [ ] **2.1.3** 创建 `Models/DTOs/PreRegisterNodeRequest.cs`、`PreRegisterNodeResponse.cs`

#### 3.2 节点注册 API

- [ ] **2.2.1** 创建 `POST /api/v1/nodes/register-with-token`（令牌注册，推荐方式）
  - 请求体：`provisionToken`, `ipAddress`, `agentVersion`（`nodeId` 可选，不提供则由服务端生成）
  - 验证：令牌存在 + 未过期 + `ProvisionStatus == "pending"`
  - 生成 `nodeSecret`（256-bit, 64 hex）
  - 生成 `trafficStatsSecret`（128-bit, 16 hex）
  - 更新 `ProvisionToken = null`，`ProvisionStatus = "provisioned"`，`SecretKey`、`SecretVersion = 1`
  - 返回：`nodeId`, `nodeSecret`, `trafficStatsSecret`, `config`
- [ ] **2.2.2** 创建 `POST /api/v1/nodes/register`（旧版注册，保留兼容）
  - 请求体：`nodeId`, `name`, `ipAddress`, `port`, `location`, `trafficStatsPort`, `trafficStatsSecret`
  - 若 `nodeId` 已存在 → 更新节点信息
  - 若 `nodeId` 不存在 → 插入新记录
  - 认证：`X-Node-Secret`（首次注册时可以为新节点创建初始密钥）
- [ ] **2.2.3** 创建 `Repositories/INodeRepository.cs`
  - `GetByIdAsync`, `GetByProvisionTokenAsync`, `GetAllAsync`, `AddAsync`, `UpdateAsync`
  - `GetActiveNodesAsync`

#### 3.3 节点配置同步 API

- [ ] **2.3.1** 创建 `GET /api/v1/nodes/{nodeId}/config`
  - 认证：`X-Node-Secret`
  - 返回：`nodeId`, `nodeSecret` (当前有效版本), `isActive`, `config` (含 `authProxyPort`, `healthCheckPort`, `trafficStatsPort`, `collectIntervalSeconds`)
- [ ] **2.3.2** 支持 `SecretVersion` 多版本密钥：返回当前最高版本的 `SecretKey`

#### 3.4 节点列表/详情/历史状态 API

- [ ] **2.4.1** `GET /api/v1/nodes?isActive=true` — 节点列表（管理员认证）
- [ ] **2.4.2** `GET /api/v1/nodes/{nodeId}` — 节点详情（管理员认证）
- [ ] **2.4.3** `GET /api/v1/nodes/{nodeId}/status-history?hours=24` — 节点历史状态（管理员认证）
- [ ] **2.4.4** 创建 `Repositories/INodeStatusRepository.cs`

#### 3.5 Edge Agent 初始化注册模块

- [ ] **2.5.1** 创建 `src/HysteriaAuth.Agent/Services/Initializer.cs`
- [ ] **2.5.2** 实现启动时的**令牌比对逻辑**：

| 场景 | 本地 ProvisionToken | 启动参数 --provision-token | 行为 |
|------|---------------------|---------------------------|------|
| 全新部署 | 不存在 | 存在 | 使用启动参数令牌，向主服务器注册，生成新配置 |
| 令牌变更 | `token_A` | `token_B`（不同） | 删除本地配置 → 使用新令牌重新注册 |
| 令牌一致 | `token_A` | `token_A`（相同） | 使用已有配置正常启动 |
| 已注册节点 | 不存在（已清零） | 不存在 | 使用已有 `nodeId` + `nodeSecret` 正常启动 |
| 令牌过期 | 存在但已过期 | 存在但已过期 | 注册失败 → 重试（指数退避，最多 5 次）→ `exit(1)` |

- [ ] **2.5.3** 实现**注册重试策略**：
  - 初始间隔 2s，最大间隔 60s（指数退避）
  - 最多重试 5 次
  - 全部失败后 `exit(1)`，由 systemd `Restart=always` 重启
- [ ] **2.5.4** 实现**配置重建流程**：
  1. 备份旧 `agent.json` → `agent.json.bak.{timestamp}`
  2. 删除 `agent.json`
  3. 调用 `POST /api/v1/nodes/register-with-token`
  4. 使用返回数据生成新 `agent.json`
  5. 持久化到磁盘
- [ ] **2.5.5** 初始化序列（`edge-node-design.md` §2.1）：
  1. 加载配置 → 2. 验证配置 → 3. 令牌比对 → 4. 注册/配置同步
  5. 启动健康检查 → 6. 启动系统监控 → 7. 启动认证代理 → 8. 启动心跳上报
- [ ] **2.5.6** 发送首次心跳（含空流量数据）

#### 3.6 Edge Agent 系统监控模块

- [ ] **2.6.1** 创建 `src/HysteriaAuth.Agent/Services/SystemMonitor.cs`
- [ ] **2.6.2** 实现 CPU 使用率采集（`/proc/stat`）：
  ```csharp
  // 两次采样（间隔 1s），计算差值
  // cpuUsage = (1 - idleDiff / totalDiff) * 100
  ```
- [ ] **2.6.3** 实现内存使用率采集（`/proc/meminfo`）：
  ```csharp
  // MemTotal, MemAvailable
  // used = Total - Available
  // usagePercent = used / Total * 100
  ```
- [ ] **2.6.4** 实现网络流量采集（`/proc/net/dev`）：
  ```csharp
  // 过滤 lo 接口，汇总所有物理接口的 rx/tx 字节数
  // 网络速率 = 两次采样的差值 / 间隔时间
  ```
- [ ] **2.6.5** 创建 `Models/SystemMetrics.cs`
  - `CpuUsagePercent`, `MemoryUsagePercent`, `MemoryUsedMb`, `MemoryTotalMb`
  - `NetworkInBytes`, `NetworkOutBytes`, `NetworkInMbps`, `NetworkOutMbps`
  - `ActiveConnections`
- [ ] **2.6.6** 定时器：每 `Monitor.IntervalSeconds`（默认 10s）触发采集

### 第 2 周：心跳上报 + 健康检查 + 密钥轮换 + 离线检测

#### 3.7 心跳与状态上报

- [ ] **2.7.1** 创建 `src/HysteriaAuth.Agent/Services/StatusReporter.cs`
- [ ] **2.7.2** 实现定时上报：将系统监控数据合并发送
  - 每 `Reporter.IntervalSeconds`（默认 30s）触发
  - 请求：`POST /api/v1/nodes/{nodeId}/heartbeat` + `X-Node-Secret`
  - **Phase 2 只上报系统状态**：`cpuUsagePercent`, `memoryUsagePercent`, ... , `reportedAt`
  - `userTraffic` 和 `onlineUsers` 字段 Phase 2 可为空对象（Phase 3 填充）
- [ ] **2.7.3** 实现上报重试：失败后重试 3 次（可配置），间隔 5s
- [ ] **2.7.4** 创建主服务器侧心跳处理：`Services/NodeService.cs` 中的 `ProcessHeartbeatAsync`
  - 写入 `NodeStatus` 记录
  - 更新 `Nodes.LastHeartbeat` + `Nodes.IsActive = true`（如果之前为 false）
  - 写入 `NodeTraffic` 汇总记录（Phase 2 中 `TotalBytesIn/Out` 和 `ActiveUsers` 可为 0）
  - 整个处理在**一个数据库事务**中完成
- [ ] **2.7.5** 创建 `Models/DTOs/HeartbeatRequest.cs`

#### 3.8 健康检查端点

- [ ] **2.8.1** 主服务器健康检查 `GET /health`（Phase 1 可能已有骨架，Phase 2 完善）
  - 返回：`status` (healthy/degraded/unhealthy), `timestamp`, `version`, `uptime`, `checks` (database, disk_space)
- [ ] **2.8.2** Edge Agent 健康检查 `GET /health`（监听 `127.0.0.1:8081`）
  - 返回：`status`, `nodeId`, `timestamp`, `checks` (hysteria_reachable, master_reachable, traffic_collector: "pending" (Phase 3 变为 "running"), system_monitor: "running")
- [ ] **2.8.3** 创建 `Middleware/HealthCheckMiddleware.cs`（Agent 侧）

#### 3.9 节点密钥轮换

- [ ] **2.9.1** 创建 `POST /api/v1/admin/nodes/{nodeId}/rotate-secret`（管理员认证）
  - 生成新 `SecretKey`（256-bit 随机）
  - `SecretVersion += 1`
  - 数据库保留新旧两版本（通过 `SecretVersion` 区分）
- [ ] **2.9.2** 实现**多版本密钥验证**：
  - `NodeAuthMiddleware` 在验证 `X-Node-Secret` 时，查询该节点的**所有版本** `SecretKey`
  - 任意一个版本匹配即通过（支持零停机切换）
- [ ] **2.9.3** 下次心跳响应中携带新密钥（Phase 2 可简单实现为心跳响应头或独立字段）
- [ ] **2.9.4** Edge Agent 收到新密钥后：
  - 更新本地 `agent.json`
  - 后续请求使用新密钥
  - 旧密钥在确认所有 Agent 升级后由管理员手动淘汰（或自动过期）

#### 3.10 节点离线检测

- [ ] **2.10.1** 创建 `Services/NodeHealthCheckService.cs`（`BackgroundService`）
  - 每 `Node.HeartbeatTimeoutSeconds / 3`（默认 30s）扫描一次
  - 查询所有 `IsActive = true` 且 `LastHeartbeat < UtcNow - 90s` 的节点
  - 将其 `IsActive` 设为 `false`
  - 记录 Warning 日志
- [ ] **2.10.2** 节点恢复：收到该节点新心跳时，`IsActive` 自动恢复为 `true`（已在 §2.7.4 实现）
- [ ] **2.10.3** 离线节点拒绝认证：`AuthService` 需检查目标节点 `IsActive` 状态
  - 若 `IsActive = false` → 返回 `node_not_allowed`

---

## 4. 应遵守的规范

| 规范来源 | 条款 | Phase 2 适用要点 |
|----------|------|-----------------|
| [数据库规范](../develop/backend-development-spec.md#4-数据库规范) | §4.6 | `Nodes` 表：`SecretKey` AES-256-GCM 加密存储、`SecretVersion` 整数、`ProvisionStatus` 枚举 |
| [数据库规范](../develop/backend-development-spec.md#4-数据库规范) | §4.7 | `NodeStatus` 表：全字段，`ReportedAt` UTC |
| [数据库规范](../develop/backend-development-spec.md#4-数据库规范) | §4.8 | `NodeTraffic` 表：全字段，`RecordedAt` UTC |
| [安全规范](../develop/backend-development-spec.md#6-安全规范) | §6.4 | 节点 SecretKey 生成：256-bit (64 hex)，AES-256-GCM 加密存储 |
| [安全规范](../develop/backend-development-spec.md#6-安全规范) | §6.4 | 密钥轮换：多版本并行，零停机 |
| [认证流程规范](../develop/backend-development-spec.md#5-认证流程规范) | §5.3 | 认证检查第 6 步：节点离线也属于 `node_not_allowed` |
| [异常处理规范](../develop/backend-development-spec.md#9-异常处理与降级规范) | §9.1 | 心跳超时 90s → `IsActive = false` |
| [异常处理规范](../develop/backend-development-spec.md#9-异常处理与降级规范) | §9.3 | 节点恢复心跳 → `IsActive = true` |
| [异常处理规范](../develop/backend-development-spec.md#9-异常处理与降级规范) | §9.4 | 注册重试策略：初始 2s / 最大 60s / 最多 5 次 / 失败后 exit(1) |
| [日志规范](../develop/backend-development-spec.md#10-日志与监控规范) | §10.1 | 节点注册成功 → Information；心跳超时 → Warning；密钥无效 → Error |
| [配置规范](../develop/backend-development-spec.md#12-配置规范) | §12.1 | `appsettings.json` 中 `Node.HeartbeatTimeoutSeconds: 90`、`Node.KeyRotationEnabled: true` |
| [配置规范](../develop/backend-development-spec.md#12-配置规范) | §12.2 | `agent.json` 扩展 `Init`、`Monitor`、`Reporter`、`HealthCheck` 配置节 |

---

## 5. 应特别注意的事项

### 5.1 密钥存储安全

- `Nodes.SecretKey` 和 `Nodes.TrafficStatsSecret` **必须在数据库中使用 AES-256-GCM 加密存储**
- 加密密钥从 `appsettings.json` 或环境变量中读取（`Encryption:MasterKey`）
- ❌ 禁止明文存储节点密钥
- 内存中解密使用，日志中不输出

### 5.2 预注册令牌安全

- `ProvisionToken` 只能使用一次，注册成功后立即清零
- 令牌有 7 天过期时间
- 令牌在数据库中使用 UNIQUE 索引约束，防止碰撞
- `ProvisionStatus` 状态机：`pending` → `provisioned` 或 `pending` → `revoked`

### 5.3 心跳事务原子性

Phase 2 的心跳处理虽然不含流量数据，但写入 `NodeStatus` + `NodeTraffic` + 更新 `Nodes.LastHeartbeat` 必须在同一事务中：

```csharp
using var transaction = await _context.Database.BeginTransactionAsync();
// 1. INSERT NodeStatus
// 2. INSERT NodeTraffic（汇总，值可为 0）
// 3. UPDATE Nodes.LastHeartbeat + IsActive
await transaction.CommitAsync();
```

该事务结构是 Phase 3 的基础，Phase 3 会在其中添加流量扣减和幂等检查。

### 5.4 CPU 采集的间隔问题

- CPU 使用率需要通过两次 `ReadProcStat()` 的差值计算
- 两次读取之间需 `Thread.Sleep(1000)`，这意味着一次 CPU 采集需要 1 秒
- 建议 CPU 采集在独立的异步任务中执行，不阻塞其他指标采集
- 如果采集间隔太短（如 10s 内 CPU 采集可能占用 1s），需评估对 Agent 整体响应的影响

### 5.5 Edge Agent 初始化顺序

启动顺序不可变更：

```
1. 加载配置 → 2. 令牌比对/注册 → 3. 启动所有模块 → 4. 发送首次心跳
```

任何模块（监控、上报、认证代理）的启动依赖第 2 步成功完成。如果注册或配置同步失败，Agent 不应启动后续模块。

### 5.6 节点离线与认证的联动

- 节点离线（`IsActive = false`）时，该节点发起的**所有认证请求**必须被拒绝
- 检查点：在 [`AuthService.AuthenticateAsync`](#) 的第 6 步（节点允许检查）中，除了 `AllowedNodes` 白名单，还需检查 `node.IsActive`
- 边缘节点离线 90 秒后，连接到该节点的用户新连接全部不可用（已有连接不受影响）

### 5.7 健康检查的语义

- `GET /health` 不应触发重负载操作（如数据库全表扫描）
- 数据库检查：简单的 `SELECT 1` 或 `CanConnectAsync()`
- 磁盘空间检查：检查可用空间是否低于阈值（如 5%）
- Agent 健康检查中 `master_reachable` 应是轻量探活（如 TCP ping 或 HEAD 请求）

---

## 6. 关键代码模板与示例

### 6.1 预注册令牌生成

```csharp
// Services/NodeService.cs
public async Task<PreRegisterNodeResponse> PreRegisterNodeAsync(PreRegisterNodeRequest request)
{
    var nodeId = Guid.NewGuid().ToString("N")[..12]; // 12 位 hex
    var provisionToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)); // 128-bit

    var node = new Node
    {
        Id = nodeId,
        Name = request.Name,
        Port = request.Port,
        Location = request.Location,
        TrafficStatsPort = request.TrafficStatsPort,
        ProvisionToken = provisionToken,
        ProvisionStatus = "pending",
        IsActive = false, // 注册完成后才激活
        CreatedAt = DateTime.UtcNow
    };

    await _nodeRepo.AddAsync(node);
    await _context.SaveChangesAsync();

    return new PreRegisterNodeResponse
    {
        ProvisionToken = provisionToken,
        MasterServerUrl = _config.MasterServerUrl,
        ExpiresAt = DateTime.UtcNow.AddDays(7),
        StartupCommand = $"./edge-agent --provision-token={provisionToken} --master-url={_config.MasterServerUrl}"
    };
}
```

### 6.2 令牌注册（主服务器侧）

```csharp
// POST /api/v1/nodes/register-with-token 的处理逻辑
public async Task<RegisterWithTokenResponse> RegisterWithTokenAsync(RegisterWithTokenRequest request)
{
    // 1. 查找令牌
    var node = await _nodeRepo.GetByProvisionTokenAsync(request.ProvisionToken);
    if (node == null)
        throw new AppException("not_found", "预注册令牌不存在或已使用", 404);

    // 2. 检查状态
    if (node.ProvisionStatus != "pending")
        throw new AppException("conflict", "该令牌已使用", 409);

    // 3. 生成密钥
    var nodeSecret = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)); // 256-bit
    var trafficStatsSecret = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)); // 128-bit

    // 4. 更新节点
    node.Name = request.Name ?? node.Name;
    node.IpAddress = request.IpAddress;
    node.SecretKey = _aesEncryption.Encrypt(nodeSecret); // AES-256-GCM 加密
    node.SecretVersion = 1;
    node.TrafficStatsSecret = _aesEncryption.Encrypt(trafficStatsSecret);
    node.ProvisionToken = null; // 令牌清零
    node.ProvisionStatus = "provisioned";
    node.IsActive = true;

    await _context.SaveChangesAsync();

    return new RegisterWithTokenResponse
    {
        NodeId = node.Id,
        NodeSecret = nodeSecret, // 返回明文（仅此一次）
        TrafficStatsSecret = trafficStatsSecret,
        Config = new NodeConfig
        {
            AuthProxyPort = 8080,
            HealthCheckPort = 8081,
            TrafficStatsPort = node.TrafficStatsPort ?? 9999,
            CollectIntervalSeconds = 30
        }
    };
}
```

### 6.3 Edge Agent 初始化器

```csharp
// Services/Initializer.cs
public async Task<bool> InitializeAsync(string[] args)
{
    // 1. 加载本地配置
    var localConfig = await LoadConfigAsync();
    var cliProvisionToken = ParseCliArg(args, "--provision-token");
    var localProvisionToken = localConfig?.ProvisionToken;

    // 2. 令牌比对
    var shouldReregister = cliProvisionToken != null
        && cliProvisionToken != localProvisionToken;

    if (shouldReregister)
    {
        _logger.LogInformation("检测到令牌变更，重建配置");
        BackupConfig();
        DeleteConfig();
        return await RegisterWithTokenAsync(cliProvisionToken);
    }

    if (localProvisionToken != null)
    {
        _logger.LogInformation("使用已有令牌注册");
        return await RegisterWithTokenAsync(localProvisionToken);
    }

    if (localConfig?.NodeId != null && localConfig?.NodeSecret != null)
    {
        _logger.LogInformation("使用已有身份启动: {NodeId}", localConfig.NodeId);
        _config = localConfig;
        return true;
    }

    _logger.LogError("无有效配置，无法启动");
    return false;
}

private async Task<bool> RegisterWithTokenAsync(string token)
{
    for (int attempt = 0; attempt < MaxRetries; attempt++)
    {
        try
        {
            var response = await _masterClient.PostAsync<RegisterWithTokenResponse>(
                "/api/v1/nodes/register-with-token",
                new { provisionToken = token, ipAddress = GetLocalIp(), agentVersion = "1.0.0" }
            );

            // 生成并持久化新配置
            var newConfig = new AgentConfig
            {
                NodeId = response.NodeId,
                NodeSecret = response.NodeSecret,
                MasterServerUrl = _masterUrl,
                AgentVersion = "1.0.0",
                TrafficStats = new() { Secret = response.TrafficStatsSecret, /* ... */ },
                // ... 其他默认配置
            };
            await SaveConfigAsync(newConfig);
            _config = newConfig;
            _logger.LogInformation("节点注册成功: {NodeId}", response.NodeId);
            return true;
        }
        catch (Exception ex)
        {
            if (attempt == MaxRetries - 1)
            {
                _logger.LogCritical(ex, "注册失败，已达最大重试次数");
                return false;
            }
            var delay = Math.Min(InitialRetrySeconds * Math.Pow(2, attempt), MaxRetrySeconds);
            _logger.LogWarning("注册失败，{Delay}s 后重试 ({Attempt}/{Max})", delay, attempt + 1, MaxRetries);
            await Task.Delay(TimeSpan.FromSeconds(delay));
        }
    }
    return false;
}
```

### 6.4 系统监控（CPU 采集）

```csharp
// Services/SystemMonitor.cs
public async Task<SystemMetrics> CollectAsync()
{
    var cpuUsage = await GetCpuUsageAsync();
    var memory = GetMemoryInfo();
    var network = GetNetworkInfo();

    return new SystemMetrics
    {
        CpuUsagePercent = Math.Round(cpuUsage, 2),
        MemoryUsagePercent = Math.Round(memory.UsagePercent, 2),
        MemoryUsedMb = memory.UsedMb,
        MemoryTotalMb = memory.TotalMb,
        NetworkInBytes = network.TotalInBytes,
        NetworkOutBytes = network.TotalOutBytes,
        NetworkInMbps = Math.Round(network.InMbps, 2),
        NetworkOutMbps = Math.Round(network.OutMbps, 2),
        ActiveConnections = GetActiveConnections(),
        CollectedAt = DateTime.UtcNow
    };
}

private async Task<double> GetCpuUsageAsync()
{
    var stat1 = ReadProcStat();
    await Task.Delay(1000); // 两次采样间隔
    var stat2 = ReadProcStat();

    var totalDiff = stat2.Total - stat1.Total;
    var idleDiff = stat2.Idle - stat1.Idle;
    return (1.0 - (double)idleDiff / totalDiff) * 100;
}
```

### 6.5 心跳处理（主服务器侧，Phase 2 仅系统状态）

```csharp
// Services/NodeService.cs
public async Task ProcessHeartbeatAsync(HeartbeatRequest request)
{
    using var transaction = await _context.Database.BeginTransactionAsync();

    try
    {
        // 1. 写入节点系统状态
        _context.NodeStatuses.Add(new NodeStatus
        {
            NodeId = request.NodeId,
            CpuUsagePercent = request.CpuUsagePercent,
            MemoryUsagePercent = request.MemoryUsagePercent,
            MemoryUsedMb = request.MemoryUsedMb,
            MemoryTotalMb = request.MemoryTotalMb,
            NetworkInBytes = request.NetworkInBytes,
            NetworkOutBytes = request.NetworkOutBytes,
            NetworkInMbps = request.NetworkInMbps,
            NetworkOutMbps = request.NetworkOutMbps,
            ActiveConnections = request.ActiveConnections,
            ReportedAt = request.ReportedAt
        });

        // 2. 写入节点流量汇总（Phase 2 可为 0）
        _context.NodeTraffics.Add(new NodeTraffic
        {
            NodeId = request.NodeId,
            TotalBytesIn = 0,
            TotalBytesOut = 0,
            ActiveUsers = 0,
            RecordedAt = DateTime.UtcNow
        });

        // 3. 更新节点心跳时间和激活状态
        var node = await _nodeRepo.GetByIdAsync(request.NodeId);
        if (node != null)
        {
            node.LastHeartbeat = DateTime.UtcNow;
            node.IsActive = true; // 恢复激活（如果之前离线）
        }

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
    }
    catch
    {
        await transaction.RollbackAsync();
        throw;
    }
}
```

### 6.6 节点离线检测（BackgroundService）

```csharp
// Services/NodeHealthCheckService.cs
public class NodeHealthCheckService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<NodeHealthCheckService> _logger;
    private readonly int _heartbeatTimeoutSeconds;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var threshold = DateTime.UtcNow.AddSeconds(-_heartbeatTimeoutSeconds);
                var offlineNodes = await context.Nodes
                    .Where(n => n.IsActive && n.LastHeartbeat < threshold)
                    .ToListAsync(stoppingToken);

                foreach (var node in offlineNodes)
                {
                    node.IsActive = false;
                    _logger.LogWarning("节点 {NodeId} 心跳超时（{Seconds}s），标记为离线",
                        node.Id, _heartbeatTimeoutSeconds);
                }

                if (offlineNodes.Count > 0)
                    await context.SaveChangesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "节点健康检查异常");
            }

            // 每 30s（HeartbeatTimeoutSeconds/3）检查一次
            await Task.Delay(_heartbeatTimeoutSeconds * 1000 / 3, stoppingToken);
        }
    }
}
```

---

## 7. 阶段完成标准

| 标准 | 验证方式 |
|------|----------|
| 管理员可预注册节点并获取令牌 | `POST /api/v1/admin/nodes/pre-register` → 201 + provisionToken |
| Edge Agent 可使用令牌注册并获取配置 | 启动 Agent（带 `--provision-token`）→ `agent.json` 自动生成 |
| 令牌只能使用一次 | 重复注册 → 409 `conflict` |
| Edge Agent 系统监控正常采集 | 查看 Agent 日志 → 每 10s 输出 CPU/Memory/Network 指标 |
| 心跳上报正常 | 主服务器日志显示心跳处理，`NodeStatus`/`NodeTraffic` 表有新记录 |
| 节点离线自动标记 | 停掉 Edge Agent → 90s 后 `Nodes.IsActive = false` |
| 节点恢复自动上线 | 重启 Edge Agent → 心跳恢复 → `Nodes.IsActive = true` |
| 离线节点拒绝认证 | 用离线节点的 `nodeId` 调认证 API → `node_not_allowed` |
| 新的认证请求走完整链路 | Hysteria Client → Edge Agent → Master Server → 认证成功响应 |
| 健康检查端点可访问 | `GET /health` (Master) → 200 healthy; `GET /health` (Agent) → 200 healthy |
| 密钥轮换后旧密钥仍可用 | 轮换后，旧 `X-Node-Secret` 仍能通过认证（多版本并存） |
| 代码通过 `dotnet build` | 退出码 0 |

---

## 8. 下一阶段 (Phase 3) 交接清单

| 交接项 | 说明 | 参考位置 |
|--------|------|----------|
| 心跳处理的事务结构 | Phase 2 已实现事务包裹 `NodeStatus` + `NodeTraffic` + `Nodes.LastHeartbeat`，Phase 3 在其中添加 `TrafficRecords` + `Users.UsedTrafficBytes` | [`NodeService.ProcessHeartbeatAsync`](#) |
| 心跳请求体结构 | `HeartbeatRequest` 已预留 `userTraffic` (object) 和 `onlineUsers` (object) 字段，Phase 3 填充 | [`HeartbeatRequest.cs`](#) |
| Edge Agent 定时器体系 | Agent 已有 `SystemMonitor`（10s）、`StatusReporter`（30s），Phase 3 添加 `TrafficCollector`（30s） | [`Program.cs` (Agent)](#) |
| 上报重试机制 | `StatusReporter` 已实现 3 次重试，Phase 3 直接复用 | [`StatusReporter.cs`](#) |
| `NodeTraffic` 表当前填写 0 | Phase 3 需要填充真实的 `TotalBytesIn/Out` 和 `ActiveUsers` | [`NodeService.cs`](#) |
| `TrafficRecord.IdempotencyKey` UNIQUE 约束已存在 | Phase 3 直接使用 | EF Core Migration |
| `Session` 表已创建但未使用 | Phase 3 需要实现会话生命周期管理 | [`Session.cs`](#) |
| Hysteria `trafficStats` API 端点 | Phase 3 需要 Edge Agent 调用 `GET /traffic?clear=1` + `GET /online` | [`../hysteria/hysteria-traffic-stats-api.md`](../hysteria/hysteria-traffic-stats-api.md) |
