import type { Database } from "bun:sqlite";
import type { User } from "@/composable/users/User";

/**
 * 查询全部用户
 * @param db 数据库连接
 * @returns 用户列表
 */
export function listUsers(db: Database): User[] {
  const stmt = db.query("SELECT id, name, email, created_at FROM users ORDER BY id DESC");
  return stmt.all() as User[];
}

/**
 * 根据 ID 查询用户
 * @param db 数据库连接
 * @param id 用户 ID
 * @returns 用户或 null
 */
export function getUser(db: Database, id: number): User | null {
  const stmt = db.query("SELECT id, name, email, created_at FROM users WHERE id = $id LIMIT 1");
  const row = stmt.get({ $id: id }) as User | undefined;
  return row ?? null;
}

/**
 * 创建用户
 * @param db 数据库连接
 * @param name 名称
 * @param email 邮箱
 * @returns 新用户
 */
export function createUser(db: Database, name: string, email: string): User {
  const insert = db.query("INSERT INTO users (name, email) VALUES ($name, $email)");
  insert.run({ $name: name, $email: email });
  const id = db.query("SELECT last_insert_rowid() as id").get() as { id: number };
  const stmt = db.query("SELECT id, name, email, created_at FROM users WHERE id = $id LIMIT 1");
  return stmt.get({ $id: id.id }) as User;
}

/**
 * 更新用户
 * @param db 数据库连接
 * @param id 用户 ID
 * @param name 名称
 * @param email 邮箱
 * @returns 更新后的用户或 null
 */
export function updateUser(db: Database, id: number, name: string, email: string): User | null {
  const update = db.query("UPDATE users SET name = $name, email = $email WHERE id = $id");
  update.run({ $name: name, $email: email, $id: id });
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
