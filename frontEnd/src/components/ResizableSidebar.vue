<template>
  <aside
    class="relative flex-shrink-0 border-r border-surface-200 bg-surface-0 min-h-screen transition-[width] duration-150 ease-in-out"
    :style="{ width: width + 'px' }"
  >
    <div class="flex items-center justify-between px-2 py-1">
      <span class="text-[13px] font-medium text-surface-700">应用导航</span>
    </div>

    <div class="px-1 pb-1">
      <PanelMenu
        :model="sidebarMenuModel"
        class="text-[13px]"
      />
    </div>

    <div
      class="absolute right-0 top-0 h-full w-1 cursor-col-resize bg-transparent hover:bg-surface-200"
      @mousedown="onMouseDown"
    />
  </aside>
</template>

<script setup lang="ts">
import { ref, onBeforeUnmount } from 'vue'
import { PanelMenu } from 'primevue'
import { sidebarMenuModel } from '@/menu'

const width = ref<number>(240)
const dragging = ref<boolean>(false)
const startX = ref<number>(0)
const startWidth = ref<number>(0)
const minWidth = 160
const maxWidth = 480

/**
 * 启动侧边栏宽度拖拽
 * @param e 鼠标按下事件
 */
const onMouseDown = (e: MouseEvent): void => {
  dragging.value = true
  startX.value = e.clientX
  startWidth.value = width.value
  document.body.style.cursor = 'col-resize'
  window.addEventListener('mousemove', onMouseMove)
  window.addEventListener('mouseup', onMouseUp)
}

/**
 * 处理拖拽过程中的宽度变化
 * @param e 鼠标移动事件
 */
const onMouseMove = (e: MouseEvent): void => {
  if (!dragging.value) return
  const delta = e.clientX - startX.value
  width.value = clampWidth(startWidth.value + delta)
}

/**
 * 结束拖拽并清理事件监听
 */
const onMouseUp = (): void => {
  dragging.value = false
  document.body.style.cursor = 'default'
  window.removeEventListener('mousemove', onMouseMove)
  window.removeEventListener('mouseup', onMouseUp)
}

/**
 * 将宽度限制在允许范围内
 * @param value 目标宽度
 * @returns 限制后的宽度
 */
const clampWidth = (value: number): number => {
  if (value < minWidth) return minWidth
  if (value > maxWidth) return maxWidth
  return value
}

onBeforeUnmount(() => {
  if (dragging.value) {
    window.removeEventListener('mousemove', onMouseMove)
    window.removeEventListener('mouseup', onMouseUp)
  }
})
</script>

<style scoped>
</style>
