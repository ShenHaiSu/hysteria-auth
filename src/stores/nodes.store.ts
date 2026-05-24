import { ref, computed } from 'vue'
import { defineStore } from 'pinia'
import { nodeApi } from '@/api/modules/nodes'
import type { NodeDto, NodeDetail, NodeStatusRecord, PreRegisterNodeRequest, UpdateNodeConfigRequest } from '@/types/node.types'

/** 心跳超时阈值 (ms)：超过此时间未收到心跳判定为离线 */
export const HEARTBEAT_TIMEOUT_MS = 90_000

export const useNodesStore = defineStore('nodes', () => {
  const items = ref<NodeDto[]>([])
  const total = ref(0)
  const page = ref(1)
  const pageSize = ref(20)
  const isLoading = ref(false)
  const isSubmitting = ref(false)
  const searchQuery = ref('')
  const activeFilter = ref<'all' | 'active' | 'inactive'>('all')
  const provisionFilter = ref<'all' | 'pending' | 'provisioned'>('all')
  const currentNode = ref<NodeDetail | null>(null)
  const statusHistory = ref<NodeStatusRecord[]>([])
  const statusHistoryTotal = ref(0)
  const statusHistoryLoading = ref(false)

  const hasMore = computed(() => page.value * pageSize.value < total.value)

  async function fetchNodes() {
    isLoading.value = true
    try {
      const filters: Record<string, unknown> = {
        search: searchQuery.value || undefined,
        page: page.value,
        pageSize: pageSize.value,
      }
      if (activeFilter.value !== 'all') {
        filters.isActive = activeFilter.value === 'active'
      }
      if (provisionFilter.value !== 'all') {
        filters.provisionStatus = provisionFilter.value
      }
      const res = await nodeApi.getList(filters as Parameters<typeof nodeApi.getList>[0])
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
    statusHistoryLoading.value = true
    try {
      const res = await nodeApi.getStatusHistory(id, params)
      statusHistory.value = res.data.items
      statusHistoryTotal.value = res.data.total
    } finally {
      statusHistoryLoading.value = false
    }
  }

  async function preRegisterNode(data: PreRegisterNodeRequest) {
    isSubmitting.value = true
    try {
      const res = await nodeApi.preRegister(data)
      return res.data
    } finally {
      isSubmitting.value = false
    }
  }

  /** Phase 7: 更新节点 Hysteria 2 配置 */
  async function updateNodeConfig(id: string, data: UpdateNodeConfigRequest) {
    isSubmitting.value = true
    try {
      await nodeApi.updateConfig(id, data)
      // 配置更新后自动刷新节点详情以获取最新 configVersion 等
      await fetchNodeDetail(id)
    } finally {
      isSubmitting.value = false
    }
  }

  async function rotateSecret(id: string) {
    isSubmitting.value = true
    try {
      await nodeApi.rotateSecret(id)
      await fetchNodeDetail(id)
    } finally {
      isSubmitting.value = false
    }
  }

  async function kickUser(nodeId: string, userId: number) {
    isSubmitting.value = true
    try {
      await nodeApi.kickUser(nodeId, userId)
    } finally {
      isSubmitting.value = false
    }
  }

  function resetFilters() {
    searchQuery.value = ''
    activeFilter.value = 'all'
    provisionFilter.value = 'all'
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
    isSubmitting,
    searchQuery,
    activeFilter,
    provisionFilter,
    currentNode,
    statusHistory,
    statusHistoryTotal,
    statusHistoryLoading,
    hasMore,
    fetchNodes,
    fetchNodeDetail,
    fetchStatusHistory,
    preRegisterNode,
    updateNodeConfig,
    rotateSecret,
    kickUser,
    resetFilters,
    setPage,
    setSearch,
  }
})
