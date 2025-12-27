import { fileURLToPath } from "node:url";
import path from "node:path";
import fs from "node:fs";
import { formatDateTime, formatYMD } from "@/utils/dateUtil";
import { formatFields } from "@/utils/stringUtil";

/**
 * 解析项目根目录（backEnd）绝对路径
 * @returns 根目录绝对路径
 */
export function resolveRootDir(): string {
  return fileURLToPath(new URL("../..", import.meta.url));
}

/**
 * 确保目录存在
 * @param dir 目录路径
 */
export function ensureDir(dir: string): void {
  if (!fs.existsSync(dir)) {
    fs.mkdirSync(dir, { recursive: true });
  }
}

/**
 * 计算当日日志文件路径（logs/yyyy-MM-dd.log）
 * @returns 日志文件绝对路径
 */
export function resolveDailyLogPath(): string {
  const root = resolveRootDir();
  const logsDir = path.resolve(root, "logs");
  ensureDir(logsDir);
  const file = `${formatYMD(new Date())}.log`;
  return path.resolve(logsDir, file);
}

/**
 * 写入一行日志（按天滚动文件）
 * @param level 日志级别
 * @param message 简要消息
 * @param fields 额外字段
 */
export function log(
  level: "INFO" | "ERROR" | "WARN" | "DEBUG",
  message: string,
  fields: Record<string, unknown> = {}
): void {
  const file = resolveDailyLogPath();
  const line = `[${level}] ${formatDateTime(new Date())} | ${message} | ${formatFields(fields)}`;
  try {
    fs.appendFileSync(file, line + "\n", { encoding: "utf-8" });
    console.log(line);
  } catch (err) {
    // 降级输出到控制台
    console.error("[log_fallback]", line, err);
  }
}
