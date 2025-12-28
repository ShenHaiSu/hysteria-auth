<template>
  <Card class="h-full">
    <template #title>
      <div class="flex items-center gap-2">
        <i class="pi pi-map text-primary"></i>
        <span>节点概览</span>
      </div>
    </template>
    <template #content>
      <div v-if="nodeStore.loading" class="flex justify-center p-4">
        <i class="pi pi-spin pi-spinner text-2xl"></i>
      </div>
      <div v-else class="grid grid-cols-2 gap-4">
        <div class="p-3 bg-surface-50 rounded-lg flex flex-col items-center">
          <span class="text-surface-500 text-sm mb-1">总节点数</span>
          <span class="text-2xl font-bold">{{ nodeStore.nodes.length }}</span>
        </div>
        <div class="p-3 bg-green-50 rounded-lg flex flex-col items-center">
          <span class="text-green-600 text-sm mb-1">活跃节点</span>
          <span class="text-2xl font-bold text-green-700">{{ activeNodesCount }}</span>
        </div>
        <div class="p-3 bg-blue-50 rounded-lg flex flex-col items-center col-span-2">
          <span class="text-blue-600 text-sm mb-1">节点分组</span>
          <div class="flex flex-wrap gap-1 justify-center">
            <Tag
              v-for="(count, group) in groupStats"
              :key="group"
              :value="`${group}: ${count}`"
              severity="secondary"
            />
          </div>
        </div>
      </div>
      <div class="mt-4 flex justify-end">
        <Button
          label="管理节点"
          icon="pi pi-arrow-right"
          size="small"
          text
          @click="$router.push('/nodes')"
        />
      </div>
    </template>
  </Card>
</template>

<script setup lang="ts">
import { computed, onMounted } from 'vue'
import Card from 'primevue/card'
import Tag from 'primevue/tag'
import Button from 'primevue/button'
import { useNodeStore } from '@/stores/node'

// #region 初始化 (Init)
const nodeStore = useNodeStore()
// #endregion

// #region 计算属性 (Computed)
/**
 * 获取活跃节点数量
 */
const activeNodesCount = computed(() => {
  return nodeStore.nodes.filter((n) => n.is_active === 1).length
})

/**
 * 获取节点分组统计
 */
const groupStats = computed(() => {
  const stats: Record<string, number> = {}
  nodeStore.nodes.forEach((n) => {
    stats[n.server_group] = (stats[n.server_group] || 0) + 1
  })
  return stats
})
// #endregion

onMounted(() => {
  if (nodeStore.nodes.length === 0) {
    nodeStore.fetchNodes()
  }
})
</script>
