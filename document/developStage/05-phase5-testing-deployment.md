# Phase 5: 测试与部署

> **阶段**: Phase 5 | **预估工期**: 1 周 | **依赖**: Phase 4（完善功能）
>
> **来源文档**: [`document/architect/resilience-monitoring.md`](../architect/resilience-monitoring.md) §3 · [`../architect/deployment.md`](../architect/deployment.md) · [`../architect/appendix.md`](../architect/appendix.md) · [`../develop/backend-development-spec.md`](../develop/backend-development-spec.md) §11, §13

---

## 1. 阶段目标与范围

### 1.1 总体目标

对前四个阶段完成的所有功能进行**系统性测试**，确保代码质量和功能正确性，然后编写**部署脚本和文档**，使系统可以交付到生产环境。

Phase 5 是项目收尾阶段——不改功能，只做验证、修复和交付。

### 1.2 范围清单

| 序号 | 交付项 | 说明 |
|------|--------|------|
| 5.1 | 单元测试（覆盖全部核心 Service） | xUnit + Moq + FluentAssertions |
| 5.2 | 集成测试（API 端点 + 数据库） | xUnit + Testcontainers / EF Core InMemory |
| 5.3 | E2E 测试（完整认证链路） | xUnit + 本地 Hysteria 实例 |
| 5.4 | 部署脚本 | `deploy-master.sh` + `deploy-agent.sh` + `deploy-agent-provisioned.sh` |
| 5.5 | systemd 服务文件 | `hysteria-auth-master.service` + `hysteria-auth-agent.service` |
| 5.6 | Nginx 反向代理配置 | `nginx-master.conf` |
| 5.7 | Docker 支持（可选） | `Dockerfile.master` + `Dockerfile.agent` + `docker-compose.yml` |
| 5.8 | 文档完善 | README、快速开始指南、API 文档链接 |

---

## 2. 阶段启动前置检查

> Phase 5 启动前，必须对 Phase 4 的以下关键产物进行审查和验证。

| # | 检查项 | 验证内容 | 通过标准 |
|----|--------|----------|----------|
| P4.1 | 认证缓存工作 | Agent 日志中有缓存命中 | Debug log 显示 "缓存命中" |
| P4.2 | 审计日志写入 | 创建用户 → `AdminAuditLogs` 有新记录 | `SELECT * FROM AdminAuditLogs` 有数据 |
| P4.3 | 速率限制生效 | 快速连续请求 > 100 次 | 429 错误响应 |
| P4.4 | CORS 头正确 | 浏览器跨域请求管理 API | `Access-Control-Allow-Origin` 返回正确 |
| P4.5 | 备份文件存在 | 检查备份目录 | 有 `.db` 文件 |
| P4.6 | Dashboard 返回数据 | `GET /api/v1/admin/dashboard` | 200 + 非零数据 |
| P4.7 | 请求体大小限制 | 发送 > 1MB 数据 | 413 |
| P4.8 | 全部功能清单确认 | 对照 `overview.md` §1.3 | 10 项核心功能全部可运行 |
| P4.9 | ✅ 代码无编译错误 | `dotnet build` | 退出码 0 |
| P4.10 | ✅ 阅读 [`backend-development-spec.md`](../develop/backend-development-spec.md) §11（测试规范）全部 | — | — |
| P4.11 | ✅ 阅读 [`backend-development-spec.md`](../develop/backend-development-spec.md) §13（部署规范）全部 | — | — |

---

## 3. 具体任务清单

### 第 1~3 天：单元测试

#### 3.1 测试项目初始化

- [ ] **5.1.1** 创建测试项目 `tests/HysteriaAuth.Tests/HysteriaAuth.Tests.csproj`
- [ ] **5.1.2** 安装 NuGet 包：
  - `xunit` + `xunit.runner.visualstudio`
  - `Moq`（Mock 框架）
  - `FluentAssertions`（断言库）
  - `Microsoft.EntityFrameworkCore.InMemory`（集成测试用）
  - `Microsoft.NET.Test.Sdk`
  - `coverlet.collector`（覆盖率收集）
