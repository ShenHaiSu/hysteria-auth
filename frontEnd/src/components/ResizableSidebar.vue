<template>
  <aside
    class="relative shrink-0 border-r border-surface-200 bg-surface-0 min-h-screen overflow-hidden"
    :class="[
      isDragging ? 'cursor-col-resize select-none' : 'transition-[width] duration-300 ease-in-out'
    ]"
    :style="{ width: width + 'px' }"
  >
    <!-- 顶部标题栏 -->
    <div class="flex items-center justify-between px-4 py-3 border-b border-surface-100 h-11.25">
      <span 
        v-show="width > COLLAPSED_WIDTH" 
        class="text-sm font-semibold text-surface-900 tracking-tight whitespace-nowrap overflow-hidden"
      >
        应用导航
      </span>
      <button
        @click="toggleCollapse"
        class="p-1.5 rounded-md hover:bg-surface-100 text-surface-500 transition-colors duration-200 flex items-center justify-center"
        :class="{ 'w-full': width <= COLLAPSED_WIDTH }"
        title="切换侧边栏"
      >
        <i :class="['pi', width <= COLLAPSED_WIDTH ? 'pi-chevron-right' : 'pi-chevron-left']" style="font-size: 0.875rem"></i>
      </button>
    </div>

    <!-- 菜单内容区域 -->
    <div 
      class="overflow-y-auto h-[calc(100vh-45px)] px-2 py-2 custom-scrollbar transition-opacity duration-200"
      :class="{ 'opacity-0 pointer-events-none': width <= COLLAPSED_WIDTH }"
    >
      <PanelMenu
        :model="sidebarMenuModel"
        class="sidebar-menu"
      />
    </div>

    <!-- 拖拽手柄 -->
    <div
      v-if="width > COLLAPSED_WIDTH"
      class="absolute right-0 top-0 h-full w-1.5 cursor-col-resize group z-10"
      @mousedown="onMouseDown"
    >
      <div
        class="h-full w-full transition-colors duration-200"
        :class="[
          isDragging ? 'bg-primary-500' : 'bg-transparent group-hover:bg-surface-300'
        ]"
      />
    </div>
  </aside>
</template>

<script setup lang="ts">
import { ref, onBeforeUnmount } from 'vue'
import { PanelMenu } from 'primevue'
import { sidebarMenuModel } from '@/menu'

// --- 状态定义 ---

/** 侧边栏当前宽度 (像素) */
const width = ref<number>(240)
/** 存储折叠前的宽度，用于恢复 */
const savedWidth = ref<number>(240)
/** 是否正在拖拽中 */
const isDragging = ref<boolean>(false)
/** 拖拽开始时的鼠标 X 坐标 */
const startX = ref<number>(0)
/** 拖拽开始时的侧边栏宽度 */
const startWidth = ref<number>(0)

// --- 常量定义 ---

/** 侧边栏最小允许宽度 */
const MIN_WIDTH = 180
/** 侧边栏最大允许宽度 */
const MAX_WIDTH = 480
/** 侧边栏折叠后的宽度 */
const COLLAPSED_WIDTH = 56

// --- 方法定义 ---

/**
 * 切换侧边栏的折叠状态
 */
const toggleCollapse = (): void => {
  if (width.value > COLLAPSED_WIDTH) {
    // 执行折叠：保存当前宽度并设为折叠宽度
    savedWidth.value = width.value
    width.value = COLLAPSED_WIDTH
  } else {
    // 执行展开：恢复到之前保存的宽度
    width.value = savedWidth.value > COLLAPSED_WIDTH ? savedWidth.value : 240
  }
}

/**
 * 处理鼠标按下事件，启动侧边栏宽度拖拽
 * @param {MouseEvent} e - 鼠标按下事件对象
 */
const onMouseDown = (e: MouseEvent): void => {
  isDragging.value = true
  startX.value = e.clientX
  startWidth.value = width.value
  
  // 改变全局鼠标样式，防止拖拽过程中鼠标闪烁
  document.body.style.cursor = 'col-resize'
  document.body.style.userSelect = 'none'
  
  window.addEventListener('mousemove', onMouseMove)
  window.addEventListener('mouseup', onMouseUp)
}

/**
 * 处理拖拽过程中的鼠标移动事件，实时更新侧边栏宽度
 * @param {MouseEvent} e - 鼠标移动事件对象
 */
const onMouseMove = (e: MouseEvent): void => {
  if (!isDragging.value) return
  
  const delta = e.clientX - startX.value
  const targetWidth = startWidth.value + delta
  
  // 使用限制函数确保宽度在合法范围内
  width.value = clampWidth(targetWidth)
}

/**
 * 处理鼠标抬起事件，结束拖拽并清理事件监听器
 */
const onMouseUp = (): void => {
  isDragging.value = false
  
  // 恢复全局样式
  document.body.style.cursor = ''
  document.body.style.userSelect = ''
  
  window.removeEventListener('mousemove', onMouseMove)
  window.removeEventListener('mouseup', onMouseUp)
}

/**
 * 将给定的宽度值限制在最小和最大范围内
 * @param {number} value - 需要限制的宽度值
 * @returns {number} 限制后的有效宽度值
 */
const clampWidth = (value: number): number => {
  return Math.min(Math.max(value, MIN_WIDTH), MAX_WIDTH)
}

// 组件卸载前确保清理全局事件监听
onBeforeUnmount(() => {
  if (isDragging.value) {
    onMouseUp()
  }
})
</script>

<style scoped>
.sidebar-menu :deep(.p-panelmenu-header-content) {
  background-color: transparent;
  border: none;
  padding: 0.375rem 0.5rem;
  transition: background-color 0.2s;
  border-radius: 0.375rem;
}

.sidebar-menu :deep(.p-panelmenu-header-content:hover) {
  background-color: var(--p-surface-100);
}

.sidebar-menu :deep(.p-panelmenu-item-content) {
  background-color: transparent;
  border: none;
  padding: 0.25rem 0.5rem;
  transition: background-color 0.2s;
  border-radius: 0.375rem;
}

.sidebar-menu :deep(.p-panelmenu-item-content:hover) {
  background-color: var(--p-surface-50);
}

.sidebar-menu :deep(.p-panelmenu-header-label),
.sidebar-menu :deep(.p-panelmenu-item-label) {
  font-size: 13px;
  color: var(--p-surface-700);
}

.sidebar-menu :deep(.p-panelmenu-icon) {
  color: var(--p-surface-400);
  font-size: 0.75rem;
}

/* 自定义滚动条样式 */
.custom-scrollbar::-webkit-scrollbar {
  width: 4px;
}

.custom-scrollbar::-webkit-scrollbar-track {
  background-color: transparent;
}

.custom-scrollbar::-webkit-scrollbar-thumb {
  background-color: var(--p-surface-200);
  border-radius: 9999px;
}

.custom-scrollbar:hover::-webkit-scrollbar-thumb {
  background-color: var(--p-surface-300);
}
</style>
