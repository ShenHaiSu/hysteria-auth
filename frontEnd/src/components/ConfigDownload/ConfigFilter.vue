<template>
  <div
    class="bg-surface-0 dark:bg-surface-900 p-4 rounded-lg shadow-sm mb-4 border border-surface-200 dark:border-surface-700"
  >
    <div class="flex flex-wrap gap-4 items-end">
      <!-- 节点分组筛选 (参考 NodeFilter.vue) -->
      <div class="flex flex-col gap-2">
        <label for="server_group" class="text-sm font-medium text-surface-600 dark:text-surface-400">节点分组</label>
        <InputText
          id="server_group"
          v-model="store.filters.server_group"
          placeholder="搜索分组..."
          class="w-full md:w-48"
          @keyup.enter="handleSearch"
        />
      </div>

      <!-- IP 筛选 -->
      <div class="flex flex-col gap-2">
        <label for="ip_address" class="text-sm font-medium text-surface-600 dark:text-surface-400">IP 地址</label>
        <InputText
          id="ip_address"
          v-model="store.filters.ip_address"
          placeholder="搜索 IP..."
          class="w-full md:w-48"
          @keyup.enter="handleSearch"
        />
      </div>

      <!-- 域名筛选 -->
      <div class="flex flex-col gap-2">
        <label for="domain" class="text-sm font-medium text-surface-600 dark:text-surface-400">域名</label>
        <InputText
          id="domain"
          v-model="store.filters.domain"
          placeholder="搜索域名..."
          class="w-full md:w-48"
          @keyup.enter="handleSearch"
        />
      </div>

      <!-- 状态筛选 -->
      <div class="flex flex-col gap-2">
        <label for="is_active" class="text-sm font-medium text-surface-600 dark:text-surface-400">启用状态</label>
        <Select
          id="is_active"
          v-model="store.filters.is_active"
          :options="statusOptions"
          optionLabel="label"
          optionValue="value"
          placeholder="全部状态"
          class="w-full md:w-48"
          showClear
          @change="handleSearch"
        />
      </div>

      <!-- 用户选择 (仅管理员可见) -->
      <div v-if="authStore.isAdmin" class="flex flex-col gap-2">
        <label for="target-user" class="text-sm font-medium text-surface-600 dark:text-surface-400">目标用户</label>
        <Select
          id="target-user"
          v-model="store.targetUserId"
          :options="userStore.users"
          optionLabel="username"
          optionValue="id"
          placeholder="选择用户 (默认为自己)"
          class="w-full md:w-48"
          showClear
          :loading="userStore.loading"
          @change="handleSearch"
        />
      </div>

      <!-- 操作按钮 -->
      <div class="flex items-center gap-2 ml-auto">
        <Button
          label="重置"
          icon="pi pi-refresh"
          severity="secondary"
          @click="store.resetFilters"
        />
        <Button
          label="生成配置"
          icon="pi pi-cog"
          :loading="store.loading"
          @click="handleSearch"
          class="p-button-primary"
        />
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { onMounted } from 'vue'
import { useConfigDownloadStore } from '@/stores/configDownload'
import { useAuthStore } from '@/stores/auth'
import { useUserStore } from '@/stores/user'
import InputText from 'primevue/inputtext'
import Select from 'primevue/select'
import Button from 'primevue/button'

// #region 初始化与状态
/**
 * 筛选过滤组件逻辑
 * 仿照 NodeFilter.vue 的搜索过滤方式，并为管理员添加用户选择功能
 */
const store = useConfigDownloadStore()
const authStore = useAuthStore()
const userStore = useUserStore()

/**
 * 状态选项定义
 */
const statusOptions = [
  { label: '已启用', value: 1 },
  { label: '已禁用', value: 0 },
]

onMounted(async () => {
  // 如果是管理员，加载用户列表以便选择
  if (authStore.isAdmin) {
    try {
      await userStore.fetchUsers()
    } catch (error) {
      console.error('加载用户列表失败:', error)
    }
  }
})
// #endregion

// #region 逻辑处理
/**
 * 触发生成配置操作 (即此处的"查询"行为)
 */
function handleSearch() {
  store.generateConfig()
}
// #endregion
</script>

<style scoped></style>
