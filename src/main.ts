import '@/assets/styles/public.css'

import { createApp } from 'vue'
import { createPinia } from 'pinia'
import App from './App.vue'
import router from './router'
import i18n from './locales'

// PrimeVue
import PrimeVue from 'primevue/config'
import Material from '@primeuix/themes/material'
import ToastService from 'primevue/toastservice'
import ConfirmationService from 'primevue/confirmationservice'
import Tooltip from 'primevue/tooltip'

// 初始化
import { useThemeStore } from '@/stores/theme.store'
import { useAuthStore } from '@/stores/auth.store'
import { vPermission } from '@/directives/permission'

const app = createApp(App)

// ── 插件注册 ──
const pinia = createPinia()
app.use(pinia)

// 恢复认证状态
const authStore = useAuthStore()
authStore.initFromStorage()

// 恢复主题
const themeStore = useThemeStore()
themeStore.initTheme()

// 路由 (守卫已在 router/index.ts 中通过 setupRouterGuards 注册)
app.use(router)

app.use(i18n)

// PrimeVue
app.use(PrimeVue, {
  theme: {
    preset: Material,
    options: {
      darkModeSelector: '.dark',
    },
  },
  ripple: true,
})
app.use(ToastService)
app.use(ConfirmationService)
app.directive('tooltip', Tooltip)

// 自定义指令
app.directive('permission', vPermission)

app.mount('#app')
