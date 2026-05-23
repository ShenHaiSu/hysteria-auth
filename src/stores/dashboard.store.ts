import { ref, computed } from 'vue'
import { defineStore } from 'pinia'
import { dashboardApi } from '@/api/modules/dashboard'
import type { DashboardOverview } from '@/types/dashboard.types'

export const useDashboardStore = defineStore('dashboard', () => {
  const overview = ref<DashboardOverview | null>(null)
  const isLoading = ref(false)
  const lastFetchedAt = ref<number | null>(null)

  /** 缓存有效期 30 秒 */
  const CACHE_TTL = 30_000

  const isCacheValid = computed(() => {
    if (!lastFetchedAt.value) return false
    return Date.now() - lastFetchedAt.value < CACHE_TTL
  })

  const totalUsers = computed(() => overview.value?.totalUsers ?? 0)
  const activeUsers = computed(() => overview.value?.activeUsers ?? 0)
  const onlineUsersNow = computed(() => overview.value?.onlineUsersNow ?? 0)
  const totalNodes = computed(() => overview.value?.totalNodes ?? 0)
  const activeNodes = computed(() => overview.value?.activeNodes ?? 0)
  const totalTrafficToday = computed(() => overview.value?.totalTrafficToday ?? 0)
  const totalTrafficThisMonth = computed(() => overview.value?.totalTrafficThisMonth ?? 0)

  async function fetchOverview(force = false) {
    if (!force && isCacheValid.value && overview.value) return

    isLoading.value = true
    try {
      const res = await dashboardApi.getOverview()
      overview.value = res.data
      lastFetchedAt.value = Date.now()
    } finally {
      isLoading.value = false
    }
  }

  function reset() {
    overview.value = null
    lastFetchedAt.value = null
  }

  return {
    overview,
    isLoading,
    lastFetchedAt,
    totalUsers,
    activeUsers,
    onlineUsersNow,
    totalNodes,
    activeNodes,
    totalTrafficToday,
    totalTrafficThisMonth,
    fetchOverview,
    reset,
  }
})
