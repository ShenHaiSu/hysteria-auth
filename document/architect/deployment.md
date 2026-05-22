# 部署方案与数据备份

> **父文档**: [架构文档目录](README.md) | **关联**: [`security-design.md`](security-design.md) · [`project-structure-configuration.md`](project-structure-configuration.md) · [`spa-integration.md`](spa-integration.md)

---

## 1. 主服务器部署

### 1.1 系统要求

- Ubuntu 20.04+ / Debian 11+
- .NET 10.0 Runtime（`--self-contained` 发布后可不需要）
- 至少 1GB RAM
- 至少 10GB 磁盘空间

### 1.2 部署脚本

```bash
#!/bin/bash
# deploy-master.sh

# 1. 创建应用目录
sudo mkdir -p /opt/hysteria-auth/master
sudo mkdir -p /var/lib/hysteria-auth
sudo mkdir -p /var/log/hysteria-auth
sudo mkdir -p /var/backups/hysteria-auth

# 2. 复制应用文件（从 publish 目录）
cp -r publish/* /opt/hysteria-auth/master/

# 3. 确保 SPA 静态文件存在
if [ -d "/opt/hysteria-auth/master/wwwroot" ]; then
    echo "SPA static files found: /opt/hysteria-auth/master/wwwroot"
else
    echo "WARNING: wwwroot/ not found. SPA frontend will not be available."
    echo "Run 'npm run build' in your Vue project and copy dist/* to wwwroot/"
fi

# 4. 设置权限
sudo chown -R www-data:www-data /opt/hysteria-auth/master
sudo chown -R www-data:www-data /var/lib/hysteria-auth
sudo chown -R www-data:www-data /var/log/hysteria-auth
sudo chown -R www-data:www-data /var/backups/hysteria-auth

# 5. 创建 systemd 服务
sudo tee /etc/systemd/system/hysteria-auth-master.service > /dev/null << EOF
[Unit]
Description=Hysteria Auth Master Server
After=network.target

[Service]
Type=notify
User=www-data
Group=www-data
WorkingDirectory=/opt/hysteria-auth/master
ExecStart=/opt/hysteria-auth/master/HysteriaAuth.Master
Restart=always
RestartSec=5
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://127.0.0.1:5000

[Install]
WantedBy=multi-user.target
EOF

# 6. 启动服务
sudo systemctl daemon-reload
sudo systemctl enable hysteria-auth-master
sudo systemctl start hysteria-auth-master
```

---

## 2. 边缘节点 Agent 部署

### 2.1 系统要求

- Ubuntu 20.04+ / Debian 11+
- .NET 10.0 Runtime（`--self-contained` 发布后可不需要）
- 至少 256MB RAM
- 至少 100MB 磁盘空间

### 2.2 部署方式一：预注册令牌部署（推荐）

> 管理员在主服务器上预注册节点，获取启动参数后在边缘节点执行。

**步骤 1：管理员在主服务器预注册**

```bash
# 调用主服务器 API 预注册节点
curl -X POST https://master.example.com/api/v1/admin/nodes/pre-register \
  -H "Authorization: Bearer {admin_token}" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "东京节点",
    "location": "Tokyo, Japan",
    "port": 443,
    "trafficStatsPort": 9999
  }'
```

**响应示例：**

```json
{
    "provisionToken": "prov_a1b2c3d4e5f6g7h8i9j0",
    "masterServerUrl": "https://master.example.com",
    "expiresAt": "2025-12-31T23:59:59Z",
    "startupCommand": "./edge-agent --provision-token=prov_a1b2c3d4e5f6g7h8i9j0 --master-url=https://master.example.com"
}
```

**步骤 2：在边缘节点执行一键启动**

