import { ref, computed } from 'vue'
import { defineStore } from 'pinia'
import { useRouter } from 'vue-router'
import { authApi } from '@/api/modules/auth'
import { storage } from '@/utils/storage'
import type { AdminDto, LoginRequest } from '@/types/auth.types'
import type { AdminRole } from '@/types/common.types'

export const useAuthStore = defineStore('auth', () => {
  const token = ref<string | null>(null)
  const adminInfo = ref<AdminDto | null>(null)
  const expiresAt = ref<string | null>(null)

  const isAuthenticated = computed(() => !!token.value)
  const role = computed<AdminRole | null>(() => adminInfo.value?.role ?? null)
  const isSuperAdmin = computed(() => role.value === 'super_admin')
  const isAdmin = computed(() => role.value === 'super_admin' || role.value === 'admin')

  function initFromStorage() {
    token.value = storage.get('token')
    expiresAt.value = storage.get('expiresAt')
    const saved = storage.get('adminInfo')
    if (saved) {
      try {
        adminInfo.value = JSON.parse(saved) as AdminDto
      } catch {
        adminInfo.value = null
      }
    }
  }

  async function login(data: LoginRequest) {
    const res = await authApi.login(data)
    const { token: newToken, expiresAt: newExpiresAt, admin } = res.data

    token.value = newToken
    expiresAt.value = newExpiresAt
    adminInfo.value = admin

    storage.set('token', newToken)
    storage.set('expiresAt', newExpiresAt)
    storage.set('adminInfo', JSON.stringify(admin))
  }

  function logout() {
    token.value = null
    adminInfo.value = null
    expiresAt.value = null

    storage.remove('token')
    storage.remove('expiresAt')
    storage.remove('adminInfo')

    const router = useRouter()
    router.push({ name: 'Login' })
  }

  async function fetchAdminInfo() {
    // 当前阶段暂不实现，Phase 1 完成后接入
    // 需要在 API 层添加 getMe() 接口
  }

  async function refreshToken() {
    // Phase 2 实现
  }

  return {
    token,
    adminInfo,
    expiresAt,
    isAuthenticated,
    role,
    isSuperAdmin,
    isAdmin,
    initFromStorage,
    login,
    logout,
    fetchAdminInfo,
    refreshToken,
  }
})
