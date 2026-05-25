import { ref, computed } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth.store'
import { useToast } from '@/composables/useToast'
import { useI18n } from 'vue-i18n'
import type { LoginRequest } from '@/types/auth.types'
import axios from 'axios'

/**
 * 认证逻辑 Composable — 封装登录/登出/Token 管理
 */
export function useAuth() {
  const router = useRouter()
  const authStore = useAuthStore()
  const toast = useToast()
  const { t } = useI18n()

  const isLoggingIn = ref(false)
  const loginError = ref<string | null>(null)

  const isAuthenticated = computed(() => authStore.isAuthenticated)

  async function login(data: LoginRequest) {
    isLoggingIn.value = true
    loginError.value = null

    try {
      await authStore.login(data)
      toast.success(t('auth.success'))
      const redirect = router.currentRoute.value.query?.redirect as string
      await router.push(redirect || { name: 'Dashboard' })
    } catch (err) {
      if (axios.isAxiosError(err)) {
        const code = err.response?.data?.error?.code
        switch (code) {
          case 'invalid_credentials':
            loginError.value = t('auth.error.invalidCredentials')
            break
          case 'account_disabled':
            loginError.value = t('auth.error.accountDisabled')
            break
          default:
            if (err.message?.includes('Network Error')) {
              loginError.value = t('auth.error.networkError')
            } else {
              loginError.value = t('auth.error.unknown')
            }
        }
      } else {
        loginError.value = t('auth.error.unknown')
      }
    } finally {
      isLoggingIn.value = false
    }
  }

  function logout() {
    authStore.logout()
  }

  return { isLoggingIn, loginError, isAuthenticated, login, logout }
}
