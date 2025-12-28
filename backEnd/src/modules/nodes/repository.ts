import type { Database } from "bun:sqlite";
import type { NodeServer, NodeFilters, ProxyConfigItem } from "@/composable/nodes/NodeServer";

/**
 * 按条件筛选节点列表
 * @param db 数据库连接
 * @param filters 查询条件
 * @returns 节点列表
 */
export function listNodes(db: Database, filters: NodeFilters = {}): NodeServer[] {
  const where: string[] = [];
  const bindings: Array<string | number | null> = [];
  if (filters.server_group) {
    where.push("server_group = ?");
    bindings.push(filters.server_group);
  }
  if (filters.ip_address) {
    where.push("ip_address = ?");
    bindings.push(filters.ip_address);
  }
  if (filters.domain) {
    where.push("domain = ?");
    bindings.push(filters.domain);
  }
  if (typeof filters.is_active === "number") {
    where.push("is_active = ?");
    bindings.push(filters.is_active);
  }
  if (typeof filters.expire_from === "number") {
    where.push("(expire_ts IS NULL OR expire_ts = 0 OR expire_ts >= ?)");
    bindings.push(filters.expire_from);
  }
  if (typeof filters.expire_to === "number") {
    where.push("(expire_ts IS NULL OR expire_ts = 0 OR expire_ts <= ?)");
    bindings.push(filters.expire_to);
  }
  const sql =
    "SELECT id, ip_address, domain, server_group, salamander, idc_name, rent_ts, expire_ts, fee, proxy_port, server_port, note1, note2, note3, note4, is_active, created_at, updated_at FROM node_server" +
    (where.length ? ` WHERE ${where.join(" AND ")}` : "") +
    " ORDER BY id DESC";
  const stmt = db.query(sql);
  return stmt.all(...(bindings as [string | number | null])) as NodeServer[];
}

/**
 * 新增节点
 * @param db 数据库连接
 * @param node 节点数据
 * @returns 新节点
 */
export function createNode(db: Database, node: Omit<NodeServer, "id" | "created_at" | "updated_at">): NodeServer {
  const insert = db.query(
    `INSERT INTO node_server (ip_address, domain, server_group, salamander, idc_name, rent_ts, expire_ts, fee, proxy_port, server_port, note1, note2, note3, note4, is_active)
     VALUES ($ip, $dom, $group, $salamander, $idc, $rent, $exp, $fee, $proxy_port, $server_port, $note1, $note2, $note3, $note4, $active)`
  );
  insert.run({
    $ip: node.ip_address,
    $dom: node.domain ?? null,
    $group: node.server_group,
    $salamander: node.salamander,
    $idc: node.idc_name ?? null,
    $rent: node.rent_ts ?? Math.floor(Date.now() / 1000),
    $exp: node.expire_ts ?? null,
    $fee: node.fee ?? 0,
    $proxy_port: node.proxy_port,
    $server_port: node.server_port ?? null,
    $note1: node.note1 ?? null,
    $note2: node.note2 ?? null,
    $note3: node.note3 ?? null,
    $note4: node.note4 ?? null,
    $active: node.is_active ?? 1,
  });
  const idRow = db.query("SELECT last_insert_rowid() as id").get() as { id: number };
  const stmt = db.query(
    `SELECT id, ip_address, domain, server_group, salamander, idc_name, rent_ts, expire_ts, fee, proxy_port, server_port, note1, note2, note3, note4, is_active, created_at, updated_at
     FROM node_server WHERE id = $id LIMIT 1`
  );
  return stmt.get({ $id: idRow.id }) as NodeServer;
}

/**
 * 更新节点
 * @param db 数据库连接
 * @param id 节点 ID
 * @param patch 要更新的字段
 * @returns 更新后的节点或 null
 */
export function updateNode(db: Database, id: number, patch: Partial<NodeServer>): NodeServer | null {
  const fields: string[] = [];
  const params: Record<string, string | number | null> = { $id: id };
  const candidates: Array<keyof NodeServer> = [
    "ip_address",
    "domain",
    "server_group",
    "salamander",
    "idc_name",
    "rent_ts",
    "expire_ts",
    "fee",
    "proxy_port",
    "server_port",
    "note1",
    "note2",
    "note3",
    "note4",
    "is_active",
  ];
  for (const key of candidates) {
    if (key in patch) {
      const col = key;
      const p = `$${col}`;
      fields.push(`${col} = ${p}`);
      params[p] = (patch as any)[key] ?? null;
    }
  }
  if (fields.length === 0) return getNodeById(db, id);
  const sql = `UPDATE node_server SET ${fields.join(", ")} WHERE id = $id`;
  db.query(sql).run(params);
  return getNodeById(db, id);
}

/**
 * 根据 ID 查询节点
 * @param db 数据库连接
 * @param id 节点 ID
 * @returns 节点或 null
 */
export function getNodeById(db: Database, id: number): NodeServer | null {
  const stmt = db.query(
    `SELECT id, ip_address, domain, server_group, salamander, idc_name, rent_ts, expire_ts, fee, proxy_port, server_port, note1, note2, note3, note4, is_active, created_at, updated_at
     FROM node_server WHERE id = $id LIMIT 1`
  );
  const row = stmt.get({ $id: id }) as NodeServer | undefined;
  return row ?? null;
}

/**
 * 删除节点
 * @param db 数据库连接
 * @param id 节点 ID
 * @returns 是否删除成功
 */
export function deleteNode(db: Database, id: number): boolean {
  const res = db.query(`DELETE FROM node_server WHERE id = $id`).run({ $id: id });
  return (res.changes ?? 0) > 0;
}

/**
 * 查询可用的代理配置节点（启用且未到期）
 * @param db 数据库连接
 * @returns 代理配置项数组
 */
export function listActiveProxyConfig(db: Database): ProxyConfigItem[] {
  const now = Math.floor(Date.now() / 1000);
  const stmt = db.query(
    `SELECT server_group, ip_address, domain, salamander, proxy_port
     FROM node_server
     WHERE is_active = 1 AND (expire_ts IS NULL OR expire_ts = 0 OR expire_ts > $now)
     ORDER BY server_group ASC, id DESC`
  );
  return stmt.all({ $now: now }) as ProxyConfigItem[];
}
