import { ref, computed } from 'vue'
import { defineStore } from 'pinia'
import router from '@/router'
import { authApi } from '@/api/modules/auth'
import { storage } from '@/utils/storage'
import type { AdminDto, LoginRequest } from '@/types/auth.types'
import type { AdminRole } from '@/types/common.types'

export const useAuthStore = defineStore('auth', () => {
  const token = ref<string | null>(null)
  const adminInfo = ref<AdminDto | null>(null)
  const expiresAt = ref<string | null>(null)

  /** 用于防止多次并发调用 fetchAdminInfo 时产生冗余请求 */
  let fetchAdminInfoPromise: Promise<void> | null = null

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
    fetchAdminInfoPromise = null

    storage.remove('token')
    storage.remove('expiresAt')
    storage.remove('adminInfo')

    router.push({ name: 'Login' })
  }

  /**
   * 懒加载管理员信息 — 使用 Promise 缓存防止并发重复请求
   */
  async function fetchAdminInfo() {
    if (adminInfo.value) return

    // 如果已有进行中的请求，复用同一个 Promise
    if (fetchAdminInfoPromise) {
      return fetchAdminInfoPromise
    }

    fetchAdminInfoPromise = (async () => {
      try {
        // 通过 /admin/login 的 getMe 风格接口获取；若无则使用存储的 adminInfo
        // 当前后端可能没有 getMe 接口，使用已有的 adminInfo 或从 token 中解析
        // 如果 localStorage 中已有 adminInfo，在 initFromStorage 阶段已恢复，所以这里直接检查
        if (!adminInfo.value) {
          // 尝试重新从 storage 恢复
          const saved = storage.get('adminInfo')
          if (saved) {
            try {
              adminInfo.value = JSON.parse(saved) as AdminDto
            } catch {
              // ignore
            }
          }
        }
        // 如果仍然没有 adminInfo，说明是全新登录状态，但 token 存在的情况
        // 此时不做处理，依赖 login 流程设置 adminInfo
      } finally {
        fetchAdminInfoPromise = null
      }
    })()

    return fetchAdminInfoPromise
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