- [ ] **5.1.3** 创建目录结构：
  ```
  tests/HysteriaAuth.Tests/
  ├── Unit/
  │   ├── Services/
  │   └── Controllers/
  ├── Integration/
  │   ├── ApiTests/
  │   └── DatabaseTests/
  └── E2E/
      └── AuthFlowTests/
  ```

#### 3.2 AuthService 测试（覆盖率目标 ≥ 90%）

- [ ] **5.2.1** 认证成功：正确用户名密码 → `Success = true`, `UserId > 0`
- [ ] **5.2.2** 密码错误：错误密码 → `invalid_credentials` (401)
- [ ] **5.2.3** 用户不存在：不存在的用户名 → `invalid_credentials` (401)
  - 注意：与密码错误返回相同错误代码（安全考虑）
- [ ] **5.2.4** 账号禁用：`IsActive = false` → `account_disabled` (403)
- [ ] **5.2.5** 账号过期：`ExpiresAt < UtcNow` → `account_expired` (403)
- [ ] **5.2.6** 永不过期：`ExpiresAt = null` → 不返回 `account_expired`
- [ ] **5.2.7** 流量耗尽：`UsedTrafficBytes >= TotalTrafficBytes` → `traffic_exhausted` (403)
- [ ] **5.2.8** 流量充足：`UsedTrafficBytes < TotalTrafficBytes` → 认证成功
- [ ] **5.2.9** 节点不在白名单：`AllowedNodes` 不包含 `nodeId` → `node_not_allowed` (403)
- [ ] **5.2.10** 节点在白名单中：`AllowedNodes` 包含 `nodeId` → 认证成功
- [ ] **5.2.11** 白名单为 NULL：`AllowedNodes = null` → 允许所有节点
- [ ] **5.2.12** 节点离线：目标节点 `IsActive = false` → `node_not_allowed`
- [ ] **5.2.13** 认证成功写入 AuthLog：成功后 `AuthLogs` 表有正确记录
- [ ] **5.2.14** 检查顺序不可变：验证 6 步检查按序执行

#### 3.3 UserService 测试（覆盖率目标 ≥ 85%）

- [ ] **5.3.1** 创建用户：正常创建 → 返回 201 + 用户数据，密码 BCrypt 加密
- [ ] **5.3.2** 创建用户-用户名重复：→ `conflict` (409)
- [ ] **5.3.3** 获取用户列表：分页 + 搜索 + 筛选
- [ ] **5.3.4** 获取用户详情：存在的 ID → 200 + 完整数据
- [ ] **5.3.5** 获取用户详情-不存在：→ `not_found` (404)
- [ ] **5.3.6** 更新用户：部分更新 → 仅修改传递的字段
- [ ] **5.3.7** 软删除用户：`IsActive = false`，历史数据保留
- [ ] **5.3.8** 重置用户流量：`UsedTrafficBytes = 0`

#### 3.4 AdminService 测试（覆盖率目标 ≥ 85%）

- [ ] **5.4.1** 管理员登录成功：正确凭据 → JWT Token + admin 信息
- [ ] **5.4.2** 密码错误：→ `invalid_credentials`
- [ ] **5.4.3** 登录失败计数：错误密码 → `FailedLoginAttempts++`
- [ ] **5.4.4** 账号锁定：连续 5 次错误 → `account_locked` (403)
- [ ] **5.4.5** 锁定过期：15 分钟后可登录
- [ ] **5.4.6** 成功登录重置：登录成功 → `FailedLoginAttempts = 0`
- [ ] **5.4.7** JWT Token 过期：过期 Token → `token_expired`
- [ ] **5.4.8** JWT Token 刷新：过期前 5 分钟内可刷新
- [ ] **5.4.9** 创建管理员：仅 `super_admin` → 成功
- [ ] **5.4.10** 权限拒绝：非 `super_admin` 创建管理员 → `forbidden` (403)

#### 3.5 TrafficService 测试（覆盖率目标 ≥ 90%）

