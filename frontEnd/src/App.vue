<template>
  <RouterView />
</template>

<script setup lang="ts">
import { onMounted } from 'vue'
import { RouterView } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

const authStore = useAuthStore()

onMounted(async () => {
  // #region 初始化获取用户信息
  // 如果本地有 token，尝试获取用户信息并挂载到 store
  if (authStore.token) {
    try {
      await authStore.fetchMe()
    } catch (error) {
      console.error('初始化获取用户信息失败:', error)
    }
  }
  // #endregion
})
</script>

<style>
/* 全局样式 */
body {
  margin: 0;
  padding: 0;
  font-family: var(--p-font-family);
}
</style>
