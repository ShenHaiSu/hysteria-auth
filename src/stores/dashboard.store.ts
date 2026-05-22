import { ref } from 'vue'
import { defineStore } from 'pinia'
import { dashboardApi } from '@/api/modules/dashboard'
import type { DashboardOverview } from '@/types/dashboard.types'

export const useDashboardStore = defineStore('dashboard', () => {
  const overview = ref<DashboardOverview | null>(null)
  const isLoading = ref(false)

  async function fetchOverview() {
    isLoading.value = true
    try {
      const res = await dashboardApi.getOverview()
      overview.value = res.data
    } finally {
      isLoading.value = false
    }
  }

  return {
    overview,
    isLoading,
    fetchOverview,
  }
})
