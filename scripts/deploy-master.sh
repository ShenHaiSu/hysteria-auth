#!/bin/bash
# deploy-master.sh
# Hysteria Auth Master Server 一键部署脚本
# 适用于 Ubuntu 20.04+ / Debian 11+
# Phase 6: 自包含发布 (self-contained)，无需安装 .NET Runtime
set -e

echo "=== Hysteria Auth Master Server 部署脚本 ==="

# 1. 创建应用目录
echo "[1/5] 创建目录结构..."
sudo mkdir -p /opt/hysteria-auth/master
sudo mkdir -p /var/lib/hysteria-auth
sudo mkdir -p /var/log/hysteria-auth
sudo mkdir -p /var/backups/hysteria-auth

# 2. 复制应用文件
echo "[2/5] 复制应用文件..."
sudo cp -r publish/* /opt/hysteria-auth/master/

# 3. 检查 SPA 静态文件
echo "[3/5] 检查 SPA 静态文件..."
if [ -d "/opt/hysteria-auth/master/wwwroot" ]; then
    echo "  -> SPA static files found: /opt/hysteria-auth/master/wwwroot"
else
    echo "  -> WARNING: wwwroot/ not found. SPA frontend will not be available."
    echo "     Run 'npm run build' in your Vue project and copy dist/* to wwwroot/"
fi

# 4. 设置权限
echo "[4/5] 设置权限..."
sudo chown -R www-data:www-data /opt/hysteria-auth/master
sudo chown -R www-data:www-data /var/lib/hysteria-auth
sudo chown -R www-data:www-data /var/log/hysteria-auth
sudo chown -R www-data:www-data /var/backups/hysteria-auth

# 5. 创建 systemd 服务 + 启动
echo "[5/5] 创建 systemd 服务并启动..."
sudo cp scripts/hysteria-auth-master.service /etc/systemd/system/

sudo systemctl daemon-reload
sudo systemctl enable hysteria-auth-master
sudo systemctl start hysteria-auth-master

echo ""
echo "=== 部署完成 ==="
echo "检查服务状态: sudo systemctl status hysteria-auth-master"
echo "查看日志: sudo journalctl -u hysteria-auth-master -f"
echo "健康检查: curl http://127.0.0.1:5000/health"
echo "SPA 前端: curl http://127.0.0.1:5000/"
