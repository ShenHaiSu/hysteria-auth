# Hysteria Auth — 代理认证后端系统

基于 C# .NET 8.0 的 Hysteria 代理认证后端系统，采用**主从架构**：

- **主服务器 (Master Server)**：提供完整的 HTTP API 服务、用户管理、数据存储、认证授权、流量统计
- **边缘节点 (Edge Agent)**：部署在各 VPS 上，负责本地 Hysteria 服务运行、状态上报、认证请求转发、流量数据采集

## 快速开始

> 参见 [document/quick-start.md](document/quick-start.md) 获取 5 分钟部署指南。

### 开发环境运行

```bash
# 启动主服务器
dotnet run --project src/HysteriaAuth.Master

# 启动边缘节点 Agent
dotnet run --project src/HysteriaAuth.Agent

# 运行全部测试
dotnet test
```

### 生产环境部署

```bash
# 主服务器一键部署
bash scripts/deploy-master.sh

# 边缘节点令牌方式部署（推荐）
bash scripts/deploy-agent-provisioned.sh <provision_token> <master_url>

# 数据库备份
bash scripts/backup-db.sh
```

## 架构概览

```
客户端 → Hysteria Server → Edge Agent → 主服务器 → SQLite
         (QUIC)    (HTTP Auth)    (REST API)
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
| **健康检查** | 主服务器和边缘节点均暴露 `/health` 端点 |

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

## 项目结构

```
src/
├── HysteriaAuth.Master/    # 主服务器
│   ├── Controllers/        # API 控制器
│   ├── Services/           # 业务逻辑
│   ├── Repositories/       # 数据访问
│   ├── Models/             # 实体、DTO、视图模型
│   ├── Data/               # EF Core 上下文和迁移
│   └── Middleware/         # 认证、异常、限流中间件
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
    ├── deploy-master.sh
    ├── deploy-agent-provisioned.sh
    ├── hysteria-auth-master.service
    ├── hysteria-auth-agent.service
    ├── nginx-master.conf
    ├── backup-db.sh
    └── restore-db.sh

docker/                     # Docker 支持
    ├── Dockerfile.master
    ├── Dockerfile.agent
    └── docker-compose.yml
```

## 技术栈

| 组件 | 技术选型 |
|------|----------|
| 开发语言 | C# (.NET 8.0) |
| Web 框架 | ASP.NET Core |
| 数据库 | SQLite (EF Core) |
| 认证 | JWT (HMAC-SHA256) + BCrypt |
| 测试 | xUnit + Moq + FluentAssertions |

## 文档导航

| 文档 | 路径 |
|------|------|
| 架构文档总目录 | [document/architect/README.md](document/architect/README.md) |
| 后端开发规范 | [document/develop/backend-development-spec.md](document/develop/backend-development-spec.md) |
| 开发阶段文档 | [document/developStage/00-README.md](document/developStage/00-README.md) |
| 快速开始指南 | [document/quick-start.md](document/quick-start.md) |

## 部署清单

| 交付项 | 路径 |
|--------|------|
| 主服务器发布包 | `publish/master/` |
| Edge Agent 发布包 | `publish/agent/` |
| 部署脚本-主服务器 | `scripts/deploy-master.sh` |
| 部署脚本-令牌方式 | `scripts/deploy-agent-provisioned.sh` |
| systemd-主服务器 | `scripts/hysteria-auth-master.service` |
| systemd-Edge Agent | `scripts/hysteria-auth-agent.service` |
| Nginx 配置 | `scripts/nginx-master.conf` |
| 备份脚本 | `scripts/backup-db.sh` |
| 恢复脚本 | `scripts/restore-db.sh` |
| Docker 支持 | `docker/` |
