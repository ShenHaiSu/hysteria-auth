# Hysteria Auth 快速开始指南

> 5 分钟内在本地或服务器上运行 Hysteria Auth 认证后端系统。

## 前提条件

- .NET 8.0 SDK (开发) 或 .NET 8.0 Runtime (部署)
- Ubuntu 20.04+ / Debian 11+
- 至少 1GB RAM (Master) / 256MB RAM (Agent)
- 至少 10GB 磁盘空间

## 1. 克隆并构建

```bash
git clone <repository_url> hysteria-auth
cd hysteria-auth/server-dev

# 还原依赖并构建
dotnet build

# 运行测试（验证环境正确）
dotnet test
```

## 2. 配置主服务器

编辑 `src/HysteriaAuth.Master/appsettings.json`，修改以下关键配置：

```json
{
    "Jwt": {
        "Secret": "生成一个至少32字符的随机密钥"
    },
    "ConnectionStrings": {
        "DefaultConnection": "Data Source=/var/lib/hysteria-auth/hysteria-auth.db"
    }
}
```

## 3. 配置边缘节点

编辑 `src/HysteriaAuth.Agent/Config/agent.json`，修改以下关键配置：

```json
{
    "NodeId": "edge-node-01",
    "NodeName": "我的边缘节点",
    "MasterServerUrl": "https://master.example.com",
    "NodeSecret": "生成一个64位hex密钥"
}
```

## 4. 启动服务

### 方式一：开发环境 (dotnet run)

```bash
# 终端 1: 启动主服务器
dotnet run --project src/HysteriaAuth.Master

# 终端 2: 启动边缘节点 Agent
dotnet run --project src/HysteriaAuth.Agent
```

### 方式二：发布并部署 (生产环境)

```bash
# 发布
dotnet publish src/HysteriaAuth.Master -c Release -o publish/master
dotnet publish src/HysteriaAuth.Agent -c Release -o publish/agent

# 一键部署主服务器
bash scripts/deploy-master.sh

# 令牌方式部署 Agent（推荐）
# 首先在主服务器上预注册节点获取令牌
bash scripts/deploy-agent-provisioned.sh <provision_token> <master_url>
```

## 5. 验证部署

```bash
# 健康检查
curl http://127.0.0.1:5000/health

# 预期响应:
# {
#     "status": "healthy",
#     "version": "1.0.0",
#     ...
# }

# 检查服务状态
sudo systemctl status hysteria-auth-master
```

## 6. 创建管理员

部署后需要通过 API 创建初始管理员账号（或使用数据库直接插入）：

```bash
# 创建管理员（需要在 appsettings.json 中配置初始管理员）
curl -X POST http://127.0.0.1:5000/api/v1/admin/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"admin123"}'
```

## 7. 创建第一个用户

```bash
# 获取管理员 Token
TOKEN=$(curl -s -X POST http://127.0.0.1:5000/api/v1/admin/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"admin123"}' | jq -r '.token')

# 创建用户
curl -X POST http://127.0.0.1:5000/api/v1/users \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "username": "testuser",
    "password": "userpassword123",
    "totalTrafficBytes": 10737418240,
    "isActive": true
  }'
```

## 8. 连接 Hysteria 客户端

在 Hysteria 客户端配置中使用：

```json
{
    "server": "edge-node-ip:port",
    "auth": "dGVzdHVzZXI6dXNlcnBhc3N3b3JkMTIz"
}
```

其中 `auth` 是 `username:password` 的 Base64 编码：
```bash
echo -n "testuser:userpassword123" | base64
```

## 下一步

- 阅读 [API 文档](architect/api-design.md)
- 配置 [Nginx 反向代理](architect/deployment.md#3-nginx-反向代理配置)
- 配置 [Hysteria 服务端](architect/edge-node-design.md#6-hysteria-服务端配置-yaml)
- 了解 [安全最佳实践](architect/security-design.md)

## 常见问题

### 数据库在哪里？
SQLite 数据库文件默认位于 `/var/lib/hysteria-auth/hysteria-auth.db`

### 如何备份？
```bash
bash scripts/backup-db.sh
```

### 如何查看日志？
```bash
sudo journalctl -u hysteria-auth-master -f
sudo journalctl -u hysteria-auth-agent -f
```