```bash
#!/bin/bash
# deploy-agent-provisioned.sh

# 1. 创建应用目录
sudo mkdir -p /opt/hysteria-auth/agent
sudo mkdir -p /var/log/hysteria-auth

# 2. 复制应用文件
cp -r publish/* /opt/hysteria-auth/agent/

# 3. 创建最小配置（仅含预注册令牌）
sudo tee /opt/hysteria-auth/agent/agent.json > /dev/null << EOF
{
    "ProvisionToken": "prov_a1b2c3d4e5f6g7h8i9j0",
    "MasterServerUrl": "https://master.example.com",
    "AgentVersion": "1.0.0"
}
EOF

# 4. 设置权限
sudo chown -R root:root /opt/hysteria-auth/agent
sudo chmod 600 /opt/hysteria-auth/agent/agent.json

# 5. 创建 systemd 服务
sudo tee /etc/systemd/system/hysteria-auth-agent.service > /dev/null << EOF
[Unit]
Description=Hysteria Auth Edge Agent
After=network.target hysteria-server.service
Requires=hysteria-server.service

[Service]
Type=notify
User=root
Group=root
WorkingDirectory=/opt/hysteria-auth/agent
ExecStart=/opt/hysteria-auth/agent/HysteriaAuth.Agent --provision-token=prov_a1b2c3d4e5f6g7h8i9j0
Restart=always
RestartSec=5
Environment=ASPNETCORE_ENVIRONMENT=Production

[Install]
WantedBy=multi-user.target
EOF

# 6. 启动服务（Edge Agent 将自动完成注册和配置生成）
sudo systemctl daemon-reload
sudo systemctl enable hysteria-auth-agent
sudo systemctl start hysteria-auth-agent
```

### 2.3 部署方式二：手动部署（旧版，保留兼容）

> 适用于测试环境或无法访问主服务器的场景。Edge Agent 将自动生成身份。

```bash
#!/bin/bash
# deploy-agent-legacy.sh

# 1. 创建应用目录
sudo mkdir -p /opt/hysteria-auth/agent
sudo mkdir -p /var/log/hysteria-auth

# 2. 复制应用文件
cp -r publish/* /opt/hysteria-auth/agent/

# 3. 创建配置文件（自动生成 NodeId 和 Secret）
sudo tee /opt/hysteria-auth/agent/agent.json > /dev/null << EOF
{
    "NodeId": "$(uuidgen)",
    "NodeName": "$(hostname)",
    "MasterServerUrl": "https://master.example.com",
    "NodeSecret": "$(openssl rand -hex 32)",
    "AgentVersion": "1.0.0",
    "Init": {
        "RegistrationRetryMax": 5,
        "RegistrationRetryInitialSeconds": 2,
        "RegistrationRetryMaxSeconds": 60
    },
    "AuthProxy": {
        "ListenAddress": "127.0.0.1",
        "ListenPort": 8080
    },
    "TrafficStats": {
        "ListenAddress": "127.0.0.1",
        "ListenPort": 9999,
        "Secret": "$(openssl rand -hex 16)",
        "CollectIntervalSeconds": 30
    },
    "Monitor": {
        "IntervalSeconds": 10,
        "NetworkInterfaces": ["eth0"]
    },
    "Reporter": {
        "IntervalSeconds": 30,
        "RetryCount": 3,
        "RetryDelaySeconds": 5
    },
    "Cache": {
        "Enabled": true,
        "MaxSize": 1000,
        "ExpirationMinutes": 5
    },
    "HealthCheck": {
        "Enabled": true,
        "ListenAddress": "127.0.0.1",
        "ListenPort": 8081
    }
}
EOF

# 4. 设置权限
sudo chown -R root:root /opt/hysteria-auth/agent
sudo chmod 600 /opt/hysteria-auth/agent/agent.json

# 5. 创建 systemd 服务
sudo tee /etc/systemd/system/hysteria-auth-agent.service > /dev/null << EOF
[Unit]
Description=Hysteria Auth Edge Agent
After=network.target hysteria-server.service
Requires=hysteria-server.service

[Service]
Type=notify
User=root
Group=root
WorkingDirectory=/opt/hysteria-auth/agent
ExecStart=/opt/hysteria-auth/agent/HysteriaAuth.Agent
Restart=always
RestartSec=5
Environment=ASPNETCORE_ENVIRONMENT=Production

[Install]
WantedBy=multi-user.target
EOF

# 6. 启动服务
sudo systemctl daemon-reload
sudo systemctl enable hysteria-auth-agent
sudo systemctl start hysteria-auth-agent
```

---

## 3. 跨平台交叉编译

### 3.1 概览

项目支持在 Windows 开发机上编译出 Linux 可执行程序，反之亦然。通过 .NET 的 `--runtime` (RID) 参数和 `--self-contained` 选项实现。

### 3.2 支持的 RID (Runtime Identifier)

