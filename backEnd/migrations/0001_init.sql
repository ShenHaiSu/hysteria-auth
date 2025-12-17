-- 初始化基础表
CREATE TABLE IF NOT EXISTS users (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  name TEXT NOT NULL,
  email TEXT NOT NULL UNIQUE,
  created_at TEXT DEFAULT (datetime('now'))
);
ALTER TABLE users ADD COLUMN username TEXT;
ALTER TABLE users ADD COLUMN login_password_md5 TEXT;
ALTER TABLE users ADD COLUMN proxy_password TEXT;
ALTER TABLE users ADD COLUMN register_ip TEXT;
ALTER TABLE users ADD COLUMN last_login_ip TEXT;
ALTER TABLE users ADD COLUMN register_ts INTEGER DEFAULT (strftime('%s','now'));
ALTER TABLE users ADD COLUMN last_login_ts INTEGER;
ALTER TABLE users ADD COLUMN is_active INTEGER DEFAULT 1;
ALTER TABLE users ADD COLUMN proxy_expire_ts INTEGER;
ALTER TABLE users ADD COLUMN updated_at TEXT;
CREATE UNIQUE INDEX IF NOT EXISTS idx_users_username ON users(username);
CREATE INDEX IF NOT EXISTS idx_users_last_login_ts ON users(last_login_ts);
CREATE INDEX IF NOT EXISTS idx_users_proxy_expire_ts ON users(proxy_expire_ts);

