#!/bin/bash
# restore-db.sh
# SQLite 数据库恢复脚本
set -e

BACKUP_FILE="$1"
DB_PATH="${DB_PATH:-/var/lib/hysteria-auth/hysteria-auth.db}"

if [ -z "$BACKUP_FILE" ]; then
    echo "Usage: $0 <backup_file>"
    echo "Example: $0 /var/backups/hysteria-auth/hysteria-auth-2025-01-01-120000.db"
    exit 1
fi

if [ ! -f "$BACKUP_FILE" ]; then
    echo "错误: 备份文件不存在: $BACKUP_FILE"
    exit 1
fi

echo "=== Hysteria Auth 数据库恢复 ==="
echo "备份文件: $BACKUP_FILE"
echo "目标数据库: $DB_PATH"

# 1. 停止主服务
echo "正在停止主服务器服务..."
sudo systemctl stop hysteria-auth-master

# 2. 备份当前数据库（以防恢复出错）
CURRENT_BACKUP="$DB_PATH.bak-$(date +%Y-%m-%d-%H%M%S)"
echo "备份当前数据库: $CURRENT_BACKUP"
cp "$DB_PATH" "$CURRENT_BACKUP"

# 3. 恢复
echo "正在恢复数据库..."
cp "$BACKUP_FILE" "$DB_PATH"
sudo chown www-data:www-data "$DB_PATH"

# 4. 启动主服务
echo "正在启动主服务器服务..."
sudo systemctl start hysteria-auth-master

echo ""
echo "=== 恢复完成 ==="
echo "恢复来源: $BACKUP_FILE"
echo "检查服务状态: sudo systemctl status hysteria-auth-master"
echo "健康检查: curl http://127.0.0.1:5000/health"
