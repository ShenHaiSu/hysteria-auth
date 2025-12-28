<!-- 用户数据列表：根据屏幕宽度切换桌面端表格或移动端卡片视图 -->
<template>
  <div class="space-y-4">
    <!-- 桌面端视图 -->
    <UserTableDesktop
      v-if="!isMobile"
      :users="users"
      :loading="loading"
      @edit="$emit('edit', $event)"
      @delete="$emit('delete', $event)"
    />

    <!-- 移动端视图 -->
    <UserTableMobile
      v-else
      :users="users"
      :loading="loading"
      @edit="$emit('edit', $event)"
      @delete="$emit('delete', $event)"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted } from 'vue'
import type { UserVO } from '@/composable/user'
import UserTableDesktop from './UserTableDesktop.vue'
import UserTableMobile from './UserTableMobile.vue'

// #region 定义属性和事件
defineProps<{
  users: UserVO[]
  loading: boolean
}>()

defineEmits<{
  (e: 'edit', user: UserVO): void
  (e: 'delete', user: UserVO): void
}>()
// #endregion

// #region 响应式断点检测
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
// #endregion
</script>
