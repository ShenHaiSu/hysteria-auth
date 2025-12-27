import '@/asset/tailwind.css'
import '@/asset/main.css'
import 'primeicons/primeicons.css'

import { createApp } from 'vue'
import { createPinia } from 'pinia'
import PrimeVue from 'primevue/config'
import ConfirmationService from 'primevue/confirmationservice'
import ToastService from 'primevue/toastservice'
import Tooltip from 'primevue/tooltip'
import material from '@primeuix/themes/material'
import primevueLocal from '@/asset/primevue-locale-zh'

import App from './App.vue'
import router from './router'

const app = createApp(App)

app.use(createPinia())
app.use(router)
app.use(ConfirmationService)
app.use(ToastService)
app.directive('tooltip', Tooltip)
app.use(PrimeVue, {
  theme: {
    preset: material,
    options: {
      prefix: 'p',
      cssLayer: false,
    },
  },
  locale: primevueLocal,
  ripple: true,
})

app.mount('#app')
