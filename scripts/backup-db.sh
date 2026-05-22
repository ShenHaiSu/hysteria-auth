#!/bin/bash
# backup-db.sh
# SQLite 数据库备份脚本
set -e

BACKUP_DIR="${BACKUP_DIR:-/var/backups/hysteria-auth}"
DB_PATH="${DB_PATH:-/var/lib/hysteria-auth/hysteria-auth.db}"
RETENTION_DAYS="${RETENTION_DAYS:-30}"

mkdir -p "$BACKUP_DIR"

BACKUP_FILE="$BACKUP_DIR/hysteria-auth-$(date +%Y-%m-%d-%H%M%S).db"

echo "正在备份 SQLite 数据库..."

# 执行在线备份（SQLite .backup 命令）
sqlite3 "$DB_PATH" ".backup '$BACKUP_FILE'"

echo "备份完成: $BACKUP_FILE"

# 清理过期备份
DELETED=$(find "$BACKUP_DIR" -name "hysteria-auth-*.db" -mtime +$RETENTION_DAYS -delete -print | wc -l)
echo "清理过期备份: $DELETED 个文件"

echo "备份任务完成于 $(date -u +'%Y-%m-%dT%H:%M:%SZ')"