- [ ] **5.5.1** 流量扣减单线程：`UsedTrafficBytes` 正确增加
- [ ] **5.5.2** 并发流量扣减：两个线程同时增量 → 最终值正确（关键测试）
- [ ] **5.5.3** 乐观并发重试：模拟 `DbUpdateConcurrencyException` → 重试后成功
- [ ] **5.5.4** 乐观并发重试耗尽：连续 3 次失败 → 抛出异常
- [ ] **5.5.5** 幂等检查-新数据：新幂等键 → 正常写入
- [ ] **5.5.6** 幂等检查-重复数据：相同幂等键 → 跳过写入（关键测试）
- [ ] **5.5.7** 幂等键格式正确性：`{nodeId}_{username}_{timestamp_rounded}`
- [ ] **5.5.8** 流量方向映射：`tx` → `BytesOut`, `rx` → `BytesIn`
- [ ] **5.5.9** 超额检测：`UsedTrafficBytes >= TotalTrafficBytes` → 触发踢人

#### 3.6 NodeService 测试（覆盖率目标 ≥ 85%）

- [ ] **5.6.1** 心跳-事务完整性：系统状态 + 流量数据 + 汇总在同一事务中
- [ ] **5.6.2** 心跳-事务回滚：模拟异常 → 无部分写入数据
- [ ] **5.6.3** 预注册节点：令牌生成 + 状态 `pending`
- [ ] **5.6.4** 令牌注册：有效令牌 → 注册成功 + 令牌清零 + 状态 `provisioned`
- [ ] **5.6.5** 令牌重复使用：→ `conflict` (409)
- [ ] **5.6.6** 令牌过期：→ `not_found` (404)
- [ ] **5.6.7** 节点离线检测：`LastHeartbeat > 90s` → `IsActive = false`

#### 3.7 Edge Agent AuthProxy 测试（覆盖率目标 ≥ 80%）

- [ ] **5.7.1** 协议转换-Hysteria → 内部：`auth` 解析为 `username + password`
- [ ] **5.7.2** 协议转换-auth 不含 `:` 分隔符：整个作为 username
- [ ] **5.7.3** 协议转换-`addr` 提取 IPv4：`"1.2.3.4:12345"` → `"1.2.3.4"`
- [ ] **5.7.4** 协议转换-`addr` 提取 IPv6：`"[::1]:12345"` → `"::1"`
- [ ] **5.7.5** 响应转换-成功：`InternalAuthResponse.Success = true` → `{"ok": true, "id": "..."}`
- [ ] **5.7.6** 响应转换-失败：`InternalAuthResponse.Success = false` → `{"ok": false}`
- [ ] **5.7.7** 缓存命中：第二次相同用户 → 直接返回缓存
- [ ] **5.7.8** 缓存降级：Mock 主服务器不可达 → 使用缓存

#### 3.8 Edge Agent TrafficCollector 测试（覆盖率目标 ≥ 80%）

- [ ] **5.8.1** 采集正常：Mock Hysteria API 返回流量数据 → 正确解析
- [ ] **5.8.2** 采集-Hysteria 不可达：Mock 异常 → 跳过 + Warning 日志
- [ ] **5.8.3** 采集-连续失败 3 次：→ Error 日志
- [ ] **5.8.4** 数据合并：采集结果正确传递给 `StatusReporter`

---

### 第 4~5 天：集成测试

#### 3.9 API 集成测试

- [ ] **5.9.1** 认证链路集成测试：
  ```
  Hysteria Client → Edge Agent (AuthProxy) → Master Server (AuthController) → DB
  ```
  - Mock Hysteria 原生请求 → Agent 的 AuthProxy → 调用真实 Master 端点 → 验证最终响应
- [ ] **5.9.2** 心跳处理集成测试：
  - 发送含系统状态 + 流量数据的心跳
  - 验证 `NodeStatus`, `TrafficRecords`, `NodeTraffic` 表记录正确
  - 验证 `Users.UsedTrafficBytes` 正确更新
  - 验证 `Nodes.LastHeartbeat` 更新
- [ ] **5.9.3** 心跳事务回滚测试：
  - 在事务中间注入异常
  - 验证所有表均无部分写入
- [ ] **5.9.4** 用户 CRUD 集成测试：
  - 创建 → 查询 → 更新 → 软删除 → 验证历史数据保留
- [ ] **5.9.5** 管理员登录集成测试：
  - 登录 → 获取 Token → 用 Token 访问管理 API → 验证权限
- [ ] **5.9.6** 审计日志集成测试：
  - 管理员创建用户 → `AdminAuditLogs` 有记录
  - `Detail` 字段包含正确的前后对比 JSON
