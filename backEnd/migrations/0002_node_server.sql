CREATE TABLE IF NOT EXISTS node_server (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  ip_address TEXT NOT NULL,
  domain TEXT,
  server_group TEXT NOT NULL,
  salamander TEXT NOT NULL,
  idc_name TEXT,
  rent_ts INTEGER DEFAULT (strftime('%s','now')),
  expire_ts INTEGER,
  fee REAL DEFAULT 0,
  proxy_port TEXT NOT NULL DEFAULT '443', -- 子节点的代理暴露端口允许 单端口写法 或者 数字-数字的范围端口
  server_port INTEGER,  -- 子节点认证服务器的端口; 只允许单端口,可以不存在; 如果不存在的话就是纯代理节点 有存在的话就是有内置认证服务器的节点, 需要定时同步主从服务器状态.
  note1 TEXT,
  note2 TEXT,
  note3 TEXT,
  note4 TEXT,
  is_active INTEGER DEFAULT 0,
  created_at TEXT DEFAULT (datetime('now')),
  updated_at TEXT
);

CREATE UNIQUE INDEX IF NOT EXISTS idx_node_server_ip_domain ON node_server(ip_address, domain);
CREATE INDEX IF NOT EXISTS idx_node_server_group ON node_server(server_group);
CREATE INDEX IF NOT EXISTS idx_node_server_expire_ts ON node_server(expire_ts);
CREATE INDEX IF NOT EXISTS idx_node_server_is_active ON node_server(is_active);

DROP TRIGGER IF EXISTS trg_node_server_set_updated_at;
CREATE TRIGGER trg_node_server_set_updated_at
AFTER UPDATE ON node_server
BEGIN
  UPDATE node_server
  SET updated_at = datetime('now')
  WHERE id = NEW.id;
END;

DROP TRIGGER IF EXISTS trg_node_server_group_propagate;
CREATE TRIGGER trg_node_server_group_propagate
AFTER UPDATE ON node_server
BEGIN
  UPDATE node_server
  SET
    idc_name = NEW.idc_name,
    salamander = NEW.salamander,
    rent_ts = NEW.rent_ts,
    expire_ts = NEW.expire_ts,
    fee = NEW.fee,
    proxy_port = NEW.proxy_port,
    server_port = NEW.server_port,
    updated_at = datetime('now')
  WHERE server_group = NEW.server_group
    AND id <> NEW.id;
END;
