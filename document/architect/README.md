# Hysteria 认证后端系统 — 架构文档目录

> **文档版本**: v1.2 | **更新日期**: 2026-05-22

本目录包含 Hysteria 认证后端系统的完整技术设计文档。以下为文档索引，按逻辑模块组织：

---

## 📋 文档结构

| #   | 文档                                                                       | 说明                                                                                      |
| --- | -------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------- |
| 1   | [`overview.md`](overview.md)                                               | **项目概述与开发计划** — 项目目标、技术栈、核心功能、开发 Phase 规划                      |
| 2   | [`system-architecture.md`](system-architecture.md)                         | **系统架构设计** — 整体架构图、主服务器架构、边缘节点架构、SPA 静态文件托管               |
| 3   | [`database-design.md`](database-design.md)                                 | **数据库设计** — ER 图、全部 9 张表结构详细设计                                           |
| 4   | [`api-design.md`](api-design.md)                                           | **API 接口设计** — 认证/用户/节点/管理/健康检查 API 完整定义                              |
| 5   | [`edge-node-design.md`](edge-node-design.md)                               | **边缘节点与 Hysteria 集成** — Edge Agent 架构、认证代理、流量采集实现、Hysteria 配置模板 |
| 6   | [`security-design.md`](security-design.md)                                 | **安全设计** — 认证安全、数据安全、网络安全、速率限制、CORS、密钥管理                     |
| 7   | [`project-structure-configuration.md`](project-structure-configuration.md) | **项目结构与配置说明** — 目录结构、主服务器/Agent 完整配置项、SPA 配置节                  |
| 8   | [`deployment.md`](deployment.md)                                           | **部署方案与数据备份** — 主服务器/边缘节点部署脚本、Nginx 反向代理、交叉编译、备份恢复    |
| 9   | [`spa-integration.md`](spa-integration.md)                                 | **SPA 前端集成设计** — Vue3+Vite7 静态文件托管、兜底路由、路径解析、构建脚本              |
| 10  | [`traffic-statistics.md`](traffic-statistics.md)                           | **流量统计与会话管理** — 方案选型、采集流程、流量检查、会话生命周期、并发控制             |
| 11  | [`resilience-monitoring.md`](resilience-monitoring.md)                     | **异常恢复、监控与测试** — 网络异常降级、日志体系、SLO 指标、测试策略                     |
| 12  | [`scalability.md`](scalability.md)                                         | **扩展性考虑** — 数据库迁移、水平扩展、未来功能规划                                       |
| 13  | [`appendix.md`](appendix.md)                                               | **附录** — Hysteria 协议参考、流量统计 API 参考、常用命令、文档修订历史                   |

---

## 📎 关联文档

以下文档位于其他目录，与本架构文档关联密切：

| 文档                    | 位置                                                                                     | 说明                                         |
| ----------------------- | ---------------------------------------------------------------------------------------- | -------------------------------------------- |
| 后端开发规范            | [`../develop/backend-development-spec.md`](../develop/backend-development-spec.md)       | 全部后端开发的强制编码规范                   |
| Hysteria 服务端配置参考 | [`../hysteria/hysteria-server-config.md`](../hysteria/hysteria-server-config.md)         | Hysteria 2 完整 YAML 配置项                  |
| Hysteria 流量统计 API   | [`../hysteria/hysteria-traffic-stats-api.md`](../hysteria/hysteria-traffic-stats-api.md) | trafficStats / online / kick / dump API 详解 |

---

## 🗺️ 阅读导航

### 首次阅读建议

```
概述 + 架构  ──→  数据库  ──→  API  ──→  边缘节点  ──→  SPA 集成
overview.md      database    api        edge-node     spa-integration
system-arch.md   design.md   design.md  design.md     .md

                                       ↓
流量统计 ← 安全 ← 配置 ← 部署 ← 监控测试 ← 扩展性 ← 附录
traffic-   security  project  deployment  resilience  scalability  appendix
statistics .md       -struct  .md         -monitoring .md          .md
.md                  ure.md               .md
```

### 按角色阅读

| 角色                | 推荐文档                                                                                                    |
| ------------------- | ----------------------------------------------------------------------------------------------------------- |
| **架构师**          | overview → system-architecture → database-design → api-design → scalability                                 |
| **后端开发**        | api-design → database-design → traffic-statistics → project-structure-configuration → resilience-monitoring |
| **前端/管理端开发** | api-design（§4.2~4.4）→ spa-integration → security-design（§6.4~6.5）                                       |
| **运维/DevOps**     | deployment → spa-integration → security-design → resilience-monitoring（§13）→ appendix                     |
| **安全审计**        | security-design（全部）→ api-design（§4.0 错误处理）→ resilience-monitoring（§13）                          |

---

## 📝 约定

- 所有文档使用 Markdown 格式，内嵌 Mermaid 图表
- 文件内交叉引用使用相对路径链接 `[显示文本](filename.md#anchor)`
- 代码块标注语言类型以启用语法高亮
- 配置示例使用 JSON/YAML/Bash/PowerShell/C#/Nginx 等标注
