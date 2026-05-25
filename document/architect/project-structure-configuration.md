# 项目结构与配置说明

> **父文档**: [架构文档目录](README.md) | **关联**: [`deployment.md`](deployment.md) · [`security-design.md`](security-design.md) · [`spa-integration.md`](spa-integration.md)

---

## 1. 项目目录结构

```
HysteriaAuth/
├── src/
│   ├── HysteriaAuth.Master/           # 主服务器项目
│   │   ├── Controllers/
│   │   │   ├── AuthController.cs      # 认证控制器
│   │   │   ├── UsersController.cs     # 用户管理控制器
│   │   │   ├── NodesController.cs     # 节点管理控制器
│   │   │   ├── AdminController.cs     # 管理功能控制器
│   │   │   ├── TrafficController.cs   # 流量统计控制器
│   │   │   └── HealthController.cs    # 健康检查控制器
│   │   ├── Services/
│   │   │   ├── AuthService.cs         # 认证服务
│   │   │   ├── UserService.cs         # 用户服务
│   │   │   ├── NodeService.cs         # 节点服务
│   │   │   ├── TrafficService.cs      # 流量服务
│   │   │   ├── AdminService.cs        # 管理员服务
│   │   │   └── AuditService.cs        # 审计日志服务
│   │   ├── Repositories/
│   │   │   ├── IUserRepository.cs
│   │   │   ├── INodeRepository.cs
│   │   │   ├── ITrafficRepository.cs
│   │   │   ├── IAuthLogRepository.cs
│   │   │   ├── IAdminRepository.cs
│   │   │   └── IAuditLogRepository.cs
│   │   ├── Models/
│   │   │   ├── Entities/              # EF Core 实体
│   │   │   │   ├── User.cs
│   │   │   │   ├── Node.cs
│   │   │   │   ├── TrafficRecord.cs
│   │   │   │   ├── AuthLog.cs
│   │   │   │   ├── Session.cs
│   │   │   │   ├── NodeStatus.cs
│   │   │   │   ├── NodeTraffic.cs
│   │   │   │   ├── Admin.cs
│   │   │   │   └── AdminAuditLog.cs
│   │   │   ├── DTOs/                  # 数据传输对象
│   │   │   │   ├── UserDto.cs
│   │   │   │   ├── NodeDto.cs
│   │   │   │   ├── AuthRequest.cs
│   │   │   │   ├── AdminDto.cs
│   │   │   │   └── ErrorResponse.cs
│   │   │   └── ViewModel/             # 视图模型
│   │   ├── Data/
│   │   │   ├── AppDbContext.cs        # EF Core 上下文
│   │   │   └── Migrations/            # 数据库迁移
│   │   ├── Middleware/
│   │   │   ├── JwtMiddleware.cs       # JWT 认证中间件
│   │   │   ├── NodeAuthMiddleware.cs  # 节点密钥认证中间件
│   │   │   ├── ExceptionMiddleware.cs # 全局异常处理中间件
│   │   │   ├── RateLimitMiddleware.cs # 速率限制中间件
│   │   │   └── AuditMiddleware.cs     # 管理员操作审计中间件
│   │   ├── Config/
│   │   │   ├── AppSettings.cs         # 配置类
│   │   │   ├── JwtSettings.cs         # JWT 配置
│   │   │   ├── RateLimitSettings.cs   # 速率限制配置
│   │   │   ├── SpaSettings.cs         # SPA 静态文件配置（新增）
│   │   │   └── HttpsSettings.cs       # HTTPS 证书自动检测 + 监听端口配置（Phase 8 新增）
│   │   ├── wwwroot/                   # SPA 前端构建产物（新增，路径可配置）
│   │   ├── appsettings.json           # 配置文件
│   │   └── Program.cs                 # 入口
│   │
│   └── HysteriaAuth.Agent/            # 边缘节点 Agent 项目
│       ├── Services/
│       │   ├── SystemMonitor.cs       # 系统监控服务
│       │   ├── AuthProxy.cs           # 认证代理服务（含协议转换）
│       │   ├── TrafficCollector.cs    # 流量采集服务
│       │   ├── StatusReporter.cs      # 状态上报服务
│       │   └── Initializer.cs         # 启动初始化与注册服务
│       ├── Models/
│       │   ├── SystemMetrics.cs       # 系统指标模型
│       │   ├── TrafficInfo.cs         # 流量数据模型
│       │   └── NodeConfig.cs          # 节点配置
│       ├── Config/
│       │   └── agent.json             # Agent 配置
│       ├── Middleware/
│       │   └── HealthCheckMiddleware.cs # 健康检查端点
│       └── Program.cs                 # 入口
│
├── tests/
│   ├── HysteriaAuth.Tests/
│   │   ├── Unit/
│   │   │   ├── Services/
│   │   │   └── Controllers/
│   │   ├── Integration/
│   │   │   ├── ApiTests/
│   │   │   └── DatabaseTests/
│   │   └── E2E/
│   │       └── AuthFlowTests/
│
├── document/
│   ├── architect/                     # 架构设计文档
│   │   ├── spa-integration.md         # SPA 集成设计（新增）
│   ├── hysteria/                      # Hysteria 配置/API 参考
│   └── develop/                       # 开发文档
│
├── scripts/
│   ├── deploy-master.sh               # 主服务器部署脚本
│   ├── deploy-agent.sh                # Agent 部署脚本
│   ├── dev-build.ps1                  # Windows 开发环境构建脚本（新增）
│   ├── publish-linux.ps1              # Windows→Linux 交叉编译脚本（新增）
│   ├── backup-db.sh                   # 数据库备份脚本
│   └── restore-db.sh                  # 数据库恢复脚本
│
├── docker/
│   ├── Dockerfile.master
│   ├── Dockerfile.agent
│   └── docker-compose.yml
│
└── HysteriaAuth.sln                   # 解决方案文件
```

