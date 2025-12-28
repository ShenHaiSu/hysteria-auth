<template>
  <Card v-if="authStore.isAdmin" class="h-full">
    <template #title>
      <div class="flex items-center gap-2">
        <i class="pi pi-users text-primary"></i>
        <span>用户统计 (管理员)</span>
      </div>
    </template>
    <template #content>
      <div v-if="userStore.loading" class="flex justify-center p-4">
        <i class="pi pi-spin pi-spinner text-2xl"></i>
      </div>
      <div v-else class="grid grid-cols-2 gap-4">
        <div class="p-3 bg-surface-50 rounded-lg flex flex-col items-center">
          <span class="text-surface-500 text-sm mb-1">总用户数</span>
          <span class="text-2xl font-bold">{{ userStore.userVOs.length }}</span>
        </div>
        <div class="p-3 bg-orange-50 rounded-lg flex flex-col items-center">
          <span class="text-orange-600 text-sm mb-1">活跃用户</span>
          <span class="text-2xl font-bold text-orange-700">{{ activeUsersCount }}</span>
        </div>
      </div>
      <div class="mt-4 flex justify-end">
        <Button
          label="用户管理"
          icon="pi pi-arrow-right"
          size="small"
          text
          @click="$router.push('/users')"
        />
      </div>
    </template>
  </Card>
</template>

<script setup lang="ts">
import { computed, onMounted } from 'vue'
import Card from 'primevue/card'
import Button from 'primevue/button'
import { useUserStore } from '@/stores/user'
import { useAuthStore } from '@/stores/auth'

// #region 初始化 (Init)
const userStore = useUserStore()
const authStore = useAuthStore()
// #endregion

// #region 计算属性 (Computed)
/**
 * 获取活跃用户数量
 */
const activeUsersCount = computed(() => {
  return userStore.userVOs.filter((u) => u.is_active).length
})
// #endregion

onMounted(() => {
  if (authStore.isAdmin && userStore.userVOs.length === 0) {
    userStore.fetchUsers()
  }
})
</script>