- [ ] **5.9.7** 速率限制集成测试：
  - 构造高频率请求 → 部分返回 429
  - 验证限流计数器正确恢复

#### 3.10 数据库集成测试

- [ ] **5.10.1** EF Core Migration 正确性：
  - 执行 Migration → 9 张表 + 索引 + 约束全部创建
  - 执行 Rollback → 表全部删除
- [ ] **5.10.2** UNIQUE 约束生效：
  - 插入重复用户名 → `DbUpdateException`
  - 插入重复幂等键 → `DbUpdateException`
- [ ] **5.10.3** 乐观并发控制（RowVersion）：
  - 两个并发更新同一 User → 一个成功一个抛出 `DbUpdateConcurrencyException`
- [ ] **5.10.4** 数据保留清理：
  - 插入过期数据 → 触发清理任务 → 过期数据被删除

---

### 第 6~7 天：E2E 测试 + 部署 + 文档

#### 3.11 E2E 测试（完整认证链路）

- [ ] **5.11.1** 端到端认证流程（条件：本地 Hysteria 实例）：
  ```
  Hysteria Client 发起 QUIC 连接
    → Hysteria Server HTTP POST /auth 到 Agent
    → Agent 协议转换
    → Agent POST /api/v1/auth/hysteria 到 Master
    → Master 6 步认证检查
    → Master 返回成功/失败
    → Agent 转换响应
    → Hysteria Server 接受/拒绝连接
  ```
- [ ] **5.11.2** 端到端流量采集流程：
  ```
  用户产生流量（下载/上传文件）
    → Hysteria 累计流量
    → Agent GET /traffic?clear=1
    → Agent 心跳上报
    → Master 幂等扣减
    → Users.UsedTrafficBytes 正确
  ```
- [ ] **5.11.3** 端到端超额踢人流程：
  ```
  用户流量超额
    → Master 检测
    → Master → Agent POST /kick-user
    → Agent → Hysteria POST /kick
    → Hysteria 断开用户连接
  ```

#### 3.12 部署脚本

- [ ] **5.12.1** 创建 `scripts/deploy-master.sh`：
  - 安装 .NET 8.0 Runtime
  - 创建目录结构（`/opt/hysteria-auth/master`, `/var/lib/hysteria-auth`, `/var/log/hysteria-auth`, `/var/backups/hysteria-auth`）
  - 复制发布文件
  - 设置权限（`www-data`）
  - 创建 systemd 服务
  - 启动服务
- [ ] **5.12.2** 创建 `scripts/deploy-agent-provisioned.sh`（推荐方式）：
  - 预注册令牌部署
  - 最小配置（仅 `provisionToken` + `masterServerUrl`）
  - 创建 systemd 服务
  - Agent 自动完成注册和配置生成
- [ ] **5.12.3** 创建 `scripts/deploy-agent-legacy.sh`（保留兼容）：
  - 手动配置部署
  - 自动生成 `NodeId` + `NodeSecret`
  - 完整 `agent.json` 配置

#### 3.13 systemd 服务文件

- [ ] **5.13.1** 创建 `scripts/hysteria-auth-master.service`：
  ```ini
  [Unit]
  Description=Hysteria Auth Master Server
  After=network.target
  
  [Service]
  Type=notify
  User=www-data
  Group=www-data
  WorkingDirectory=/opt/hysteria-auth/master
  ExecStart=/usr/bin/dotnet /opt/hysteria-auth/master/HysteriaAuth.Master.dll
  Restart=always
  RestartSec=5
  Environment=ASPNETCORE_ENVIRONMENT=Production
  Environment=ASPNETCORE_URLS=http://127.0.0.1:5000
  
  [Install]
  WantedBy=multi-user.target
  ```
- [ ] **5.13.2** 创建 `scripts/hysteria-auth-agent.service`：
  ```ini
  [Unit]
  Description=Hysteria Auth Edge Agent
  After=network.target hysteria-server.service
  Requires=hysteria-server.service
  
  [Service]
  Type=notify
  User=root
  Group=root
  WorkingDirectory=/opt/hysteria-auth/agent
  ExecStart=/usr/bin/dotnet /opt/hysteria-auth/agent/HysteriaAuth.Agent.dll
  Restart=always
  RestartSec=5
  Environment=ASPNETCORE_ENVIRONMENT=Production
  
  [Install]
  WantedBy=multi-user.target
  ```

