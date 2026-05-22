import '@/assets/styles/public.css'

import { createApp } from 'vue'
import { createPinia } from 'pinia'

import App from './App.vue'
import router from './router'
import PrimeVue from 'primevue/config'
import Material from '@primeuix/themes/material'
import ToastService from 'primevue/toastservice'
import ConfirmationService from 'primevue/confirmationservice'
import Tooltip from 'primevue/tooltip'

const app = createApp(App)

app.use(createPinia())
app.directive('tooltip', Tooltip)
app.use(router)
app.use(ToastService)
app.use(ConfirmationService)
app.use(PrimeVue, {
  theme: {
    preset: Material,
  },
  ripple: true,
})

app.mount('#app')
