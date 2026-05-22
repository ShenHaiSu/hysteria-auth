/**
 * localStorage 类型安全封装
 */

export const storage = {
  get(key: string): string | null {
    try {
      return localStorage.getItem(key)
    } catch {
      return null
    }
  },

  set(key: string, value: string): void {
    try {
      localStorage.setItem(key, value)
    } catch {
      // 静默处理（如配额已满）
    }
  },

  remove(key: string): void {
    try {
      localStorage.removeItem(key)
    } catch {
      // 静默处理
    }
  },

  getJSON<T>(key: string): T | null {
    const raw = this.get(key)
    if (!raw) return null
    try {
      return JSON.parse(raw) as T
    } catch {
      return null
    }
  },

  setJSON(key: string, value: unknown): void {
    try {
      this.set(key, JSON.stringify(value))
    } catch {
      // 静默处理
    }
  },
}
