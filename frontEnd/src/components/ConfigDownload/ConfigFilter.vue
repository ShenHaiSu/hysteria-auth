<template>
  <div class="card p-4 bg-surface-0 dark:bg-surface-900 border-round shadow-1">
    <div class="flex flex-wrap gap-4 items-end">
      <!-- 节点分组筛选 (参考 NodeFilter.vue) -->
      <div class="flex flex-col gap-2">
        <label for="server_group" class="font-semibold text-sm">节点分组</label>
        <InputText
          id="server_group"
          v-model="store.filters.server_group"
          placeholder="搜索分组..."
          class="w-full md:w-48"
        />
      </div>

      <!-- IP 筛选 -->
      <div class="flex flex-col gap-2">
        <label for="ip_address" class="font-semibold text-sm">IP 地址</label>
        <InputText
          id="ip_address"
          v-model="store.filters.ip_address"
          placeholder="搜索 IP..."
          class="w-full md:w-48"
        />
      </div>

      <!-- 域名筛选 -->
      <div class="flex flex-col gap-2">
        <label for="domain" class="font-semibold text-sm">域名</label>
        <InputText
          id="domain"
          v-model="store.filters.domain"
          placeholder="搜索域名..."
          class="w-full md:w-48"
        />
      </div>

      <!-- 状态筛选 -->
      <div class="flex flex-col gap-2">
        <label for="is_active" class="font-semibold text-sm">启用状态</label>
        <Select
          id="is_active"
          v-model="store.filters.is_active"
          :options="statusOptions"
          optionLabel="label"
          optionValue="value"
          placeholder="全部状态"
          class="w-full md:w-48"
          showClear
        />
      </div>

      <!-- 用户选择 (仅管理员可见) -->
      <div v-if="authStore.isAdmin" class="flex flex-col gap-2">
        <label for="target-user" class="font-semibold text-sm">目标用户</label>
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
        />
      </div>

      <!-- 操作按钮 -->
      <div class="flex gap-2 ml-auto">
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
          @click="store.generateConfig"
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
</script>

<style scoped>
.card {
  border: 1px solid var(--surface-border);
  border-radius: 6px;
}
</style>
