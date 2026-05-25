import { format as dateFnsFormat, formatDistanceToNow } from 'date-fns'
import { zhCN, enUS } from 'date-fns/locale'

const localeMap = {
  'zh-CN': zhCN,
  'en-US': enUS,
}

/**
 * 格式化日期
 */
export function formatDate(
  date: string | Date,
  pattern: string = 'yyyy-MM-dd HH:mm:ss',
  locale: 'zh-CN' | 'en-US' = 'zh-CN',
): string {
  const d = typeof date === 'string' ? new Date(date) : date
  return dateFnsFormat(d, pattern, { locale: localeMap[locale] })
}

/**
 * 相对时间（如 "3 分钟前"）
 */
export function formatRelativeTime(
  date: string | Date,
  locale: 'zh-CN' | 'en-US' = 'zh-CN',
): string {
  const d = typeof date === 'string' ? new Date(date) : date
  return formatDistanceToNow(d, { addSuffix: true, locale: localeMap[locale] })
}

/**
 * 格式化文件大小 (bytes → 人类可读)
 */
export function formatFileSize(bytes: number): string {
  if (bytes === 0) return '0 B'
  const units = ['B', 'KB', 'MB', 'GB', 'TB']
  const k = 1024
  const i = Math.floor(Math.log(bytes) / Math.log(k))
  const value = bytes / Math.pow(k, i)
  return `${value.toFixed(i === 0 ? 0 : 2)} ${units[i]}`
}

/**
 * 格式化数字（千分位）
 */
export function formatNumber(num: number): string {
  return num.toLocaleString('zh-CN')
}
