<!-- 用户列表 - 移动端卡片视图 -->
<template>
  <div class="space-y-3">
    <div
      v-if="loading"
      class="flex flex-col items-center justify-center py-12 bg-surface-0 dark:bg-surface-900 rounded-lg border border-surface-200 dark:border-surface-700"
    >
      <i class="pi pi-spin pi-spinner text-3xl text-primary-500 mb-2"></i>
      <span class="text-surface-500">加载中...</span>
    </div>
    <template v-else-if="users.length > 0">
      <div
        v-for="user in users"
        :key="user.id"
        class="bg-surface-0 dark:bg-surface-900 rounded-lg shadow-sm border border-surface-200 dark:border-surface-700 p-4 space-y-4"
      >
        <!-- 卡片头部: 用户名和权限 -->
        <div class="flex justify-between items-start">
          <div class="flex flex-col gap-1">
            <span class="text-lg font-bold text-surface-900 dark:text-surface-0">{{
              user.username
            }}</span>
            <div class="flex items-center gap-2">
              <Tag
                :value="user.permission === 'admin' ? '管理' : '用户'"
                :severity="user.permission === 'admin' ? 'danger' : 'info'"
                class="w-fit"
              />
              <Tag
                :value="user.status_label"
                :severity="user.is_active ? 'success' : 'secondary'"
                class="w-fit"
              />
            </div>
          </div>
          <div class="text-xs text-surface-500 font-mono">ID: {{ user.id }}</div>
        </div>

        <!-- 卡片主体: 用户详情 -->
        <div
          class="grid grid-cols-1 gap-3 text-sm bg-surface-50 dark:bg-surface-800/50 p-3 rounded-md"
        >
          <div>
            <div
              class="text-surface-500 dark:text-surface-400 text-[10px] uppercase tracking-wider mb-1"
            >
              邮箱地址
            </div>
            <div class="text-surface-900 dark:text-surface-0 break-all">
              {{ user.email }}
            </div>
          </div>

          <div>
            <div
              class="text-surface-500 dark:text-surface-400 text-[10px] uppercase tracking-wider mb-1"
            >
              代理密码 (点击复制)
            </div>
            <div
              class="font-mono text-primary-600 dark:text-primary-400 font-bold cursor-pointer hover:underline break-all"
              @click="copyToClipboard(user.proxy_password)"
            >
              {{ user.proxy_password }}
            </div>
          </div>

          <div class="grid grid-cols-2 gap-4">
            <div>
              <div
                class="text-surface-500 dark:text-surface-400 text-[10px] uppercase tracking-wider mb-1"
              >
                代理到期
              </div>
              <div class="text-surface-700 dark:text-surface-200">
                {{ user.formatted_proxy_expire_at }}
              </div>
            </div>
            <div>
              <div
                class="text-surface-500 dark:text-surface-400 text-[10px] uppercase tracking-wider mb-1"
              >
                创建时间
              </div>
              <div class="text-surface-700 dark:text-surface-200">
                {{ user.formatted_created_at }}
              </div>
            </div>
          </div>

          <div>
            <div
              class="text-surface-500 dark:text-surface-400 text-[10px] uppercase tracking-wider mb-1"
            >
              最后登录
            </div>
            <div class="flex flex-col gap-1">
              <div class="flex items-center gap-1" v-if="user.last_login_ip">
                <i class="pi pi-map-marker text-xs text-surface-400"></i>
                <span class="text-surface-700 dark:text-surface-200">{{ user.last_login_ip }}</span>
              </div>
              <div class="flex items-center gap-1">
                <i class="pi pi-clock text-xs text-surface-400"></i>
                <span class="text-surface-700 dark:text-surface-200">{{
                  user.formatted_last_login_at
                }}</span>
              </div>
            </div>
          </div>
        </div>

        <!-- 操作按钮组 -->
        <div class="grid grid-cols-2 gap-2 pt-2">
          <Button
            icon="pi pi-pencil"
            label="编辑用户"
            severity="warn"
            outlined
            size="small"
            @click="$emit('edit', user)"
          />
          <Button
            icon="pi pi-trash"
            label="删除用户"
            severity="danger"
            outlined
            size="small"
            @click="$emit('delete', user)"
          />
        </div>
      </div>
    </template>
    <div
      v-else
      class="flex flex-col items-center justify-center py-12 bg-surface-0 dark:bg-surface-900 rounded-lg border border-dashed border-surface-300 dark:border-surface-700"
    >
      <i class="pi pi-inbox text-4xl text-surface-300 dark:text-surface-600 mb-2"></i>
      <p class="text-surface-500 dark:text-surface-400">暂无用户数据</p>
    </div>
  </div>
</template>

<script setup lang="ts">
import Tag from 'primevue/tag'
import Button from 'primevue/button'
import { useToast } from 'primevue/usetoast'
import type { UserVO } from '@/composable/user'

// #region 定义属性和事件
defineProps<{
  users: UserVO[]
  loading: boolean
}>()

defineEmits<{
  (e: 'edit', user: UserVO): void
  (e: 'delete', user: UserVO): void
}>()

const toast = useToast()

/**
 * 复制文本到剪切板
 * @param text 要复制的文本
 */
const copyToClipboard = async (text: string) => {
  try {
    await navigator.clipboard.writeText(text)
    toast.add({
      severity: 'success',
      summary: '复制成功',
      detail: '代理密码已复制到剪切板',
      life: 1000,
    })
  } catch (err) {
    console.error('无法复制文本: ', err)
    toast.add({
      severity: 'error',
      summary: '复制失败',
      detail: '请手动复制',
      life: 1000,
    })
  }
}
// #endregion
</script>
