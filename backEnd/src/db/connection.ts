import { Database } from "bun:sqlite";
import { fileURLToPath } from "node:url";
import path from "node:path";
import fs from "node:fs";

let dbSingleton: Database | null = null;

/**
 * 计算数据库文件的绝对路径
 * @returns 数据库文件绝对路径
 */
export function resolveDbPath(): string {
  const dir = fileURLToPath(new URL("../..", import.meta.url)); // backEnd/
  const dbDir = path.resolve(dir, "dbSet");
  if (!fs.existsSync(dbDir)) {
    fs.mkdirSync(dbDir, { recursive: true });
  }
  return path.resolve(dbDir, "app.db");
}

/**
 * 获取全局的数据库连接实例（单例）
 * @returns Database 实例
 */
export function getDb(): Database {
  if (dbSingleton) return dbSingleton;
  const dbPath = resolveDbPath();
  dbSingleton = new Database(dbPath);
  dbSingleton.run("PRAGMA journal_mode = WAL;");
  return dbSingleton;
}

/**
 * 关闭数据库连接
 */
export function closeDb(): void {
  if (dbSingleton) {
    dbSingleton.close();
    dbSingleton = null;
  }
}

