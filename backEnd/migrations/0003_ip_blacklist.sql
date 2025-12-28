-- IP 黑名单表：用于持久化封禁恶意 IP
CREATE TABLE IF NOT EXISTS ip_blacklist (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    ip TEXT NOT NULL UNIQUE,          -- 被封禁的 IP 地址
    reason TEXT,                      -- 封禁原因 (如: 暴力破解, 蜜罐触发, 恶意 UA)
    blocked_at DATETIME DEFAULT CURRENT_TIMESTAMP, -- 封禁开始时间
    expires_at DATETIME,              -- 封禁过期时间 (NULL 表示永久封禁)
    is_active INTEGER DEFAULT 1       -- 是否激活 (1: 激活, 0: 已解封)
);

CREATE INDEX IF NOT EXISTS idx_ip_blacklist_ip ON ip_blacklist(ip);
CREATE INDEX IF NOT EXISTS idx_ip_blacklist_expires ON ip_blacklist(expires_at);
