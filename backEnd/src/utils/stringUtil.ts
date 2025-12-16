/**
 * 左侧填充至指定长度
 * @param input 原始字符串
 * @param length 目标长度
 * @param char 填充字符，默认空格
 * @returns 填充后的字符串
 */
export function padLeft(input: string, length: number, char = " "): string {
  if (input.length >= length) return input;
  return char.repeat(length - input.length) + input;
}

/**
 * 右侧填充至指定长度
 * @param input 原始字符串
 * @param length 目标长度
 * @param char 填充字符，默认空格
 * @returns 填充后的字符串
 */
export function padRight(input: string, length: number, char = " "): string {
  if (input.length >= length) return input;
  return input + char.repeat(length - input.length);
}

/**
 * 将任意值安全转换为字符串
 * @param value 任意值
 * @returns 字符串表现形式
 */
export function safeToString(value: unknown): string {
  if (value === null) return "null";
  if (value === undefined) return "undefined";
  if (typeof value === "string") return value;
  try {
    return JSON.stringify(value);
  } catch {
    return String(value);
  }
}

/**
 * 将对象格式化为 key=value 的串
 * @param fields 字段对象
 * @returns 格式化后的 KV 文本
 */
export function formatFields(fields: Record<string, unknown>): string {
  const parts: string[] = [];
  for (const [k, v] of Object.entries(fields)) {
    parts.push(`${k}=${safeToString(v)}`);
  }
  return parts.join(" ");
}

