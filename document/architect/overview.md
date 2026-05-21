# 项目概述与开发计划

> **父文档**: [架构文档目录](README.md) | **关联**: [`system-architecture.md`](system-architecture.md) · [`scalability.md`](scalability.md)

## 0. 文档范围说明

> **本文档仅针对后端架构设计**。Web 管理控制台前端在独立分支单独开发，不在本文档范围内。前端通过调用本文档定义的 REST API 与后端交互。

## 1. 项目概述

### 1.1 项目目标

构建一个基于 C# .NET 的 Hysteria 代理认证后端系统，采用主从架构设计：

- **主服务器（Master Server）**：提供完整的 HTTP API 服务、用户管理、数据存储、认证授权、流量统计
- **边缘节点（Edge Node）**：部署在各 VPS 上，负责本地 Hysteria 服务运行、状态上报、认证请求转发、流量数据采集

### 1.2 技术栈

| 组件 | 技术选型 |
|------|----------|
| 开发语言 | C# (.NET 8.0) |
| Web 框架 | ASP.NET Core |
| 数据库 | SQLite (轻量级，适合中小规模) |
| ORM | Entity Framework Core |
| 部署环境 | Ubuntu / Debian Linux |
| 认证协议 | Hysteria HTTP Auth API |
| 流量采集 | Hysteria [`trafficStats` API](../hysteria/hysteria-traffic-stats-api.md)（方案一：Edge Agent 内部采集） |
| 节点通信 | REST API |
| 系统监控 | `proc` 文件系统（`/proc/stat`、`/proc/meminfo`、`/proc/net/dev`） |
| 日志 | Microsoft.Extensions.Logging + 文件日志 |
| 配置管理 | JSON 配置文件 (`appsettings.json`, `agent.json`) |
| 容器化（可选） | Docker / Docker Compose |

### 1.3 核心功能

1. **用户认证**：Hysteria 客户端连接时，Hysteria 服务端通过 HTTP Auth 向 Edge Agent 发起验证请求；Edge Agent 完成协议转换后转发到主服务器进行身份校验
2. **流量采集统计**（方案一）：Edge Agent 定时调用本地 Hysteria 的 [`trafficStats` API](../hysteria/hysteria-traffic-stats-api.md) 采集各用户流量数据，汇总后上报主服务器，实现精准的流量记录与扣减
3. **流量管理**：记录并限制用户流量使用，支持流量配额、已用流量追踪、超额断连
4. **账号管理**：完整的用户 CRUD 操作、激活/禁用、过期管理、节点白名单
5. **节点监控**：边缘节点实时采集系统指标（CPU、内存、网络）并上报主服务器
6. **在线用户管理**：通过 Hysteria `/online` API 实时掌握各节点在线用户数，支持通过 `/kick` 接口踢用户下线
7. **深度流量审计**：支持通过 Hysteria [`/dump/streams`](../hysteria/hysteria-traffic-stats-api.md#get-dumpstreams--查询-tcp-流详情) API 导出 TCP 流详情，用于故障排查和安全审计
8. **管理员管理**：管理员账号 CRUD、基于 JWT 的身份认证、操作审计日志
9. **健康检查**：主服务器和边缘节点的健康探活接口，支撑外部负载均衡和监控系统
10. **数据可靠性**：流量数据幂等上报、并发扣减保障、数据备份与恢复策略

---

## 2. 开发计划

### Phase 1: 核心功能（2-3 周）

- [ ] 项目初始化
- [ ] 数据库模型和 EF Core 配置（含 Admins、AdminAuditLogs 表）
- [ ] 用户 CRUD API
- [ ] 管理员登录和 CRUD API
- [ ] Hysteria 认证 API（双层设计）
- [ ] Edge Agent 认证代理（含协议转换）
- [ ] 统一错误响应格式中间件

### Phase 2: 节点管理（1-2 周）

- [ ] 节点注册和初始化流程
- [ ] 系统监控模块
- [ ] 状态上报功能
- [ ] 节点管理 API
- [ ] 节点密钥轮换机制
- [ ] 健康检查端点

### Phase 3: 流量统计（1-2 周）

- [ ] Edge Agent 流量采集模块（方案一）
- [ ] 主服务器流量汇总与幂等扣减
- [ ] 在线用户管理与踢用户下线
- [ ] TCP 流详情查询（按需）
- [ ] 会话生命周期管理
- [ ] 并发控制与事务保障

### Phase 4: 完善功能（1 周）

- [ ] 认证缓存
- [ ] 日志记录
- [ ] 审计日志
- [ ] 异常处理和降级
- [ ] 速率限制
- [ ] CORS 配置
- [ ] 数据备份与恢复脚本

### Phase 5: 测试和部署（1 周）

- [ ] 单元测试（≥ 80% 核心模块覆盖率）
- [ ] 集成测试
- [ ] E2E 认证流程测试
- [ ] 部署脚本
- [ ] 文档完善
