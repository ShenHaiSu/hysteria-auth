<!-- 用户列表 - 桌面端表格视图 -->
<template>
  <div
    class="bg-surface-0 dark:bg-surface-900 rounded-lg shadow-sm border border-surface-200 dark:border-surface-700 overflow-hidden"
  >
    <DataTable
      :value="users"
      :loading="loading"
      dataKey="id"
      responsiveLayout="scroll"
      stripedRows
      removableSort
      rowHover
      class="p-datatable-sm"
      :pt="{
        column: {
          headerCell: {
            class:
              'bg-surface-50 dark:bg-surface-800/50 text-surface-700 dark:text-surface-0/70 font-semibold py-3 border-b border-surface-200 dark:border-surface-700',
          },
          bodyCell: { class: 'py-3 border-b border-surface-100 dark:border-surface-800' },
        },
      }"
    >
      <template #empty>
        <div
          class="flex flex-col items-center justify-center py-8 text-surface-500 dark:text-surface-400"
        >
          <i class="pi pi-inbox text-4xl mb-2"></i>
          <span>未找到用户信息</span>
        </div>
      </template>
      <template #loading> 正在加载用户数据，请稍候... </template>

      <Column field="id" header="ID" sortable style="width: 5rem"></Column>
      <Column field="username" header="用户名" sortable></Column>
      <Column field="email" header="邮箱" sortable></Column>
      <Column field="permission" header="权限">
        <template #body="slotProps">
          <Tag
            :value="slotProps.data.permission === 'admin' ? '管理' : '用户'"
            :severity="slotProps.data.permission === 'admin' ? 'danger' : 'info'"
          />
        </template>
      </Column>
      <Column field="proxy_password" header="代理密码">
        <template #body="slotProps">
          <code
            class="text-primary font-bold cursor-pointer hover:underline"
            @click="copyToClipboard(slotProps.data.proxy_password)"
            v-tooltip.top="'点击复制'"
          >
            {{ slotProps.data.proxy_password }}
          </code>
        </template>
      </Column>
      <Column field="formatted_proxy_expire_at" header="代理到期" sortable></Column>
      <Column field="status_label" header="状态">
        <template #body="slotProps">
          <Tag
            :value="slotProps.data.status_label"
            :severity="slotProps.data.is_active ? 'success' : 'secondary'"
          />
        </template>
      </Column>
      <Column header="登录信息" class="text-sm">
        <template #body="slotProps">
          <div class="flex flex-col gap-1">
            <div class="flex items-center gap-1" v-if="slotProps.data.last_login_ip">
              <i class="pi pi-map-marker text-xs text-500"></i>
              <span>{{ slotProps.data.last_login_ip }}</span>
            </div>
            <div class="flex items-center gap-1">
              <i class="pi pi-clock text-xs text-500"></i>
              <span class="text-500">{{ slotProps.data.formatted_last_login_at }}</span>
            </div>
          </div>
        </template>
      </Column>
      <Column field="formatted_created_at" header="创建时间" sortable></Column>

      <Column header="操作" style="min-width: 8rem">
        <template #body="slotProps">
          <div class="flex gap-2">
            <Button
              icon="pi pi-pencil"
              outlined
              rounded
              severity="warn"
              @click="$emit('edit', slotProps.data)"
              v-tooltip.top="'编辑'"
            />
            <Button
              icon="pi pi-trash"
              outlined
              rounded
              severity="danger"
              @click="$emit('delete', slotProps.data)"
              v-tooltip.top="'删除'"
            />
          </div>
        </template>
      </Column>
    </DataTable>
  </div>
</template>

<script setup lang="ts">
import DataTable from 'primevue/datatable'
import Column from 'primevue/column'
import Button from 'primevue/button'
import Tag from 'primevue/tag'
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
