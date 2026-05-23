import { ref } from 'vue'
import { defineStore } from 'pinia'
import { adminApi } from '@/api/modules/admins'
import type { AdminDto, AdminFilters, CreateAdminRequest, UpdateAdminRequest } from '@/types/admin.types'

export const useAdminsStore = defineStore('admins', () => {
  const items = ref<AdminDto[]>([])
  const total = ref(0)
  const page = ref(1)
  const pageSize = ref(20)
  const isLoading = ref(false)
  const isSubmitting = ref(false)
  const filters = ref<AdminFilters>({})
  const searchQuery = ref('')

  async function fetchAdmins() {
    isLoading.value = true
    try {
      const res = await adminApi.getList({
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

  async function createAdmin(data: CreateAdminRequest) {
    isSubmitting.value = true
    try {
      const res = await adminApi.create(data)
      return res.data
    } finally {
      isSubmitting.value = false
    }
  }

  async function updateAdmin(id: number, data: UpdateAdminRequest) {
    isSubmitting.value = true
    try {
      const res = await adminApi.update(id, data)
      return res.data
    } finally {
      isSubmitting.value = false
    }
  }

  function resetFilters() {
    filters.value = {}
    searchQuery.value = ''
    page.value = 1
    fetchAdmins()
  }

  function setPage(newPage: number) {
    page.value = newPage
    fetchAdmins()
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
    fetchAdmins,
    createAdmin,
    updateAdmin,
    resetFilters,
    setPage,
    setSearch,
  }
})
