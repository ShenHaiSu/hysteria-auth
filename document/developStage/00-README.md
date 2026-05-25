# Hysteria 认证后端 — 分阶段开发落地指南

> **版本**: v1.1 | **日期**: 2026-05-22 | **来源**: [`document/architect/`](../architect/README.md) + [`document/develop/backend-development-spec.md`](../develop/backend-development-spec.md)

---

## 目录

| # | 文档 | 阶段 | 预估工期 | 说明 |
|---|------|------|----------|------|
| 0 | [`00-README.md`](00-README.md) | — | — | 本文档，阶段总览与索引 |
| 1 | [`01-phase1-core-foundation.md`](01-phase1-core-foundation.md) | **Phase 1: 核心基础** | 2-3 周 | 项目初始化、数据库、用户/管理员 CRUD、认证 API、Edge Agent 认证代理、错误中间件 |
| 2 | [`02-phase2-node-management.md`](02-phase2-node-management.md) | **Phase 2: 节点管理** | 1-2 周 | 节点注册、系统监控、心跳上报、节点管理 API、密钥轮换、健康检查 |
| 3 | [`03-phase3-traffic-statistics.md`](03-phase3-traffic-statistics.md) | **Phase 3: 流量统计** | 1-2 周 | 流量采集、汇总扣减、在线用户管理、会话生命周期、并发控制与事务 |
| 4 | [`04-phase4-refinement.md`](04-phase4-refinement.md) | **Phase 4: 完善功能** | 1 周 | 认证缓存、日志审计、异常降级、速率限制、CORS、数据备份 |
| 5 | [`05-phase5-testing-deployment.md`](05-phase5-testing-deployment.md) | **Phase 5: 测试与部署** | 1 周 | 单元测试、集成测试、E2E 测试、部署脚本、文档完善 |
| 6 | [`06-phase6-spa-integration-deployment.md`](06-phase6-spa-integration-deployment.md) | **Phase 6: SPA 集成与部署现代化** | 1 周 | SPA 静态文件托管、.NET 10.0 升级、自包含部署、交叉编译、Docker 多阶段构建 |
| 8 | [`09-phase8-https-auto-detection.md`](09-phase8-https-auto-detection.md) | **Phase 8: HTTPS 证书自动检测** | 0.5~1 天 | 启动时自动检测 cert/ 目录证书文件，存在则启用 HTTPS，缺失则 HTTP + 安全警告 |

---

## 阶段依赖关系

```
Phase 1 ──→ Phase 2 ──→ Phase 3 ──→ Phase 4 ──→ Phase 5
 核心基础    节点管理    流量统计    完善功能    测试部署
               │                         │
               └──────────┬──────────────┘
                          ↓
                      Phase 6 (可与 Phase 4/5 并行)
                   SPA 前端集成与部署现代化
                          │
                          ↓
                      Phase 8 (可与 Phase 7 并行)
                   HTTPS 证书自动检测
```

> **注**: Phase 6（SPA 集成与部署现代化）仅依赖 Phase 1（`Program.cs` 管道就绪），可与 Phase 4/5 并行执行。Phase 8（HTTPS 自动检测）依赖 Phase 6 的路径解析模式，可与 Phase 7 并行。

每个后续阶段启动前，**必须**对前一阶段的关键产物进行审查和验证（详见各阶段文档的「阶段启动前置检查」章节）。

---

## 全局强制规范（贯穿全部阶段）

以下规范来自 [`backend-development-spec.md`](../develop/backend-development-spec.md)，**每个阶段都必须遵守**，在具体阶段文档中不再逐条重复：

| 规范域 | 核心要求 |
|--------|----------|
| **时间戳** | 所有时间戳 ISO 8601 UTC (`yyyy-MM-ddTHH:mm:ssZ`)，使用 `DateTime.UtcNow`，禁止 `DateTime.Now` |
| **分层架构** | Controllers → Services → Repositories，严格分层，不跨层调用 |
| **统一错误响应** | 所有错误返回 `{"error": {"code": "...", "message": "...", "requestId": "..."}}` |
| **密码安全** | BCrypt, Work Factor = 12，用户密码和管理员密码均须遵守 |
| **流量单位** | 所有流量字段以字节 (Bytes) 为单位，`BIGINT` 类型 |
| **软删除** | 用户/管理员删除操作仅设置 `IsActive = false`，不物理删除 |
| **审计日志** | `AdminAuditLogs` 表不可物理删除、不可修改 |
| **HTTPS** | 生产环境强制 HTTPS |
| **日志脱敏** | 禁止日志中输出密码、密钥等敏感信息 |
| **SQL 注入防护** | 使用 EF Core 参数化查询，禁止 SQL 拼接 |

---

## 每个阶段文档的结构约定

每个阶段文档包含以下固定章节：

1. **阶段目标与范围** — 本阶段要完成什么
2. **阶段启动前置检查** — 进入当前阶段前必须验证的上一阶段重点项
3. **具体任务清单** — 可勾选的每日/每周任务拆解
4. **应遵守的规范** — 引用自 backend-development-spec.md 的相关条款
5. **应特别注意的事项** — 易出错、易遗漏的关键点
6. **关键代码模板与示例** — 可直接参考的实现模式
7. **阶段完成标准** — 如何判断阶段已完成
8. **下一阶段交接清单** — 需要传递给下一阶段的信息

---

## 阅读导航

| 角色 | 推荐阅读顺序 |
|------|-------------|
| **开发负责人** | 00 → 01 → 02 → 03 → 04 → 05 → 06（全部） |
| **后端开发 Phase 1** | 00 → 01 |
| **后端开发 Phase 2** | 00 → 01（前置知识）→ 02 |
| **后端开发 Phase 3** | 00 → 01, 02（前置知识）→ 03 |
| **后端开发 Phase 4** | 00 → 01-03（前置知识）→ 04 |
| **后端开发 Phase 6** | 00 → 01（前置知识）→ 06 |
| **前端开发** | 00 → 06（重点关注 SPA 托管配置）+ [`spa-integration.md`](../architect/spa-integration.md) §8 |
| **测试/DevOps** | 00 → 05（同时参阅 01-04 理解功能）→ 06（部署脚本） |
