<!-- 节点列表展示组件 -->
<template>
  <div class="space-y-4">
    <!-- 头部工具栏 (移动端和桌面端共用) -->
    <div
      class="bg-surface-0 dark:bg-surface-900 rounded-lg shadow-sm border border-surface-200 dark:border-surface-700 p-4"
    >
      <div class="flex justify-between items-center">
        <div class="flex items-center gap-2">
          <i class="pi pi-server text-primary-500 text-xl"></i>
          <span class="text-xl font-bold text-surface-900 dark:text-surface-0">节点列表</span>
        </div>
        <Button
          label="新增节点"
          icon="pi pi-plus"
          size="small"
          severity="primary"
          raised
          @click="$emit('add')"
        />
      </div>
    </div>

    <!-- 根据屏幕宽度切换视图 -->
    <NodeListDesktop
      v-if="!isMobile"
      :nodes="nodes"
      :loading="loading"
      @edit="$emit('edit', $event)"
      @delete="$emit('delete', $event)"
      @stop="$emit('stop', $event)"
      @copy="$emit('copy', $event)"
    />
    <NodeListMobile
      v-else
      :nodes="nodes"
      :loading="loading"
      @edit="$emit('edit', $event)"
      @delete="$emit('delete', $event)"
      @stop="$emit('stop', $event)"
      @copy="$emit('copy', $event)"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted } from 'vue'
import type { NodeServer } from '@/composable/node'
import Button from 'primevue/button'
import NodeListDesktop from './NodeListDesktop.vue'
import NodeListMobile from './NodeListMobile.vue'

defineProps<{
  nodes: NodeServer[]
  loading: boolean
}>()

defineEmits(['add', 'edit', 'delete', 'stop', 'copy'])

// 响应式断点检测
const isMobile = ref(false)
let mediaQuery: MediaQueryList | null = null

/**
 * 更新移动端状态
 * @param e MediaQueryListEvent
 */
const updateMatch = (e: MediaQueryListEvent | MediaQueryList) => {
  isMobile.value = e.matches
}

onMounted(() => {
  // 使用 Tailwind 默认的 md 断点 (768px)
  mediaQuery = window.matchMedia('(max-width: 767px)')
  isMobile.value = mediaQuery.matches
  mediaQuery.addEventListener('change', updateMatch)
})

onUnmounted(() => {
  if (mediaQuery) {
    mediaQuery.removeEventListener('change', updateMatch)
  }
})
</script>
