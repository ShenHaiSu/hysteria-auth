import { ref, computed } from 'vue'
import { defineStore } from 'pinia'
import { login as apiLogin, getMe as apiGetMe, logout as apiLogout } from '@/fetch/auth'
import type { LoginRequest, AuthUser } from '@/composable/auth'

/**
 * 认证相关的 Pinia Store
 * 处理用户登录、登出、用户信息同步及令牌管理
 */
export const useAuthStore = defineStore('auth', () => {
  // #region 状态 (State)
  const token = ref<string | null>(localStorage.getItem('token'))
  const user = ref<AuthUser | null>(null)
  const loading = ref(false)
  // #endregion

  // #region 计算属性 (Getters)
  const isAuthenticated = computed(() => !!token.value)
  const isAdmin = computed(() => user.value?.permission === 'admin')
  // #endregion

  // #region 动作 (Actions)
  /**
   * 用户登录
   * @param loginData 包含用户名和 MD5 密码的请求体
   */
  async function login(loginData: LoginRequest) {
    loading.value = true
    try {
      const response = await apiLogin(loginData)
      token.value = response.token
      user.value = response.user
      localStorage.setItem('token', response.token)
    } finally {
      loading.value = false
    }
  }

  /**
   * 获取当前登录用户信息
   * 通常在应用初始化或刷新页面时调用
   */
  async function fetchMe() {
    if (!token.value) return
    loading.value = true
    try {
      const userInfo = await apiGetMe()
      user.value = userInfo
    } catch (error) {
      // 如果获取用户信息失败（如 token 过期），则清理登录状态
      logout()
      throw error
    } finally {
      loading.value = false
    }
  }

  /**
   * 用户登出
   * 清理本地存储的令牌和内存中的用户信息
   */
  async function logout() {
    try {
      await apiLogout()
    } finally {
      token.value = null
      user.value = null
      localStorage.removeItem('token')
    }
  }
  // #endregion

  return {
    token,
    user,
    loading,
    isAuthenticated,
    isAdmin,
    login,
    fetchMe,
    logout,
  }
})
