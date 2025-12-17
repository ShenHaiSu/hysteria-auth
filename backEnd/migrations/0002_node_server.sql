CREATE TABLE IF NOT EXISTS node_server (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  ip_address TEXT NOT NULL,
  domain TEXT,
  salamander TEXT NOT NULL,
  idc_name TEXT,
  rent_ts INTEGER DEFAULT (strftime('%s','now')),
  expire_ts INTEGER,
  fee REAL DEFAULT 0,
  note1 TEXT,
  note2 TEXT,
  note3 TEXT,
  note4 TEXT,
  is_active INTEGER DEFAULT 1,
  created_at TEXT DEFAULT (datetime('now')),
  updated_at TEXT
);
CREATE UNIQUE INDEX IF NOT EXISTS idx_node_server_ip_domain ON node_server(ip_address, domain);
CREATE INDEX IF NOT EXISTS idx_node_server_expire_ts ON node_server(expire_ts);
