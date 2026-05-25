# Hysteria Auth 快速开始指南

> 5 分钟内在本地或服务器上运行 Hysteria Auth 认证后端系统。

## 前提条件

### 生产环境 (Linux 部署)
- Ubuntu 20.04+ / Debian 11+
- .NET 8.0/10.0 Runtime
- 至少 1GB RAM (Master) / 256MB RAM (Agent)
- 至少 10GB 磁盘空间

### 开发环境 (Windows)
- Windows 10+ / Windows Server 2016+
- .NET 10.0 SDK
- Node.js 22+ + npm（用于前端 SPA 构建）
- PowerShell 5.1+

---

## 1. 克隆并构建

```bash
git clone <repository_url> hysteria-auth
cd hysteria-auth/server-dev

# 还原依赖并构建
dotnet build

# 运行测试（验证环境正确）
dotnet test
```

---

## 2. 配置主服务器

编辑 [`src/HysteriaAuth.Master/appsettings.json`](src/HysteriaAuth.Master/appsettings.json)，修改以下关键配置：

```json
{
    "Jwt": {
        "Secret": "生成一个至少32字符的随机密钥"
    },
    "ConnectionStrings": {
        "DefaultConnection": "Data Source=/var/lib/hysteria-auth/hysteria-auth.db"
    },
    "Spa": {
        "Enabled": true,
        "StaticFilesPath": "wwwroot",
        "FallbackFile": "index.html"
    }
}
```

> `Spa.StaticFilesPath` 支持相对路径（相对于 `ContentRootPath`）或绝对路径。详见 [`spa-integration.md`](document/architect/spa-integration.md#23-路径解析规则)。

---

## 3. Windows 开发环境一键构建

在 Windows 开发机上，使用 PowerShell 脚本一键构建后端 + 前端 SPA：

```powershell
# 在项目根目录执行
cd server-dev

# 一键构建（默认寻找 ../hysteria-auth-web/dist 作为前端产物）
.\scripts\dev-build.ps1

# 指定前端 dist 路径
.\scripts\dev-build.ps1 -FrontendDistPath "D:\projects\hysteria-auth-web\dist"

# 构建 Debug 版本
.\scripts\dev-build.ps1 -Configuration Debug
```

构建产物输出到 `publish/local-dev/`，包含：

| 产物 | 路径 |
|------|------|
| Master 可执行文件 | `publish/local-dev/HysteriaAuth.Master.exe` |
| Agent 可执行文件 | `publish/local-dev/agent/HysteriaAuth.Agent.exe` |
| SPA 前端静态文件 | `publish/local-dev/wwwroot/` |
| 配置文件 | `publish/local-dev/appsettings.json` |

### 启动本地开发测试

```powershell
# 直接在 Windows 上启动 Master 服务器
cd publish/local-dev
.\HysteriaAuth.Master.exe

# 浏览器访问
# SPA 管理界面: http://localhost:5000
# 健康检查:     http://localhost:5000/health
```

---

## 4. 跨平台编译（Windows → Linux）

在 Windows 开发机上编译出 Linux-x64 可执行程序，用于服务器部署：

```powershell
# 交叉编译 + 打包
.\scripts\publish-linux.ps1

# 指定前端 dist 路径
.\scripts\publish-linux.ps1 -FrontendDistPath "D:\projects\hysteria-auth-web\dist"
```

产物为 `publish/hysteria-auth-linux-x64-{timestamp}.tar.gz`，包含完整的 Master、Agent 和 SPA 前端。

### 部署到 Linux 服务器

```bash
# 上传
scp publish/hysteria-auth-linux-x64-*.tar.gz user@server:/tmp/

# 在服务器上解压部署
ssh user@server
cd /opt/hysteria-auth
sudo tar -xzf /tmp/hysteria-auth-linux-x64-*.tar.gz --strip-components=1
sudo chown -R www-data:www-data /opt/hysteria-auth
sudo systemctl restart hysteria-auth-master
```

---

## 5. 生产环境部署 (Linux)

### 方式一：dotnet publish（手动）

```bash
# 发布
dotnet publish src/HysteriaAuth.Master -c Release -o publish/master
dotnet publish src/HysteriaAuth.Agent -c Release -o publish/agent

# 复制前端 SPA 构建产物
cp -r ../hysteria-auth-web/dist publish/master/wwwroot

# 一键部署主服务器
bash scripts/deploy-master.sh
```

### 方式二：Docker 部署

```bash
cd docker
docker compose up -d
```

### 方式三：预注册令牌部署 Agent（推荐）

```bash
# 令牌方式部署 Agent
# 首先在主服务器上预注册节点获取令牌
bash scripts/deploy-agent-provisioned.sh <provision_token> <master_url>
```

---

## 6. 验证部署

```bash
# 健康检查
curl http://127.0.0.1:5000/health

# 预期响应:
# {
#     "status": "healthy",
#     "version": "1.0.0",
#     ...
# }

# SPA 管理界面
curl http://127.0.0.1:5000/
# 预期返回 index.html 内容

# 检查服务状态
sudo systemctl status hysteria-auth-master
```

---

## 7. 创建管理员

部署后需要通过 API 创建初始管理员账号（种子数据已内置 `admin/admin123`）：

```bash
# 管理员登录获取 Token
curl -X POST http://127.0.0.1:5000/api/v1/admin/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"admin123"}'
```

---

## 8. 创建第一个用户

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

---

## 9. 连接 Hysteria 客户端

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

---

## 下一步

- 阅读 [API 文档](document/architect/api-design.md)
- 阅读 [SPA 集成设计](document/architect/spa-integration.md) — 前端开发与静态文件托管
- 配置 [Nginx 反向代理](document/architect/deployment.md#3-nginx-反向代理配置)
- 配置 [Hysteria 服务端](document/architect/edge-node-design.md#6-hysteria-服务端配置-yaml)
- 了解 [安全最佳实践](document/architect/security-design.md)

---

## 常见问题

### 数据库在哪里？
SQLite 数据库文件默认位于 `/var/lib/hysteria-auth/hysteria-auth.db`（开发环境使用项目根目录）。

### 如何备份？
```bash
bash scripts/backup-db.sh
```

### SPA 界面无法访问？
检查 [`appsettings.json`](src/HysteriaAuth.Master/appsettings.json) 中 `Spa.Enabled` 是否为 `true`，`Spa.StaticFilesPath` 目录下是否存在 `index.html`。启动日志会输出解析后的绝对路径和存在性校验结果。

### Windows 上构建失败？
确保已安装 .NET 10.0 SDK 和 Node.js 22+。若前端 dist 目录不存在，脚本会跳过前端复制并给出提示，Master 服务器仍可正常启动（仅 API 功能）。

### 如何查看日志？
```bash
sudo journalctl -u hysteria-auth-master -f
sudo journalctl -u hysteria-auth-agent -f
```
