import { getDb } from "@/db/connection";
import { fileURLToPath } from "node:url";
import path from "node:path";
import fs from "node:fs";

/**
 * 获取迁移目录路径
 * @returns 迁移目录绝对路径
 */
function migrationsDir(): string {
  const root = fileURLToPath(new URL("../..", import.meta.url)); // backEnd/
  return path.resolve(root, "migrations");
}

/**
 * 确保迁移元信息表存在
 */
function ensureMeta(): void {
  const db = getDb();
  db.run(`
    CREATE TABLE IF NOT EXISTS meta_migrations (
      id INTEGER PRIMARY KEY AUTOINCREMENT,
      name TEXT UNIQUE NOT NULL,
      applied_at TEXT DEFAULT (datetime('now'))
    );
  `);
}

/**
 * 获取已经应用的迁移名称集合
 * @returns 已应用迁移集合
 */
function appliedSet(): Set<string> {
  const db = getDb();
  const rows = db.query("SELECT name FROM meta_migrations").all() as Array<{ name: string }>;
  return new Set(rows.map((r) => r.name));
}

/**
 * 执行指定 SQL 文件的迁移
 * @param file SQL 文件名
 */
function applyMigration(file: string): void {
  const db = getDb();
  const full = path.resolve(migrationsDir(), file);
  const sql = fs.readFileSync(full, "utf-8");
  db.run("BEGIN;");
  try {
    db.run(sql);
    db.query("INSERT INTO meta_migrations (name) VALUES ($name)").run({ $name: file });
    db.run("COMMIT;");
    console.log(`[migrate] applied: ${file}`);
  } catch (err) {
    db.run("ROLLBACK;");
    console.error(`[migrate] failed: ${file}`, err);
    throw err;
  }
}

/**
 * 扫描并执行所有未应用的迁移
 */
export function runMigrations(): void {
  ensureMeta();
  const dir = migrationsDir();
  if (!fs.existsSync(dir)) {
    fs.mkdirSync(dir, { recursive: true });
  }
  const files = fs
    .readdirSync(dir)
    .filter((f) => f.endsWith(".sql"))
    .sort((a, b) => a.localeCompare(b));
  const applied = appliedSet();
  for (const f of files) {
    if (!applied.has(f)) {
      applyMigration(f);
    }
  }
}

/**
 * 命令行入口：执行迁移
 */
if (import.meta.main) {
  runMigrations();
}
