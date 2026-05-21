# 附录

> **父文档**: [架构文档目录](README.md) | **关联**: [`../hysteria/hysteria-server-config.md`](../hysteria/hysteria-server-config.md) · [`../hysteria/hysteria-traffic-stats-api.md`](../hysteria/hysteria-traffic-stats-api.md)

---

## A. Hysteria HTTP 认证协议参考

Hysteria 2 的 HTTP 认证插件使用以下协议（详见[官方文档](../hysteria/hysteria-server-config.md#91-http-验证本项目的核心集成方式)）：

```
POST {auth_url}
Content-Type: application/json

{
    "addr": "123.123.123.123:44556",
    "auth": "base64_encoded_credentials",
    "tx": 52428800
}
```

| 字段 | 类型 | 说明 |
|------|------|------|
| `addr` | string | 客户端地址和端口 |
| `auth` | string | 客户端提交的密码（Base64 编码） |
| `tx` | uint64 | 客户端期望发送速率（字节/秒，服务端视角） |

**成功响应（HTTP 200）：**

```json
{
    "ok": true,
    "id": "username_identifier"
}
```

**失败响应：**

返回任何非 200 的 HTTP 状态码均视为认证失败。

> **与旧版协议的区别**: Hysteria 2 的认证请求体使用 `addr`、`auth`、`tx` 三个字段，而非旧版的 `username`、`password`、`remote_addr`。Hysteria 2 不区分用户名和密码，由认证后端自行解析 `auth` 字段。

---

## B. Hysteria 流量统计 API 参考

详见 [`../hysteria/hysteria-traffic-stats-api.md`](../hysteria/hysteria-traffic-stats-api.md)，四个核心接口：

| 接口 | 方法 | 用途 |
|------|------|------|
| `/traffic` | GET | 查询各用户累计流量，支持 `?clear=1` 清零 |
| `/online` | GET | 查询各用户在线连接数 |
| `/kick` | POST | 踢指定用户下线 |
| `/dump/streams` | GET | 导出所有 TCP 流详情 |

---

## C. 常用命令

```bash
# 发布主服务器
dotnet publish src/HysteriaAuth.Master -c Release -o publish/master

# 发布 Edge Agent
dotnet publish src/HysteriaAuth.Agent -c Release -o publish/agent

# 运行主服务器（开发环境）
dotnet run --project src/HysteriaAuth.Master

# 运行 Edge Agent（开发环境）
dotnet run --project src/HysteriaAuth.Agent

# 创建数据库迁移
dotnet ef migrations add InitialCreate --project src/HysteriaAuth.Master

# 应用数据库迁移
dotnet ef database update --project src/HysteriaAuth.Master

# 手动测试 Hysteria 流量统计 API
curl -H 'Authorization: your_secret' http://127.0.0.1:9999/traffic
curl -H 'Authorization: your_secret' http://127.0.0.1:9999/online

# 手动备份数据库
bash scripts/backup-db.sh

# 手动恢复数据库
bash scripts/restore-db.sh /var/backups/hysteria-auth/hysteria-auth-2025-01-01-120000.db

# 运行测试
dotnet test tests/HysteriaAuth.Tests/HysteriaAuth.Tests.csproj

# 查看测试覆盖率
dotnet test tests/HysteriaAuth.Tests/HysteriaAuth.Tests.csproj \
    --collect:"XPlat Code Coverage" \
    --results-directory:./coverage
```

---

## D. 文档修订历史

| 日期 | 版本 | 变更说明 |
|------|------|----------|
| 2026-05-21 | v1.1 | 大规模补全：新增管理员表和审计日志表、健康检查 API、统一错误响应格式、会话生命周期管理、并发控制与数据一致性、密钥轮换策略、CORS/速率限制配置、数据备份恢复、SLO 指标、测试策略、Edge Agent 初始化流程、管理员管理 API；**文档拆分**：将原单一 `architecture-design.md` 拆分为 12 个子文档 + 目录索引 |
| — | v1.0 | 初始版本 |
