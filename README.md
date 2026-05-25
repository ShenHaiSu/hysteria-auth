# Hysteria Auth — 代理认证后端系统

> **版本**: v1.2 | **更新日期**: 2026-05-22

基于 C# .NET 10.0 的 Hysteria 代理认证后端系统，采用**主从架构**：

- **主服务器 (Master Server)**：提供完整的 HTTP API 服务、用户管理、数据存储、认证授权、流量统计、SPA 管理控制台托管
- **边缘节点 (Edge Agent)**：部署在各 VPS 上，负责本地 Hysteria 服务运行、状态上报、认证请求转发、流量数据采集

## 快速开始

> 参见 [document/quick-start.md](document/quick-start.md) 获取 5 分钟部署指南。

### Windows 开发环境一键构建

```powershell
# 在项目根目录执行
.\scripts\dev-build.ps1

# 指定前端 dist 路径
.\scripts\dev-build.ps1 -FrontendDistPath "D:\projects\hysteria-auth-web\dist"

# 启动测试
cd publish\local-dev
.\HysteriaAuth.Master.exe
# 浏览器访问 http://localhost:5000
```

### 开发环境运行

```bash
# 启动主服务器
dotnet run --project src/HysteriaAuth.Master

# 启动边缘节点 Agent
dotnet run --project src/HysteriaAuth.Agent

# 运行全部测试
dotnet test
```

### 跨平台编译（Windows → Linux）

```powershell
# 在 Windows 上编译 Linux-x64 版本 + 打包 .tar.gz
.\scripts\publish-linux.ps1
```

### 生产环境部署

```bash
# 主服务器一键部署（自包含，无需 .NET Runtime）
bash scripts/deploy-master.sh

# 边缘节点令牌方式部署（推荐）
bash scripts/deploy-agent-provisioned.sh <provision_token> <master_url>

# 数据库备份
bash scripts/backup-db.sh
```

## 架构概览

```
客户端 → Hysteria Server → Edge Agent → 主服务器 → SQLite
         (QUIC)    (HTTP Auth)    (REST API)    (EF Core)

浏览器 → 主服务器 :5000 (SPA 管理控制台)
         (UseStaticFiles + MapFallbackToFile)
```

详细架构设计参见 [document/architect/](document/architect/README.md)。

## 核心功能

| 功能 | 说明 |
|------|------|
| **用户认证** | 双层认证架构：Hysteria 原生 HTTP Auth → Edge Agent 协议转换 → 主服务器验证 |
| **流量统计** | 边缘节点定时采集 Hysteria trafficStats API，合并心跳上报，幂等扣减 |
| **流量管理** | 配额控制、已用流量追踪、超额自动踢下线 |
| **节点监控** | CPU/内存/网络实时采集，心跳超时自动标记离线 |
| **管理员管理** | JWT 认证、RBAC 权限 (super_admin/admin/readonly)、操作审计日志 |
| **SPA 管理控制台** | 托管 Vue3+Vite7 前端 SPA，支持 history 模式路由兜底，可配置静态文件路径 |
| **健康检查** | 主服务器和边缘节点均暴露 `/health` 端点 |
| **跨平台部署** | 自包含发布（--self-contained），Windows→Linux 交叉编译，无需目标机器安装 .NET Runtime |

## API 文档

完整 API 定义参见 [document/architect/api-design.md](document/architect/api-design.md)

关键端点：

| 端点 | 说明 | 认证 |
|------|------|------|
| `POST /api/v1/auth/hysteria` | Hysteria 用户认证 | `X-Node-Secret` |
| `GET /api/v1/users` | 用户列表 | `Bearer {admin_token}` |
| `POST /api/v1/users` | 创建用户 | `Bearer {admin_token}` |
| `POST /api/v1/nodes/{nodeId}/heartbeat` | 节点心跳+流量上报 | `X-Node-Secret` |
| `POST /api/v1/admin/login` | 管理员登录 | 无 |
| `GET /health` | 健康检查 | 无 |
| `/` (SPA) | Web 管理控制台 | 无（静态文件） |

## 项目结构

