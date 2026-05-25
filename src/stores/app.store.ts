import { ref } from 'vue'
import { defineStore } from 'pinia'
import { storage } from '@/utils/storage'

export type Locale = 'zh-CN' | 'en-US'

export const useAppStore = defineStore('app', () => {
  /** 侧边栏折叠状态 (桌面端) */
  const sidebarCollapsed = ref(storage.get('sidebarCollapsed') === 'true')
  /** 移动端菜单是否展开（抽屉 overlay） */
  const mobileMenuVisible = ref(false)
  /** 全局加载遮罩 */
  const globalLoading = ref(false)
  /** 当前语言 */
  const locale = ref<Locale>('zh-CN')
  /** 路由加载浮窗状态 */
  const routeLoading = ref(false)

  function toggleSidebar() {
    sidebarCollapsed.value = !sidebarCollapsed.value
    storage.set('sidebarCollapsed', String(sidebarCollapsed.value))
  }

  function toggleMobileMenu() {
    mobileMenuVisible.value = !mobileMenuVisible.value
  }

  function closeMobileMenu() {
    mobileMenuVisible.value = false
  }

  function setGlobalLoading(loading: boolean) {
    globalLoading.value = loading
  }

  function setLocale(newLocale: Locale) {
    locale.value = newLocale
    storage.set('locale', newLocale)
  }

  function initLocale() {
    const saved = storage.get('locale')
    if (saved === 'zh-CN' || saved === 'en-US') {
      locale.value = saved
    }
  }

  return {
    sidebarCollapsed,
    mobileMenuVisible,
    globalLoading,
    locale,
    routeLoading,
    toggleSidebar,
    toggleMobileMenu,
    closeMobileMenu,
    setGlobalLoading,
    setLocale,
    initLocale,
  }
})
