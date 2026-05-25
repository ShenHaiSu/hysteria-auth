# 系统架构设计

> **父文档**: [架构文档目录](README.md) | **关联**: [`overview.md`](overview.md) · [`edge-node-design.md`](edge-node-design.md) · [`spa-integration.md`](spa-integration.md)

## 1. 整体架构图

```mermaid
graph TB
    subgraph Client[客户端设备]
        C1[Hysteria Client 1]
        C2[Hysteria Client 2]
        C3[Hysteria Client N]
    end

    subgraph Master[主服务器 Master Server]
        API[HTTP API Gateway]
        Auth[认证服务]
        UserMgr[用户管理服务]
        TrafficSvc[流量统计服务]
        DB[(SQLite 数据库)]
        NodeMgr[节点管理服务]
        AdminMgr[管理员服务]
        HealthEP[健康检查端点]
        AuditSvc[审计日志服务]
        SPA[SPA 静态文件托管<br/>UseStaticFiles + Fallback]
    end

    subgraph Edge1[边缘节点 1]
        H1[Hysteria Server<br/>trafficStats :9999]
        Agent1[Edge Agent]
        Mon1[系统监控]
    end

    subgraph Edge2[边缘节点 2]
        H2[Hysteria Server<br/>trafficStats :9999]
        Agent2[Edge Agent]
        Mon2[系统监控]
    end

    subgraph EdgeN[边缘节点 N]
        HN[Hysteria Server<br/>trafficStats :9999]
        AgentN[Edge Agent]
        MonN[系统监控]
    end

    C1 -->|QUIC 连接| H1
    C2 -->|QUIC 连接| H2
    C3 -->|QUIC 连接| HN

    H1 -->|HTTP Auth| Agent1
    H2 -->|HTTP Auth| Agent2
    HN -->|HTTP Auth| AgentN

    Agent1 -->|认证转发 + 流量上报 + 心跳| API
    Agent2 -->|认证转发 + 流量上报 + 心跳| API
    AgentN -->|认证转发 + 流量上报 + 心跳| API

    API --> Auth
    API --> TrafficSvc
    API --> NodeMgr
    API --> AdminMgr
    Auth --> UserMgr
    Auth --> DB
    UserMgr --> DB
    TrafficSvc --> DB
    NodeMgr --> DB
    AdminMgr --> AuditSvc
    AuditSvc --> DB

    H1 -.->|GET /traffic /online| Agent1
    H2 -.->|GET /traffic /online| Agent2
    HN -.->|GET /traffic /online| AgentN

    Mon1 --> Agent1
    Mon2 --> Agent2
    MonN --> AgentN
```

### 架构要点

- **主服务器** 是中心化控制面，承载所有业务逻辑和数据存储
- **边缘节点** 是无状态的转发与采集层，每个节点独立运行 Hysteria Server + Edge Agent
- 客户端通过 **QUIC** 直连边缘节点的 Hysteria Server，不经过主服务器
- 认证请求由 Hysteria Server 通过 **本地回环 HTTP** 交给 Edge Agent，Agent 完成协议转换后转发到主服务器
- 流量数据通过 **定时采集 → 合并心跳上报** 的异步通道汇总到主服务器
- **SPA 静态文件托管**：Master 服务器通过 ASP.NET Core 中间件托管 Vue3+Vite7 管理控制台前端

---

## 2. 主服务器架构

```mermaid
graph LR
    subgraph ASP.NET Core
        subgraph Controllers
            AuthCtrl[认证控制器]
            UserCtrl[用户控制器]
            NodeCtrl[节点控制器]
            AdminCtrl[管理控制器]
            TrafficCtrl[流量控制器]
            HealthCtrl[健康检查控制器]
        end

        subgraph Services
            AuthService[认证服务]
            UserService[用户服务]
            NodeService[节点服务]
            TrafficService[流量服务]
            AdminService[管理员服务]
            AuditService[审计日志服务]
        end

        subgraph Repositories
            UserRepo[用户仓储]
            NodeRepo[节点仓储]
            TrafficRepo[流量仓储]
            LogRepo[日志仓储]
            AdminRepo[管理员仓储]
            AuditLogRepo[审计日志仓储]
        end

        subgraph Middleware
            AuthMW[认证中间件<br/>JWT验证 + 节点密钥]
            ExceptionMW[异常处理中间件]
            RateLimitMW[速率限制中间件]
            AuditMW[审计中间件<br/>记录管理员操作]
        end

        subgraph SPA_MW[SPA 静态文件中间件]
            StaticFiles[UseStaticFiles]
            Fallback[MapFallbackToFile]
        end
    end

    subgraph Data
        SQLite[(SQLite DB)]
    end

    AuthCtrl --> AuthService
    UserCtrl --> UserService
    NodeCtrl --> NodeService
    TrafficCtrl --> TrafficService
    AdminCtrl --> AdminService
    AdminCtrl --> AuditService

    AuthService --> UserRepo
    AuthService --> TrafficRepo
    UserService --> UserRepo
    NodeService --> NodeRepo
    TrafficService --> TrafficRepo
    AdminService --> AdminRepo
    AuditService --> AuditLogRepo

    UserRepo --> SQLite
    NodeRepo --> SQLite
    TrafficRepo --> SQLite
    LogRepo --> SQLite
    AdminRepo --> SQLite
    AuditLogRepo --> SQLite

    AuthMW --> API
    ExceptionMW --> API
    RateLimitMW --> API
    AuditMW --> AdminCtrl
    SPA_MW --> API
```

