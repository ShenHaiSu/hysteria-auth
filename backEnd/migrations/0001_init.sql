-- 初始化基础表：用户表
CREATE TABLE IF NOT EXISTS users (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  username TEXT NOT NULL UNIQUE,
  email TEXT NOT NULL UNIQUE,
  login_password_md5 TEXT,
  proxy_password TEXT,
  permission TEXT DEFAULT 'user',
  is_active INTEGER DEFAULT 1,
  proxy_expire_ts INTEGER DEFAULT 0,
  register_ts INTEGER DEFAULT (strftime('%s','now')),
  last_login_ts INTEGER,
  register_ip TEXT,
  last_login_ip TEXT,
  created_at TEXT DEFAULT (datetime('now')),
  updated_at TEXT
);

-- 索引：提高查询效率
CREATE UNIQUE INDEX IF NOT EXISTS idx_users_username ON users(username);
CREATE INDEX IF NOT EXISTS idx_users_last_login_ts ON users(last_login_ts);
CREATE INDEX IF NOT EXISTS idx_users_proxy_expire_ts ON users(proxy_expire_ts);
CREATE INDEX IF NOT EXISTS idx_users_is_active ON users(is_active);
CREATE INDEX IF NOT EXISTS idx_users_permission ON users(permission);

-- 触发器：自动更新 updated_at 时间戳
DROP TRIGGER IF EXISTS trg_users_set_updated_at;
CREATE TRIGGER trg_users_set_updated_at
AFTER UPDATE ON users
BEGIN
  UPDATE users
  SET updated_at = datetime('now')
  WHERE id = NEW.id;
END;