#### 3.14 Nginx 反向代理配置

- [ ] **5.14.1** 创建 `scripts/nginx-master.conf`：
  ```nginx
  server {
      listen 443 ssl http2;
      server_name master.example.com;
      
      # 健康检查
      location /health { ... }
      # 认证 API（Edge Agent 调用）  
      location /api/v1/auth/ { ... }
      # 管理 API
      location /api/v1/admin/ { ... }
      # 用户管理 API
      location /api/v1/users/ { ... }
      # 节点 API
      location /api/v1/nodes/ { ... }
  }
  ```

#### 3.15 Docker 支持（可选）

- [ ] **5.15.1** 创建 `docker/Dockerfile.master`
- [ ] **5.15.2** 创建 `docker/Dockerfile.agent`
- [ ] **5.15.3** 创建 `docker/docker-compose.yml`（含 Hysteria + Agent 组合）

#### 3.16 文档完善

- [ ] **5.16.1** 创建项目根目录 `README.md`：
  - 项目简介
  - 快速开始（5 分钟跑起来）
  - 架构概览
  - API 文档链接
  - 部署指南链接
  - 开发指南链接
- [ ] **5.16.2** 创建 `document/quick-start.md`（快速开始指南）
- [ ] **5.16.3** 验证所有文档交叉引用链接有效

---

## 4. 应遵守的规范

