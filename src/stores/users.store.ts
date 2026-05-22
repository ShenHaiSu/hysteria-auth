import { ref, computed } from 'vue'
import { defineStore } from 'pinia'
import { userApi } from '@/api/modules/users'
import type { UserDto, UserFilters, UserTrafficStats } from '@/types/user.types'

export const useUsersStore = defineStore('users', () => {
  const items = ref<UserDto[]>([])
  const total = ref(0)
  const page = ref(1)
  const pageSize = ref(20)
  const isLoading = ref(false)
  const filters = ref<UserFilters>({})
  const searchQuery = ref('')
  const currentUser = ref<UserDto | null>(null)
  const trafficStats = ref<UserTrafficStats | null>(null)

  const hasMore = computed(() => page.value * pageSize.value < total.value)

  async function fetchUsers() {
    isLoading.value = true
    try {
      const res = await userApi.getList({
        ...filters.value,
        search: searchQuery.value || undefined,
        page: page.value,
        pageSize: pageSize.value,
      })
      items.value = res.data.items
      total.value = res.data.total
    } finally {
      isLoading.value = false
    }
  }

  async function fetchUserDetail(id: number) {
    isLoading.value = true
    try {
      const res = await userApi.getById(id)
      currentUser.value = res.data
    } finally {
      isLoading.value = false
    }
  }

  async function fetchTrafficStats(id: number, period: 'day' | 'week' | 'month' | 'all' = 'month') {
    const res = await userApi.getTrafficStats(id, period)
    trafficStats.value = res.data
  }

  function resetFilters() {
    filters.value = {}
    searchQuery.value = ''
    page.value = 1
    fetchUsers()
  }

  function setPage(newPage: number) {
    page.value = newPage
    fetchUsers()
  }

  function setSearch(q: string) {
    searchQuery.value = q
    page.value = 1
  }

  return {
    items,
    total,
    page,
    pageSize,
    isLoading,
    filters,
    searchQuery,
    currentUser,
    trafficStats,
    hasMore,
    fetchUsers,
    fetchUserDetail,
    fetchTrafficStats,
    resetFilters,
    setPage,
    setSearch,
  }
})