```
src/
├── HysteriaAuth.Master/    # 主服务器
│   ├── Controllers/        # API 控制器
│   ├── Services/           # 业务逻辑
│   ├── Repositories/       # 数据访问
│   ├── Models/             # 实体、DTO、视图模型
│   ├── Data/               # EF Core 上下文和迁移
│   ├── Config/             # 强类型配置类（含 SpaSettings）
│   ├── wwwroot/            # SPA 前端构建产物（路径可配置）
│   └── Middleware/         # 认证、异常、限流、SPA 静态文件中间件
│
└── HysteriaAuth.Agent/     # 边缘节点 Agent
    ├── Services/           # 系统监控、认证代理、流量采集、上报
    ├── Models/             # Agent 模型
    └── Config/             # agent.json 配置

tests/
└── HysteriaAuth.Tests/     # 测试项目
    ├── Unit/Services/      # 单元测试
    ├── Integration/        # 集成测试
    └── E2E/                # 端到端测试

scripts/                    # 部署和运维脚本
    ├── dev-build.ps1       # Windows 开发环境一键构建（新增）
    ├── publish-linux.ps1   # Windows→Linux 交叉编译打包（新增）
    ├── deploy-master.sh
    ├── deploy-agent-provisioned.sh
    ├── hysteria-auth-master.service
    ├── hysteria-auth-agent.service
    ├── nginx-master.conf
    ├── backup-db.sh
    └── restore-db.sh

docker/                     # Docker 支持（含前端 SPA 多阶段构建）
    ├── Dockerfile.master
    ├── Dockerfile.agent
    └── docker-compose.yml
```

## 技术栈

| 组件 | 技术选型 |
|------|----------|
| 开发语言 | C# (.NET 10.0) |
| Web 框架 | ASP.NET Core |
| 数据库 | SQLite (EF Core) |
| 认证 | JWT (HMAC-SHA256) + BCrypt |
| SPA 托管 | `UseStaticFiles` + `MapFallbackToFile` |
| 测试 | xUnit + Moq + FluentAssertions |
| 部署模式 | 自包含发布（--self-contained），支持跨平台交叉编译 |

## 配置亮点

| 配置项 | 位置 | 说明 |
|--------|------|------|
| `Spa.Enabled` | `appsettings.json` | 是否启用 SPA 托管，默认 `true` |
| `Spa.StaticFilesPath` | `appsettings.json` | 前端构建产物路径，支持相对/绝对路径，默认 `"wwwroot"` |
| `Spa.FallbackFile` | `appsettings.json` | SPA 兜底文件名，默认 `"index.html"` |
| `Spa.CacheMaxAgeSeconds` | `appsettings.json` | 静态资源缓存最大时长（秒），默认 86400 |

> 当 `StaticFilesPath` 目录不存在时，仅记录 Warning 日志，不阻止服务启动。API 功能完全正常。

## 文档导航

| 文档 | 路径 |
|------|------|
| 架构文档总目录 | [document/architect/README.md](document/architect/README.md) |
| SPA 前端集成设计 | [document/architect/spa-integration.md](document/architect/spa-integration.md) |
| 部署方案与交叉编译 | [document/architect/deployment.md](document/architect/deployment.md) |
| 后端开发规范 | [document/develop/backend-development-spec.md](document/develop/backend-development-spec.md) |
| 开发阶段文档 | [document/developStage/00-README.md](document/developStage/00-README.md) |
| 快速开始指南 | [document/quick-start.md](document/quick-start.md) |

## 部署清单

| 交付项 | 路径 |
|--------|------|
| 主服务器发布包 | `publish/linux-x64/` |
| Edge Agent 发布包 | `publish/linux-x64/agent/` |
| Windows 开发构建 | `scripts/dev-build.ps1` |
| Linux 交叉编译打包 | `scripts/publish-linux.ps1` |
| 部署脚本-主服务器 | `scripts/deploy-master.sh` |
| 部署脚本-令牌方式 | `scripts/deploy-agent-provisioned.sh` |
| systemd-主服务器 | `scripts/hysteria-auth-master.service` |
| systemd-Edge Agent | `scripts/hysteria-auth-agent.service` |
| Nginx 配置 | `scripts/nginx-master.conf` |
| 备份脚本 | `scripts/backup-db.sh` |
| 恢复脚本 | `scripts/restore-db.sh` |
| Docker 支持 | `docker/` |
