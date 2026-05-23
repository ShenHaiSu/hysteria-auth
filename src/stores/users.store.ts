import { ref, computed } from 'vue'
import { defineStore } from 'pinia'
import { userApi } from '@/api/modules/users'
import type { UserDto, UserFilters, UserTrafficStats } from '@/types/user.types'
import type { CreateUserRequest, UpdateUserRequest } from '@/types/user.types'

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
  const isSubmitting = ref(false)

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

  async function createUser(data: CreateUserRequest) {
    isSubmitting.value = true
    try {
      const res = await userApi.create(data)
      return res.data
    } finally {
      isSubmitting.value = false
    }
  }

  async function updateUser(id: number, data: UpdateUserRequest) {
    isSubmitting.value = true
    try {
      const res = await userApi.update(id, data)
      return res.data
    } finally {
      isSubmitting.value = false
    }
  }

  async function deleteUser(id: number) {
    await userApi.delete(id)
  }

  async function resetTraffic(id: number) {
    const res = await userApi.resetTraffic(id)
    return res.data
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
    isSubmitting,
    filters,
    searchQuery,
    currentUser,
    trafficStats,
    hasMore,
    fetchUsers,
    fetchUserDetail,
    fetchTrafficStats,
    createUser,
    updateUser,
    deleteUser,
    resetTraffic,
    resetFilters,
    setPage,
    setSearch,
  }
})
