import { computed } from 'vue'
import { useThemeStore } from '@/stores/theme.store'

/**
 * 主题切换 Composable — 桥接 themeStore 和组件
 */
export function useTheme() {
  const themeStore = useThemeStore()

  const mode = computed(() => themeStore.mode)
  const isDark = computed(() => themeStore.mode === 'dark')
  const isLight = computed(() => themeStore.mode === 'light')

  function toggle() {
    themeStore.toggleTheme()
  }

  function setMode(m: 'light' | 'dark') {
    themeStore.setTheme(m)
  }

  return { mode, isDark, isLight, toggle, setMode }
}
