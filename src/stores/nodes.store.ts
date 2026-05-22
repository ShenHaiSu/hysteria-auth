import { ref, computed } from 'vue'
import { defineStore } from 'pinia'
import { nodeApi } from '@/api/modules/nodes'
import type { NodeDto, NodeDetail, NodeStatusRecord } from '@/types/node.types'

export const useNodesStore = defineStore('nodes', () => {
  const items = ref<NodeDto[]>([])
  const total = ref(0)
  const page = ref(1)
  const pageSize = ref(20)
  const isLoading = ref(false)
  const searchQuery = ref('')
  const currentNode = ref<NodeDetail | null>(null)
  const statusHistory = ref<NodeStatusRecord[]>([])
  const statusHistoryTotal = ref(0)

  const hasMore = computed(() => page.value * pageSize.value < total.value)

  async function fetchNodes() {
    isLoading.value = true
    try {
      const res = await nodeApi.getList({
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

  async function fetchNodeDetail(id: string) {
    isLoading.value = true
    try {
      const res = await nodeApi.getById(id)
      currentNode.value = res.data
    } finally {
      isLoading.value = false
    }
  }

  async function fetchStatusHistory(
    id: string,
    params: { page: number; pageSize: number; startDate?: string; endDate?: string },
  ) {
    const res = await nodeApi.getStatusHistory(id, params)
    statusHistory.value = res.data.items
    statusHistoryTotal.value = res.data.total
  }

  function resetFilters() {
    searchQuery.value = ''
    page.value = 1
    fetchNodes()
  }

  function setPage(newPage: number) {
    page.value = newPage
    fetchNodes()
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
    searchQuery,
    currentNode,
    statusHistory,
    statusHistoryTotal,
    hasMore,
    fetchNodes,
    fetchNodeDetail,
    fetchStatusHistory,
    resetFilters,
    setPage,
    setSearch,
  }
})
