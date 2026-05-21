# 部署方案与数据备份

> **父文档**: [架构文档目录](README.md) | **关联**: [`security-design.md`](security-design.md) · [`project-structure-configuration.md`](project-structure-configuration.md)

---

## 1. 主服务器部署

### 1.1 系统要求

- Ubuntu 20.04+ / Debian 11+
- .NET 8.0 Runtime
- 至少 1GB RAM
- 至少 10GB 磁盘空间

### 1.2 部署脚本

```bash
#!/bin/bash
# deploy-master.sh

# 1. 安装 .NET Runtime
sudo apt-get update
sudo apt-get install -y dotnet-runtime-8.0

# 2. 创建应用目录
sudo mkdir -p /opt/hysteria-auth/master
sudo mkdir -p /var/lib/hysteria-auth
sudo mkdir -p /var/log/hysteria-auth
sudo mkdir -p /var/backups/hysteria-auth

# 3. 复制应用文件
cp -r publish/* /opt/hysteria-auth/master/

# 4. 设置权限
sudo chown -r www-data:www-data /opt/hysteria-auth/master
sudo chown -r www-data:www-data /var/lib/hysteria-auth
sudo chown -r www-data:www-data /var/log/hysteria-auth
sudo chown -r www-data:www-data /var/backups/hysteria-auth

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
ExecStart=/usr/bin/dotnet /opt/hysteria-auth/master/HysteriaAuth.Master.dll
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
- .NET 8.0 Runtime
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

# 1. 安装 .NET Runtime
sudo apt-get update
sudo apt-get install -y dotnet-runtime-8.0

# 2. 创建应用目录
sudo mkdir -p /opt/hysteria-auth/agent
sudo mkdir -p /var/log/hysteria-auth

# 3. 复制应用文件
cp -r publish/* /opt/hysteria-auth/agent/

# 4. 创建最小配置（仅含预注册令牌）
sudo tee /opt/hysteria-auth/agent/agent.json > /dev/null << EOF
{
    "ProvisionToken": "prov_a1b2c3d4e5f6g7h8i9j0",
    "MasterServerUrl": "https://master.example.com",
    "AgentVersion": "1.0.0"
}
EOF

# 5. 设置权限
sudo chown -r root:root /opt/hysteria-auth/agent
sudo chmod 600 /opt/hysteria-auth/agent/agent.json

# 6. 创建 systemd 服务
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
ExecStart=/usr/bin/dotnet /opt/hysteria-auth/agent/HysteriaAuth.Agent.dll --provision-token=prov_a1b2c3d4e5f6g7h8i9j0
Restart=always
RestartSec=5
Environment=ASPNETCORE_ENVIRONMENT=Production

[Install]
WantedBy=multi-user.target
EOF

# 7. 启动服务（Edge Agent 将自动完成注册和配置生成）
sudo systemctl daemon-reload
sudo systemctl enable hysteria-auth-agent
sudo systemctl start hysteria-auth-agent
```

### 2.3 部署方式二：手动部署（旧版，保留兼容）

> 适用于测试环境或无法访问主服务器的场景。Edge Agent 将自动生成身份。

```bash
#!/bin/bash
# deploy-agent-legacy.sh

# 1. 安装 .NET Runtime
sudo apt-get update
sudo apt-get install -y dotnet-runtime-8.0

# 2. 创建应用目录
sudo mkdir -p /opt/hysteria-auth/agent
sudo mkdir -p /var/log/hysteria-auth

# 3. 复制应用文件
cp -r publish/* /opt/hysteria-auth/agent/

# 4. 创建配置文件（自动生成 NodeId 和 Secret）
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

# 5. 设置权限
sudo chown -r root:root /opt/hysteria-auth/agent
sudo chmod 600 /opt/hysteria-auth/agent/agent.json

# 6. 创建 systemd 服务
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
ExecStart=/usr/bin/dotnet /opt/hysteria-auth/agent/HysteriaAuth.Agent.dll
Restart=always
RestartSec=5
Environment=ASPNETCORE_ENVIRONMENT=Production

[Install]
WantedBy=multi-user.target
EOF

# 7. 启动服务
sudo systemctl daemon-reload
sudo systemctl enable hysteria-auth-agent
sudo systemctl start hysteria-auth-agent
```

---

## 3. Nginx 反向代理配置

```nginx
server {
    listen 443 ssl http2;
    server_name master.example.com;

    ssl_certificate /etc/ssl/certs/hysteria-auth.crt;
    ssl_certificate_key /etc/ssl/private/hysteria-auth.key;

    # 健康检查（无需认证）
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

---

## 4. 数据备份与恢复

### 4.1 自动备份策略

| 参数 | 值 |
|------|-----|
| 备份间隔 | 24 小时（可配置） |
| 备份方式 | SQLite `.backup` 命令（在线热备份） |
| 备份目录 | `/var/backups/hysteria-auth/` |
| 保留天数 | 30 天 |
| 备份命名 | `hysteria-auth-{yyyy-MM-dd-HHmmss}.db` |

### 4.2 备份脚本

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

### 4.3 恢复脚本

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

### 4.4 数据保留策略

| 数据类型 | 保留期限 | 清理方式 |
|----------|----------|----------|
| `TrafficRecords` | 90 天 | 后台定时任务每日清理 |
| `AuthLogs` | 90 天 | 后台定时任务每日清理 |
| `NodeStatus` | 30 天 | 后台定时任务每日清理 |
| `NodeTraffic` | 90 天 | 后台定时任务每日清理 |
| `AdminAuditLogs` | 365 天 | 后台定时任务每日清理 |
| `Sessions` (closed) | 7 天 | 后台定时任务每日清理 |
| 数据库备份 | 30 天 | 备份脚本自动清理 |
