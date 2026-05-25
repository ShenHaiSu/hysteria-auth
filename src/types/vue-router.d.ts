import type { AdminRole } from '@/types/common.types'

declare module 'vue-router' {
  interface RouteMeta {
    /** 页面标题 (用于 document.title 和面包屑) */
    title?: string
    /** 侧边栏图标 (PrimeIcons class) */
    icon?: string
    /** 允许访问的角色列表，undefined = 不限制 */
    roles?: AdminRole[]
    /** 是否需要认证 */
    requiresAuth?: boolean
    /** 是否在侧边栏隐藏 */
    hidden?: boolean
    /** 使用的布局模板 */
    layout?: 'auth' | 'default'
  }
}

export {}
