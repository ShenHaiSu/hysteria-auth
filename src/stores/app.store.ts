import { ref } from 'vue'
import { defineStore } from 'pinia'
import { storage } from '@/utils/storage'

export type Locale = 'zh-CN' | 'en-US'

export const useAppStore = defineStore('app', () => {
  const sidebarCollapsed = ref(false)
  const globalLoading = ref(false)
  const locale = ref<Locale>('zh-CN')
  const routeLoading = ref(false)

  function toggleSidebar() {
    sidebarCollapsed.value = !sidebarCollapsed.value
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
    globalLoading,
    locale,
    routeLoading,
    toggleSidebar,
    setGlobalLoading,
    setLocale,
    initLocale,
  }
})