| 目标平台 | RID | 说明 |
|----------|-----|------|
| Windows x64 | `win-x64` | Windows 10+ / Windows Server 2016+ |
| Linux x64 | `linux-x64` | Ubuntu 20.04+ / Debian 11+ / CentOS 8+ |
| Linux ARM64 | `linux-arm64` | 树莓派 4+ / ARM 云服务器 |

### 3.3 Windows → Linux 一键编译

使用 PowerShell 脚本 [`scripts/publish-linux.ps1`](../scripts/publish-linux.ps1)：

```powershell
# 在 server-dev 目录执行
.\scripts\publish-linux.ps1

# 指定前端 dist 路径
.\scripts\publish-linux.ps1 -FrontendDistPath "D:\projects\hysteria-auth-web\dist"
```

**输出**：`publish/hysteria-auth-linux-x64-{timestamp}.tar.gz`

**产物内容**：

```
publish/linux-x64/
├── HysteriaAuth.Master          # Master 可执行文件 (Linux ELF)
├── appsettings.json
├── wwwroot/                     # SPA 前端构建产物
│   ├── index.html
│   └── assets/
├── agent/                       # Agent 可执行文件
│   └── HysteriaAuth.Agent
├── libclrjit.so                 # .NET 运行时库
└── *.so / *.dll                 # 其他依赖
```

### 3.4 手动交叉编译命令

```bash
# Windows 上编译 Linux 版本
dotnet publish src/HysteriaAuth.Master -c Release -r linux-x64 --self-contained true -o publish/linux-x64
dotnet publish src/HysteriaAuth.Agent -c Release -r linux-x64 --self-contained true -o publish/linux-x64/agent

# 复制前端 SPA
cp -r ../hysteria-auth-web/dist publish/linux-x64/wwwroot

# 打包
tar -czf hysteria-auth-linux-x64.tar.gz -C publish linux-x64
```

```bash
# Linux 上编译 Windows 版本（通常不需要）
dotnet publish src/HysteriaAuth.Master -c Release -r win-x64 --self-contained true -o publish/win-x64
```

### 3.5 本地开发 Windows 版本编译

```powershell
# 使用 PowerShell 脚本
.\scripts\dev-build.ps1

# 或手动执行
dotnet publish src/HysteriaAuth.Master -c Release -r win-x64 --self-contained true -o publish/local-dev
dotnet publish src/HysteriaAuth.Agent -c Release -r win-x64 --self-contained true -o publish/local-dev/agent

# 复制前端 SPA
if (Test-Path ../hysteria-auth-web/dist) {
    Copy-Item -Recurse ../hysteria-auth-web/dist publish/local-dev/wwwroot
}

# 启动
cd publish/local-dev
.\HysteriaAuth.Master.exe
```

---

## 4. Nginx 反向代理配置

### 4.1 方案 A：ASP.NET Core 托管 SPA（推荐，Nginx 仅反代）

保持 Nginx 配置简洁，所有请求透传到 Kestrel，由 ASP.NET Core 的 `UseStaticFiles` + `MapFallbackToFile` 处理 SPA 兜底。

