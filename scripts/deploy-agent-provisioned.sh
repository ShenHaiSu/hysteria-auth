#!/bin/bash
# deploy-agent-provisioned.sh
# Edge Agent 预注册令牌部署脚本（推荐方式）
# 适用于 Ubuntu 20.04+ / Debian 11+
set -e

# Usage: ./deploy-agent-provisioned.sh <provision_token> <master_server_url>
PROVISION_TOKEN="${1:-}"
MASTER_URL="${2:-}"

if [ -z "$PROVISION_TOKEN" ] || [ -z "$MASTER_URL" ]; then
    echo "Usage: $0 <provision_token> <master_server_url>"
    echo "Example: $0 prov_a1b2c3d4e5f6g7h8i9j0 https://master.example.com"
    exit 1
fi

echo "=== Hysteria Auth Edge Agent 部署脚本（令牌模式）==="

# 1. 安装 .NET Runtime
echo "[1/7] 安装 .NET 8.0 Runtime..."
sudo apt-get update -qq
sudo apt-get install -y -qq dotnet-runtime-8.0

# 2. 创建应用目录
echo "[2/7] 创建目录结构..."
sudo mkdir -p /opt/hysteria-auth/agent
sudo mkdir -p /var/log/hysteria-auth

# 3. 复制应用文件
echo "[3/7] 复制应用文件..."
sudo cp -r publish/* /opt/hysteria-auth/agent/

# 4. 创建最小配置（仅含预注册令牌）
echo "[4/7] 创建 agent.json 配置..."
sudo tee /opt/hysteria-auth/agent/agent.json > /dev/null << EOF
{
    "ProvisionToken": "${PROVISION_TOKEN}",
    "MasterServerUrl": "${MASTER_URL}",
    "AgentVersion": "1.0.0"
}
EOF

# 5. 设置权限
echo "[5/7] 设置权限..."
sudo chown -R root:root /opt/hysteria-auth/agent
sudo chmod 600 /opt/hysteria-auth/agent/agent.json

# 6. 创建 systemd 服务
echo "[6/7] 创建 systemd 服务..."
sudo cp scripts/hysteria-auth-agent.service /etc/systemd/system/

# 7. 启动服务（Agent 将自动完成注册和配置生成）
echo "[7/7] 启动服务..."
sudo systemctl daemon-reload
sudo systemctl enable hysteria-auth-agent
sudo systemctl start hysteria-auth-agent

echo ""
echo "=== 部署完成 ==="
echo "检查服务状态: sudo systemctl status hysteria-auth-agent"
echo "查看日志: sudo journalctl -u hysteria-auth-agent -f"
echo "健康检查: curl http://127.0.0.1:8081/health"