| 规范来源 | 条款 | Phase 5 适用要点 |
|----------|------|-----------------|
| [测试规范](../develop/backend-development-spec.md#11-测试规范) | §11.1 | xUnit + Moq + FluentAssertions |
| [测试规范](../develop/backend-development-spec.md#11-测试规范) | §11.2 | 各模块覆盖率目标（AuthService≥90%, UserService≥85%, TrafficService≥90%, 等） |
| [测试规范](../develop/backend-development-spec.md#11-测试规范) | §11.3 | 必测场景：认证成功/失败、协议转换、并发扣减、幂等检查、心跳事务、E2E 认证链路 |
| [部署规范](../develop/backend-development-spec.md#13-部署规范) | §13.1 | 系统要求：Ubuntu 20.04+, .NET 8.0 Runtime, ≥1GB RAM (Master), ≥256MB RAM (Agent) |
| [部署规范](../develop/backend-development-spec.md#13-部署规范) | §13.2 | systemd：Master 用 `www-data`/`Type=notify`，Agent 用 `root`/`Type=notify`/`After=hysteria-server.service` |
| [部署规范](../develop/backend-development-spec.md#13-部署规范) | §13.3 | 备份：24h 间隔，`.backup` 命令，30 天保留 |
| [部署规范](../develop/backend-development-spec.md#13-部署规范) | §13.4 | Hysteria 配置：`auth.http.url` 指向 `127.0.0.1:8080/auth`，`trafficStats.listen` 绑定 `127.0.0.1:9999` |

---

## 5. 应特别注意的事项

### 5.1 测试数据隔离

- 单元测试使用 **Mock 对象**，不触碰数据库
- 集成测试使用 **EF Core InMemory** 或 **Testcontainers**（推荐 Testcontainers SQLite）
- ❌ 测试不能使用生产数据库
- 每个测试方法应当是独立的（不依赖其他测试的执行顺序）

### 5.2 并发测试的可靠性

- 并发扣减测试（两个线程同时更新 `UsedTrafficBytes`）可能在不同硬件上表现不同
- 使用 `Task.WhenAll` 而非 `Parallel.ForEach`（避免线程池竞争）
- 测试结果应是确定性的（无论重试多少次，最终值都是一致的）
- 可能需要 `[Collection]` 特性来防止测试间的数据库状态污染

### 5.3 E2E 测试的环境依赖

- E2E 测试需要本地运行的 Hysteria 实例
- 如果无法提供 Hysteria 实例，可以 Mock Hysteria 的 HTTP Auth 和 trafficStats API
- 理想情况：CI 环境中有 Docker 化的 Hysteria 实例
- **最低要求**：至少有一个 E2E 测试脚本可以在本地手动运行

### 5.4 覆盖率不是唯一标准

- 覆盖率目标是指导性的，更重要的是**关键场景是否被覆盖**
- 必测场景清单（§3.2~§3.8）> 覆盖率数字
- 如果覆盖率达标但关键场景遗漏 → 不合格
- 如果覆盖率略低于目标但关键场景全覆盖 → 可接受

### 5.5 部署脚本的安全性

- `agent.json` 权限必须是 `600`（仅 root 可读写）
- 密钥（`NodeSecret`, `trafficStatsSecret`）必须在部署时随机生成
- ❌ 部署脚本中不能硬编码密钥
- systemd 服务文件中的环境变量不应包含密钥（密钥在 `agent.json` 中）

### 5.6 数据库备份的首次执行

- 部署后应立即手动执行一次备份，验证备份流程正确
- 首次备份后才能启用自动备份定时任务
- 备份目录的磁盘空间至少是数据库文件的 30 倍（保留 30 份）

### 5.7 文档的准确性

- README 中的"快速开始"必须能在干净环境中实际运行通过
- 所有 API 端点示例的 URL 和请求体必须与实际代码一致
- 配置示例中的占位符（如 `your-super-secret-key`）必须标注清楚需要替换

---

## 6. 关键代码模板与示例

### 6.1 单元测试示例（认证成功）

```csharp
// tests/HysteriaAuth.Tests/Unit/Services/AuthServiceTests.cs
using Xunit;
using Moq;
using FluentAssertions;

namespace HysteriaAuth.Tests.Unit.Services;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _userRepoMock;
    private readonly Mock<IAuthLogRepository> _authLogRepoMock;
    private readonly Mock<INodeRepository> _nodeRepoMock;
    private readonly AuthService _sut; // System Under Test

    public AuthServiceTests()
    {
        _userRepoMock = new Mock<IUserRepository>();
        _authLogRepoMock = new Mock<IAuthLogRepository>();
        _nodeRepoMock = new Mock<INodeRepository>();
        _sut = new AuthService(
            _userRepoMock.Object,
            _authLogRepoMock.Object,
            _nodeRepoMock.Object,
            Mock.Of<ILogger<AuthService>>());
    }

    [Fact]
    public async Task AuthenticateAsync_ValidCredentials_ReturnsSuccess()
    {
        // Arrange
        var password = "testPassword123";
        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);

        var user = new User
        {
            Id = 1,
            Username = "testuser",
            Password = hashedPassword,
            IsActive = true,
            TotalTrafficBytes = 10L * 1024 * 1024 * 1024, // 10GB
            UsedTrafficBytes = 0,
            ExpiresAt = null,
            AllowedNodes = null
        };

        var request = new AuthRequest
        {
            Username = "testuser",
            Password = password,
            NodeId = "edge-node-01",
            ClientIp = "192.168.1.100"
        };

        _userRepoMock.Setup(r => r.GetByUsernameAsync("testuser"))
            .ReturnsAsync(user);
        _nodeRepoMock.Setup(r => r.GetByIdAsync("edge-node-01"))
            .ReturnsAsync(new Node { Id = "edge-node-01", IsActive = true });

        // Act
        var result = await _sut.AuthenticateAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.UserId.Should().Be(1);
        result.RemainingTraffic.Should().Be(user.TotalTrafficBytes);

        // 验证 AuthLog 已被写入
        _authLogRepoMock.Verify(r => r.AddAsync(
            It.Is<AuthLog>(log =>
                log.Username == "testuser" &&
                log.Success == true &&
                log.NodeId == "edge-node-01")),
            Times.Once);
    }

    [Fact]
    public async Task AuthenticateAsync_WrongPassword_ReturnsInvalidCredentials()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            Password = BCrypt.Net.BCrypt.HashPassword("correctPassword", 12),
            IsActive = true
        };

        var request = new AuthRequest
        {
            Username = "testuser",
            Password = "wrongPassword",
            NodeId = "edge-node-01",
            ClientIp = "192.168.1.100"
        };

        _userRepoMock.Setup(r => r.GetByUsernameAsync("testuser"))
            .ReturnsAsync(user);

        // Act
        var result = await _sut.AuthenticateAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("invalid_credentials");
        result.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task AuthenticateAsync_TrafficExhausted_ReturnsTrafficExhausted()
    {
        // Arrange
        var password = "testPassword123";
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            Password = BCrypt.Net.BCrypt.HashPassword(password, 12),
            IsActive = true,
            TotalTrafficBytes = 1024,
            UsedTrafficBytes = 1024, // 已用完
            ExpiresAt = null,
            AllowedNodes = null
        };

        var request = new AuthRequest
        {
            Username = "testuser",
            Password = password,
            NodeId = "edge-node-01",
            ClientIp = "192.168.1.100"
        };

        _userRepoMock.Setup(r => r.GetByUsernameAsync("testuser"))
            .ReturnsAsync(user);

        // Act
        var result = await _sut.AuthenticateAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("traffic_exhausted");
        result.StatusCode.Should().Be(403);
    }
}
```

### 6.2 集成测试示例（API 端点）

```csharp
// tests/HysteriaAuth.Tests/Integration/ApiTests/AuthApiTests.cs
public class AuthApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public AuthApiTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // 替换数据库为 InMemory
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                services.Remove(descriptor);
                services.AddDbContext<AppDbContext>(options =>
                    options.UseInMemoryDatabase("TestDb"));
            });
        });
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task Post_AuthHysteria_ValidCredentials_Returns200()
    {
        // Arrange — 先创建一个测试用户
        var createResponse = await _client.PostAsJsonAsync("/api/v1/users", new
        {
            username = "testuser",
            password = "testpass123",
            totalTrafficBytes = 10737418240
        });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Act
        var authRequest = new
        {
            username = "testuser",
            password = "testpass123",
            nodeId = "edge-node-01",
            clientIp = "192.168.1.100"
        };
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/hysteria");
        request.Headers.Add("X-Node-Secret", "valid-node-secret");
        request.Content = JsonContent.Create(authRequest);
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        body.Success.Should().BeTrue();
        body.UserId.Should().BeGreaterThan(0);
    }
}
```

### 6.3 并发测试示例

```csharp
// tests/HysteriaAuth.Tests/Integration/DatabaseTests/ConcurrencyTests.cs
[Fact]
public async Task UpdateUsedTraffic_ConcurrentUpdates_ShouldBeCorrect()
{
    // Arrange — 创建测试用户
    var user = new User
    {
        Username = "concurrent_test",
        Password = BCrypt.Net.BCrypt.HashPassword("pass", 12),
        TotalTrafficBytes = 10L * 1024 * 1024 * 1024,
        UsedTrafficBytes = 0,
        IsActive = true,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };
    _context.Users.Add(user);
    await _context.SaveChangesAsync();

    var service = new TrafficService(_context, Mock.Of<ILogger<TrafficService>>());

    // Act — 两个任务同时更新同一用户
    var task1 = Task.Run(() =>
        service.UpdateUsedTrafficAsync(user.Id, 1024));
    var task2 = Task.Run(() =>
        service.UpdateUsedTrafficAsync(user.Id, 2048));

    await Task.WhenAll(task1, task2);

    // Assert — 最终值应为两者之和
    var updatedUser = await _context.Users.FindAsync(user.Id);
    updatedUser.UsedTrafficBytes.Should().Be(3072); // 1024 + 2048
}
```

### 6.4 部署脚本示例

```bash
#!/bin/bash
# scripts/deploy-master.sh
set -e

echo "=== Hysteria Auth Master Server 部署脚本 ==="

# 1. 安装 .NET Runtime
echo "[1/6] 安装 .NET 8.0 Runtime..."
sudo apt-get update -qq
sudo apt-get install -y -qq dotnet-runtime-8.0

# 2. 创建应用目录
echo "[2/6] 创建目录结构..."
sudo mkdir -p /opt/hysteria-auth/master
sudo mkdir -p /var/lib/hysteria-auth
sudo mkdir -p /var/log/hysteria-auth
sudo mkdir -p /var/backups/hysteria-auth

# 3. 复制应用文件
echo "[3/6] 复制应用文件..."
sudo cp -r publish/* /opt/hysteria-auth/master/

# 4. 设置权限
echo "[4/6] 设置权限..."
sudo chown -R www-data:www-data /opt/hysteria-auth/master
sudo chown -R www-data:www-data /var/lib/hysteria-auth
sudo chown -R www-data:www-data /var/log/hysteria-auth
sudo chown -R www-data:www-data /var/backups/hysteria-auth

# 5. 创建 systemd 服务
echo "[5/6] 创建 systemd 服务..."
sudo cp scripts/hysteria-auth-master.service /etc/systemd/system/

# 6. 启动服务
echo "[6/6] 启动服务..."
sudo systemctl daemon-reload
sudo systemctl enable hysteria-auth-master
sudo systemctl start hysteria-auth-master

echo "=== 部署完成 ==="
echo "检查服务状态: sudo systemctl status hysteria-auth-master"
echo "查看日志: sudo journalctl -u hysteria-auth-master -f"
```

---

## 7. 阶段完成标准（Phase 5 = 项目完成标准）

| 标准 | 验证方式 |
|------|----------|
| **单元测试** | |
| AuthService 覆盖率 ≥ 90% | `dotnet test --collect:"XPlat Code Coverage"` |
| UserService 覆盖率 ≥ 85% | 同上 |
| TrafficService 覆盖率 ≥ 90% | 同上 |
| NodeService 覆盖率 ≥ 85% | 同上 |
| AdminService 覆盖率 ≥ 85% | 同上 |
| AuthProxy (Agent) 覆盖率 ≥ 80% | 同上 |
| TrafficCollector (Agent) 覆盖率 ≥ 80% | 同上 |
| 必测场景全部覆盖 | 审查测试列表 (§3.2~§3.8) |
| **集成测试** | |
| API 集成测试通过 | `dotnet test --filter "Category=Integration"` |
| 数据库集成测试通过 | 同上 |
| **E2E 测试** | |
| 完整认证链路通过 | 手动运行 E2E 脚本或测试 |
| 流量采集链路通过 | 同上 |
| **部署** | |
| 部署脚本可在干净 Ubuntu 上执行 | 在 Docker/VM 中验证 |
| systemd 服务正常启动 | `systemctl status` 显示 `active (running)` |
| 健康检查返回 healthy | `curl http://127.0.0.1:5000/health` → healthy |
| Nginx 反向代理可正常转发 | `curl https://master.example.com/health` → healthy |
| 数据库备份脚本可执行 | `bash scripts/backup-db.sh` → 生成 `.db` 文件 |
| **文档** | |
| README.md 完整可读 | 第三方能根据 README 部署 |
| 快速开始指南可 5 分钟跑通 | 实际验证 |
| API 文档交叉引用有效 | 检查所有 Markdown 链接 |
| **代码质量** | |
| `dotnet build` 无警告 | 0 Warning |
| `dotnet test` 全部通过 | 0 Failed |
| API 错误率 < 0.1% | 负载测试验证 |

---

## 8. 项目交付清单

Phase 5 完成后，以下为最终交付给运维/部署人员的完整清单：

| 交付项 | 路径 | 说明 |
|--------|------|------|
| 主服务器发布包 | `publish/master/` | `dotnet publish -c Release` 输出 |
| Edge Agent 发布包 | `publish/agent/` | `dotnet publish -c Release` 输出 |
| 部署脚本-主服务器 | `scripts/deploy-master.sh` | 一键部署 |
| 部署脚本-令牌方式 | `scripts/deploy-agent-provisioned.sh` | 推荐 |
| 部署脚本-旧版 | `scripts/deploy-agent-legacy.sh` | 保留兼容 |
| systemd-主服务器 | `scripts/hysteria-auth-master.service` | 服务定义 |
| systemd-Edge Agent | `scripts/hysteria-auth-agent.service` | 服务定义 |
| Nginx 配置 | `scripts/nginx-master.conf` | 反向代理 |
| 备份脚本 | `scripts/backup-db.sh` | 数据备份 |
| 恢复脚本 | `scripts/restore-db.sh` | 数据恢复 |
| 配置模板 | `appsettings.json.example` | 含注释 |
| Agent 配置模板 | `agent.json.example` | 含注释 |
| README | `README.md` | 项目说明 |
| 快速开始 | `document/quick-start.md` | 5 分钟部署指南 |
| 测试报告 | `coverage/` | 覆盖率报告 |
| Docker（可选） | `docker/` | Docker 化部署 |