```nginx
server {
    listen 443 ssl http2;
    server_name master.example.com;

    ssl_certificate /etc/ssl/certs/hysteria-auth.crt;
    ssl_certificate_key /etc/ssl/private/hysteria-auth.key;

    # 所有请求透传到 Kestrel（API + SPA）
    location / {
        proxy_pass http://127.0.0.1:5000;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

### 4.2 方案 B：Nginx 直接托管静态文件（性能更优）

```nginx
server {
    listen 443 ssl http2;
    server_name master.example.com;

    ssl_certificate /etc/ssl/certs/hysteria-auth.crt;
    ssl_certificate_key /etc/ssl/private/hysteria-auth.key;

    # SPA 静态资源（Nginx 直接托管）
    location / {
        root /opt/hysteria-auth/master/wwwroot;
        try_files $uri $uri/ /index.html;

        # 静态资源长缓存
        location ~* \.(js|css|png|jpg|jpeg|gif|ico|svg|woff|woff2|ttf)$ {
            expires 1d;
            add_header Cache-Control "public, immutable";
        }
    }

    # 健康检查（无需认证，不走日志）
    location /health {
        proxy_pass http://127.0.0.1:5000;
        proxy_set_header Host $host;
        access_log off;
    }

    # 认证 API（Edge Agent 调用）
    location /api/v1/auth/ {
        proxy_pass http://127.0.0.1:5000;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }

    # 管理 API
    location /api/v1/admin/ {
        # 可配置 IP 白名单
        # allow 192.168.1.0/24;
        # deny all;

        proxy_pass http://127.0.0.1:5000;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
    }

    # 用户管理 API
    location /api/v1/users/ {
        proxy_pass http://127.0.0.1:5000;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
    }

    # 节点 API
    location /api/v1/nodes/ {
        proxy_pass http://127.0.0.1:5000;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
    }
}
```

> ⚠️ 若使用方案 B，需将 [`appsettings.json`](../src/HysteriaAuth.Master/appsettings.json) 中 `Spa.Enabled` 设为 `false`，避免 ASP.NET Core 和 Nginx 同时处理静态文件。

---

## 5. 数据备份与恢复

### 5.1 自动备份策略

| 参数 | 值 |
|------|-----|
| 备份间隔 | 24 小时（可配置） |
| 备份方式 | SQLite `.backup` 命令（在线热备份） |
| 备份目录 | `/var/backups/hysteria-auth/` |
| 保留天数 | 30 天 |
| 备份命名 | `hysteria-auth-{yyyy-MM-dd-HHmmss}.db` |

### 5.2 备份脚本

```bash
#!/bin/bash
# backup-db.sh

BACKUP_DIR="/var/backups/hysteria-auth"
DB_PATH="/var/lib/hysteria-auth/hysteria-auth.db"
RETENTION_DAYS=30

mkdir -p "$BACKUP_DIR"

# 执行在线备份
sqlite3 "$DB_PATH" ".backup '$BACKUP_DIR/hysteria-auth-$(date +%Y-%m-%d-%H%M%S).db'"

# 清理过期备份
find "$BACKUP_DIR" -name "hysteria-auth-*.db" -mtime +$RETENTION_DAYS -delete

echo "Backup completed at $(date)"
```

### 5.3 恢复脚本

```bash
#!/bin/bash
# restore-db.sh

BACKUP_FILE="$1"
DB_PATH="/var/lib/hysteria-auth/hysteria-auth.db"

if [ -z "$BACKUP_FILE" ]; then
    echo "Usage: restore-db.sh <backup_file>"
    exit 1
fi

if [ ! -f "$BACKUP_FILE" ]; then
    echo "Backup file not found: $BACKUP_FILE"
    exit 1
fi

# 停止主服务
sudo systemctl stop hysteria-auth-master

# 备份当前数据库（以防万一）
cp "$DB_PATH" "$DB_PATH.bak-$(date +%Y-%m-%d-%H%M%S)"

# 恢复
cp "$BACKUP_FILE" "$DB_PATH"
sudo chown www-data:www-data "$DB_PATH"

# 启动主服务
sudo systemctl start hysteria-auth-master

echo "Restore completed from $BACKUP_FILE"
```

### 5.4 数据保留策略

| 数据类型 | 保留期限 | 清理方式 |
|----------|----------|----------|
| `TrafficRecords` | 90 天 | 后台定时任务每日清理 |
| `AuthLogs` | 90 天 | 后台定时任务每日清理 |
| `NodeStatus` | 30 天 | 后台定时任务每日清理 |
| `NodeTraffic` | 90 天 | 后台定时任务每日清理 |
| `AdminAuditLogs` | 365 天 | 后台定时任务每日清理 |
| `Sessions` (closed) | 7 天 | 后台定时任务每日清理 |
| 数据库备份 | 30 天 | 备份脚本自动清理 |

---

## 6. 构建脚本参考

| 脚本 | 平台 | 用途 |
|------|------|------|
| [`scripts/dev-build.ps1`](../scripts/dev-build.ps1) | Windows (PowerShell) | 本地开发构建：编译 win-x64 + 复制前端 + 一键启动 |
| [`scripts/publish-linux.ps1`](../scripts/publish-linux.ps1) | Windows (PowerShell) | 交叉编译：编译 linux-x64 + 打包 tar.gz |
| [`scripts/deploy-master.sh`](../scripts/deploy-master.sh) | Linux (Bash) | 生产环境 Master 部署 |
| [`scripts/deploy-agent-provisioned.sh`](../scripts/deploy-agent-provisioned.sh) | Linux (Bash) | 生产环境 Agent 令牌方式部署 |