### 分层职责

| 层 | 职责 |
|----|------|
| **Controllers** | HTTP 请求路由、参数校验、响应格式化 |
| **Services** | 业务逻辑编排、事务管理 |
| **Repositories** | 数据访问抽象，封装 EF Core 操作 |
| **Middleware** | 横切关注点：认证、异常、限流、审计、SPA 静态文件托管 |
| **Data** | SQLite 数据库文件和 EF Core 上下文 |

---

## 3. 边缘节点架构

```mermaid
graph TB
    subgraph EdgeNode[边缘节点]
        subgraph Hysteria
            HS[Hysteria Server<br/>trafficStats :9999]
            HC[hysteria.yaml]
        end

        subgraph EdgeAgent
            EA[Edge Agent 进程]
            SysMon[系统监控模块]
            AuthProxy[认证代理模块]
            TrafficCollector[流量采集模块]
            ReportMgr[上报管理模块]
            InitMgr[初始化注册模块]
            HealthEP[健康检查端点]
        end
    end

    subgraph MasterServer[主服务器]
        API[HTTP API]
    end

    HS -->|HTTP Auth| AuthProxy
    TrafficCollector -->|GET /traffic?clear=1| HS
    TrafficCollector -->|GET /online| HS
    SysMon -->|采集数据| EA
    AuthProxy -->|协议转换 + 转发认证| API
    ReportMgr -->|定时上报 含流量数据| API
    InitMgr -->|启动注册| API

    EA --> SysMon
    EA --> AuthProxy
    EA --> TrafficCollector
    EA --> ReportMgr
    EA --> InitMgr
    EA --> HealthEP
```

### Edge Agent 模块职责

| 模块 | 职责 |
|------|------|
| **系统监控 (SysMon)** | 采集 CPU、内存、网络等系统指标 |
| **认证代理 (AuthProxy)** | 接收本地 Hysteria 认证请求，完成 Hysteria 原生协议 → 内部协议的转换 |
| **流量采集 (TrafficCollector)** | 定时调用 Hysteria `trafficStats` API 获取各用户流量和在线数 |
| **上报管理 (ReportMgr)** | 合并系统指标和流量数据，定时发送心跳到主服务器 |
| **初始化注册 (InitMgr)** | 首次启动时向主服务器注册节点信息 |
| **健康检查 (HealthEP)** | 暴露 `/health` 端点供外部监控探活 |

> 分布式节点通信设计详情见 [`edge-node-design.md`](edge-node-design.md)。

---

## 4. SPA 静态文件托管

> 详细设计见 [`spa-integration.md`](spa-integration.md)

Master 服务器通过 ASP.NET Core 内置的 `UseStaticFiles` + `MapFallbackToFile` 中间件托管 Vue3+Vite7 前端 SPA 的构建产物。中间件管道在 `MapControllers` 之后插入兜底路由，确保 API 请求优先匹配，非 API 请求回退到 `index.html`（支持 Vue Router history 模式）。

### 4.1 核心组件

| 组件 | 说明 |
|------|------|
| `SpaSettings` 配置类 | 强类型绑定 `appsettings.json` 中 `Spa` 节，含 `Enabled`、`StaticFilesPath`、`FallbackFile`、`CacheMaxAgeSeconds` |
| 路径解析器 | 启动时将 `StaticFilesPath`（相对或绝对路径）解析为绝对路径，并校验目录存在性 |
| `UseStaticFiles` | 提供 `wwwroot/`（或自定义路径）下的 JS/CSS/资源文件 |
| `MapFallbackToFile` | 将非 API、非物理文件的请求回退到 `index.html` |

### 4.2 中间件管道路由优先级

```
1. MapControllers    → /api/v1/*        (API 控制器)
2. UseStaticFiles    → /assets/*        (物理静态文件)
3. MapFallbackToFile → /admin/users     (SPA 兜底 → index.html)
```

### 4.3 路径配置示例

| 配置值 | 说明 |
|--------|------|
| `"wwwroot"` | 默认，相对于 ContentRootPath |
| `"/var/www/spa"` | Linux 绝对路径 |
| `"D:\\WebUI\\dist"` | Windows 绝对路径 |
| `"../frontend/dist"` | 相对路径，指向上级目录 |
