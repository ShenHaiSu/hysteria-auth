import type { Database } from "bun:sqlite";
import type { ActiveUser, AuthUserInfo } from "@/composable/auth/Auth";

/**
 * 根据来源 IP 判断节点服务器是否处于激活状态
 * @param db 数据库连接
 * @param ip 来源 IP 地址
 * @returns 匹配到的节点服务器 ID，未匹配返回 null
 */
export function findActiveNodeServerByIp(db: Database, ip: string): number | null {
  const stmt = db.query("SELECT id FROM node_server WHERE ip_address = $ip AND is_active = 1 LIMIT 1");
  const row = stmt.get({ $ip: ip }) as { id: number } | undefined;
  return row?.id ?? null;
}

/**
 * 通过用户的代理密码查找激活状态的用户
 * @param db 数据库连接
 * @param proxyPassword 代理密码（非登录密码/混淆密码）
 * @returns 用户记录（包含 id 与 username），未匹配返回 null
 */
export function findActiveUserByProxyPassword(db: Database, proxyPassword: string): ActiveUser | null {
  const stmt = db.query("SELECT id, username FROM users WHERE proxy_password = $pw AND is_active = 1 LIMIT 1");
  const row = stmt.get({ $pw: proxyPassword }) as ActiveUser | undefined;
  return row ?? null;
}

/**
 * 根据用户名与 MD5 登录口令查找激活用户
 * @param db 数据库连接
 * @param username 用户名
 * @param md5 登录口令 MD5
 * @returns 用户基础信息或 null
 */
export function findUserForLogin(db: Database, username: string, md5: string): AuthUserInfo | null {
  const stmt = db.query(
    `SELECT id, username, permission, is_active, proxy_expire_ts, last_login_ts
     FROM users
     WHERE username = $u AND login_password_md5 = $m AND is_active = 1
     LIMIT 1`
  );
  const row = stmt.get({ $u: username, $m: md5 }) as AuthUserInfo | undefined;
  return row ?? null;
}

/**
 * 更新用户登录审计信息
 * @param db 数据库连接
 * @param id 用户 ID
 * @param ip 登录来源 IP
 */
export function updateLoginAudit(db: Database, id: number, ip: string): void {
  const nowSec = Math.floor(Date.now() / 1000);
  const stmt = db.query(`UPDATE users SET last_login_ts = $ts, last_login_ip = $ip, updated_at = datetime('now') WHERE id = $id`);
  stmt.run({ $ts: nowSec, $ip: ip, $id: id });
}

/**
 * 统计用户表总行数
 * @param db 数据库连接
 * @returns 用户总数
 */
export function countUsers(db: Database): number {
  const stmt = db.query("SELECT COUNT(*) as count FROM users");
  const row = stmt.get() as { count: number };
  return row.count;
}

/**
 * 创建初始管理员用户
 * @param db 数据库连接
 * @param username 用户名
 * @param proxyPassword 代理密码
 * @param loginPasswordMd5 登录密码 MD5 (此处存为 'empty')
 */
export function createInitialAdmin(db: Database, username: string, proxyPassword: string): void {
  const stmt = db.query(
    `INSERT INTO users (email, username, login_password_md5, proxy_password, is_active, permission, register_ts)
     VALUES ($email, $username, 'empty', $proxyPassword, 1, 'admin', $ts)`
  );
  stmt.run({
    $email: `${username}@internal`,
    $username: username,
    $proxyPassword: proxyPassword,
    $ts: Math.floor(Date.now() / 1000),
  });
}

/**
 * 查找指定用户名的用户（包含登录密码字段，用于初始化校验）
 * @param db 数据库连接
 * @param username 用户名
 * @returns 用户基础信息及密码或 null
 */
export function findUserWithPasswordByUsername(db: Database, username: string): (AuthUserInfo & { login_password_md5: string }) | null {
  const stmt = db.query(
    `SELECT id, username, permission, is_active, proxy_expire_ts, last_login_ts, login_password_md5
     FROM users WHERE username = $u LIMIT 1`
  );
  const row = stmt.get({ $u: username }) as (AuthUserInfo & { login_password_md5: string }) | undefined;
  return row ?? null;
}

/**
 * 更新用户密码
 * @param db 数据库连接
 * @param id 用户 ID
 * @param md5 新的 MD5 密码
 */
export function updateUserPassword(db: Database, id: number, md5: string): void {
  const stmt = db.query(`UPDATE users SET login_password_md5 = $m, updated_at = datetime('now') WHERE id = $id`);
  stmt.run({ $m: md5, $id: id });
}

/**
 * 根据 ID 查询用户基础信息（用于 /users/me）
 * @param db 数据库连接
 * @param id 用户 ID
 * @returns 用户基础信息或 null
 */
export function getUserInfoById(db: Database, id: number): AuthUserInfo | null {
  const stmt = db.query(
    `SELECT id, username, permission, is_active, proxy_expire_ts, last_login_ts
     FROM users WHERE id = $id LIMIT 1`
  );
  const row = stmt.get({ $id: id }) as AuthUserInfo | undefined;
  return row ?? null;
}