---

## 2. 主服务器配置 (appsettings.json)

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
    "Spa": {
        "Enabled": true,
        "StaticFilesPath": "wwwroot",
        "FallbackFile": "index.html",
        "CacheMaxAgeSeconds": 86400
    },
    "Https": {
        "ListenAddress": "0.0.0.0",
        "ListenPort": 5000,
        "CertDirectoryPath": "cert",
        "CertFileName": "server.cert",
        "CertKeyFileName": "server.key"
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

### 配置项说明

| 配置节 | 关键项 | 说明 |
|--------|--------|------|
| `ConnectionStrings` | `DefaultConnection` | SQLite 数据库文件路径 |
| `Jwt` | `Secret` / `ExpirationMinutes` | JWT 签名密钥和过期时间 |
| `Auth` | `CacheEnabled` / `CacheExpirationMinutes` | 认证缓存开关和过期时间 |
| `Node` | `HeartbeatTimeoutSeconds` | 超过此时间无心跳标记节点离线 |
| `Traffic` | `TrafficDataRetentionDays` / `ConcurrentUpdateRetryCount` | 流量数据保留天数和并发重试次数 |
| `Admin` | `MaxFailedLoginAttempts` / `LockoutDurationMinutes` | 管理员登录失败锁定策略 |
| `RateLimit` | `AuthPerMinute` / `AdminApiPerMinute` | 各端点速率限制配置 |
| `Spa` | `Enabled` / `StaticFilesPath` / `FallbackFile` | SPA 静态文件托管开关、构建产物路径（支持相对/绝对路径）、兜底文件名。详见 [`spa-integration.md`](spa-integration.md) |
| `Https` | `ListenAddress` / `ListenPort` | Kestrel 监听地址和端口。默认 `0.0.0.0:5000`。无论 HTTP/HTTPS 均使用同一端口 |
| `Https` | `CertDirectoryPath` / `CertFileName` / `CertKeyFileName` | HTTPS 证书自动检测：启动时检查该目录下是否存在指定证书文件，存在则启用 HTTPS。支持相对和绝对路径。详见 Phase 8 |
| `Backup` | `AutoBackupEnabled` / `BackupIntervalHours` | 自动备份开关和间隔 |

---

## 3. 边缘节点 Agent 配置 (agent.json)

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

### 配置项说明

| 配置节 | 关键项 | 说明 |
|--------|--------|------|
| `NodeId` | UUID | 节点唯一标识，首次启动时自动生成 |
| `MasterServerUrl` | URL | 主服务器地址 |
| `Init` | `RegistrationRetryMax` / `RegistrationRetryMaxSeconds` | 注册重试最大次数和最大间隔 |
| `AuthProxy` | `ListenPort` | 认证代理监听端口，需与 Hysteria `auth.http.url` 一致 |
| `TrafficStats` | `Secret` / `CollectIntervalSeconds` | 需与 Hysteria `trafficStats.secret` 一致 |
| `Monitor` | `IntervalSeconds` / `NetworkInterfaces` | 系统监控采集间隔和网口 |
| `Reporter` | `IntervalSeconds` / `RetryCount` | 心跳上报间隔和重试次数 |
| `Cache` | `MaxSize` / `ExpirationMinutes` | 认证缓存条目上限和过期时间 |
| `HealthCheck` | `Enabled` / `ListenPort` | 健康检查端点开关和端口 |
