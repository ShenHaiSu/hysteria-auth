#!/bin/bash
# deploy-master.sh
# Hysteria Auth Master Server 一键部署脚本
# 适用于 Ubuntu 20.04+ / Debian 11+
set -e

echo "=== Hysteria Auth Master Server 部署脚本 ==="

# 1. 安装 .NET Runtime
echo "[1/6] 安装 .NET 8.0 Runtime..."
sudo apt-get update -qq
sudo apt-get install -y -qq dotnet-runtime-8.0

# 2. 创建应用目录
echo "[2/6] 创建目录结构..."
sudo mkdir -p /opt/hysteria-auth/master
sudo mkdir -p /var/lib/hysteria-auth
sudo mkdir -p /var/log/hysteria-auth
sudo mkdir -p /var/backups/hysteria-auth

# 3. 复制应用文件
echo "[3/6] 复制应用文件..."
sudo cp -r publish/* /opt/hysteria-auth/master/

# 4. 设置权限
echo "[4/6] 设置权限..."
sudo chown -R www-data:www-data /opt/hysteria-auth/master
sudo chown -R www-data:www-data /var/lib/hysteria-auth
sudo chown -R www-data:www-data /var/log/hysteria-auth
sudo chown -R www-data:www-data /var/backups/hysteria-auth

# 5. 创建 systemd 服务
echo "[5/6] 创建 systemd 服务..."
sudo cp scripts/hysteria-auth-master.service /etc/systemd/system/

# 6. 启动服务
echo "[6/6] 启动服务..."
sudo systemctl daemon-reload
sudo systemctl enable hysteria-auth-master
sudo systemctl start hysteria-auth-master

echo ""
echo "=== 部署完成 ==="
echo "检查服务状态: sudo systemctl status hysteria-auth-master"
echo "查看日志: sudo journalctl -u hysteria-auth-master -f"
echo "健康检查: curl http://127.0.0.1:5000/health"
