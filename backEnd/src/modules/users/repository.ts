import type { Database } from "bun:sqlite";
import type { User } from "@/composable/users/User";

/**
 * 生成随机代理密码
 * @param length 长度
 * @returns 随机字符串
 */
function generateProxyPassword(length: number = 20): string {
  const chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
  let result = "";
  for (let i = 0; i < length; i++) {
    result += chars.charAt(Math.floor(Math.random() * chars.length));
  }
  return result;
}

/**
 * 查询全部用户
 * @param db 数据库连接
 * @returns 用户列表
 */
export function listUsers(db: Database): User[] {
  const stmt = db.query("SELECT * FROM users ORDER BY id DESC");
  return stmt.all() as User[];
}

/**
 * 根据 ID 查询用户
 * @param db 数据库连接
 * @param id 用户 ID
 * @returns 用户或 null
 */
export function getUser(db: Database, id: number): User | null {
  const stmt = db.query("SELECT * FROM users WHERE id = $id LIMIT 1");
  const row = stmt.get({ $id: id }) as User | undefined;
  return row ?? null;
}

/**
 * 创建用户
 * @param db 数据库连接
 * @param username 用户名
 * @param email 邮箱
 * @returns 新用户
 */
export function createUser(db: Database, username: string, email: string): User {
  const proxyPassword = generateProxyPassword(20);
  const now = Math.floor(Date.now() / 1000);

  const insert = db.query(`
    INSERT INTO users (
      username, email, login_password_md5, proxy_password, 
      permission, is_active, proxy_expire_ts, register_ts
    ) VALUES (
      $username, $email, 'empty', $proxyPassword, 
      'user', 1, 0, $register_ts
    )
  `);

  insert.run({
    $username: username,
    $email: email,
    $proxyPassword: proxyPassword,
    $register_ts: now,
  });

  const id = db.query("SELECT last_insert_rowid() as id").get() as { id: number };
  return getUser(db, id.id) as User;
}

/**
 * 更新用户
 * @param db 数据库连接
 * @param id 用户 ID
 * @param username 用户名
 * @param email 邮箱
 * @returns 更新后的用户或 null
 */
export function updateUser(db: Database, id: number, username: string, email: string): User | null {
  const update = db.query(`
    UPDATE users 
    SET username = $username, email = $email, updated_at = datetime('now') 
    WHERE id = $id
  `);
  update.run({ $username: username, $email: email, $id: id });
  return getUser(db, id);
}

/**
 * 删除用户
 * @param db 数据库连接
 * @param id 用户 ID
 * @returns 是否删除成功
 */
export function deleteUser(db: Database, id: number): boolean {
  const del = db.query("DELETE FROM users WHERE id = $id");
  const res = del.run({ $id: id });
  return (res.changes ?? 0) > 0;
}
