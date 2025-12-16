/**
 * 格式化日期时间为指定格式
 * @param date 目标日期对象
 * @param fmt 格式模板（支持 yyyy MM dd HH mm ss SSS）
 * @returns 格式化后的字符串
 */
export function format(date: Date, fmt: string): string {
  const pad = (n: number, l = 2) => n.toString().padStart(l, "0");
  const map: Record<string, string> = {
    yyyy: date.getFullYear().toString(),
    MM: pad(date.getMonth() + 1),
    dd: pad(date.getDate()),
    HH: pad(date.getHours()),
    mm: pad(date.getMinutes()),
    ss: pad(date.getSeconds()),
    SSS: pad(date.getMilliseconds(), 3),
  };
  return fmt.replace(/yyyy|MM|dd|HH|mm|ss|SSS/g, (m) => map[m]);
}

/**
 * 返回 yyyy-MM-dd 格式（日志按天命名用）
 * @param date 目标日期，默认当前时间
 * @returns yyyy-MM-dd 字符串
 */
export function formatYMD(date: Date = new Date()): string {
  return format(date, "yyyy-MM-dd");
}

/**
 * 返回标准日志时间格式 yyyy-MM-dd HH:mm:ss.SSS
 * @param date 目标日期，默认当前时间
 * @returns 格式化后的时间字符串
 */
export function formatDateTime(date: Date = new Date()): string {
  return format(date, "yyyy-MM-dd HH:mm:ss.SSS");
}

