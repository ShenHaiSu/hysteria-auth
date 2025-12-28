<!-- 节点列表展示组件 -->
<template>
  <div class="space-y-4">
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
import NodeListDesktop from './NodeListDesktop.vue'
import NodeListMobile from './NodeListMobile.vue'

defineProps<{
  nodes: NodeServer[]
  loading: boolean
}>()

defineEmits(['edit', 'delete', 'stop', 'copy'])

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
