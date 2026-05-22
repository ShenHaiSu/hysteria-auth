# 扩展性考虑

> **父文档**: [架构文档目录](README.md) | **关联**: [`database-design.md`](database-design.md) · [`overview.md`](overview.md)

---

## 1. 数据库扩展

虽然当前使用 SQLite，但通过 EF Core 抽象，未来可以轻松迁移到：

- PostgreSQL（推荐，适合中等规模，支持更高级的并发控制）
- MySQL（适合已有 MySQL 基础设施）
- SQL Server（适合企业环境）

### 迁移要点

1. 修改 `AppDbContext` 的数据库 Provider 配置
2. 更新连接字符串
3. 重新生成 EF Core Migration 并执行
4. SQLite 的 `[Timestamp]` 行版本在 PostgreSQL 中对应 `xmin` 系统列，需要稍作适配

---

## 2. 水平扩展

| 组件 | 扩展方式 |
|------|----------|
| 主服务器 | 多实例 + 负载均衡（需改用共享数据库） |
| 边缘节点 | 无限水平扩展，每个节点独立运行 |
| 数据库 | 读写分离、分库分表 |

### 主服务器水平扩展注意事项

- SQLite 不支持多实例并发写入，扩展为多实例**必须先迁移数据库**为 PostgreSQL/MySQL
- JWT Token 使用 HMAC-SHA256 签名，多实例共享同一 Secret 即可无状态验证
- 节点心跳通过负载均衡器路由到任意实例均可处理
- 速率限制需要从内存模式切换为分布式缓存（如 Redis）

---

## 3. 未来功能规划

- 用户自助门户（查看流量、修改密码）
- 多租户支持
- 计费系统
- Web 管理控制台
- Prometheus/Grafana 监控集成
- 流量预测与自动扩容
- Docker 镜像发布与容器化部署
- gRPC 替代 REST 用于节点通信（更低延迟）
- 流量 QoS 分级（不同用户不同带宽限制）
