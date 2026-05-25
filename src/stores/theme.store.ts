import { ref } from 'vue'
import { defineStore } from 'pinia'
import { storage } from '@/utils/storage'

export type ThemeMode = 'light' | 'dark'

export const useThemeStore = defineStore('theme', () => {
  const mode = ref<ThemeMode>('light')

  function applyTheme() {
    document.documentElement.classList.toggle('dark', mode.value === 'dark')
    storage.set('theme', mode.value)
  }

  function initTheme() {
    const saved = storage.get('theme')
    if (saved === 'dark' || saved === 'light') {
      mode.value = saved
    } else if (window.matchMedia('(prefers-color-scheme: dark)').matches) {
      mode.value = 'dark'
    }
    applyTheme()
  }

  function setTheme(newMode: ThemeMode) {
    mode.value = newMode
    applyTheme()
  }

  function toggleTheme() {
    mode.value = mode.value === 'light' ? 'dark' : 'light'
    applyTheme()
  }

  return { mode, initTheme, setTheme, toggleTheme }
})
