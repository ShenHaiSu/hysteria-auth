<!-- 节点列表 - 移动端卡片视图 -->
<template>
  <div class="space-y-3">
    <div
      v-if="loading"
      class="flex flex-col items-center justify-center py-12 bg-surface-0 dark:bg-surface-900 rounded-lg border border-surface-200 dark:border-surface-700"
    >
      <i class="pi pi-spin pi-spinner text-3xl text-primary-500 mb-2"></i>
      <span class="text-surface-500">加载中...</span>
    </div>
    <template v-else-if="nodes.length > 0">
      <div
        v-for="node in nodes"
        :key="node.id"
        class="bg-surface-0 dark:bg-surface-900 rounded-lg shadow-sm border border-surface-200 dark:border-surface-700 p-4 space-y-4"
      >
        <!-- 卡片头部: 分组和状态 -->
        <div class="flex justify-between items-start">
          <div class="flex flex-col gap-1">
            <Tag :value="node.server_group" severity="info" class="w-fit" />
            <div class="flex items-center gap-1 text-surface-500 dark:text-surface-400 mt-1">
              <i class="pi pi-building text-xs"></i>
              <span class="text-xs font-medium">{{ node.idc_name || '未知 IDC' }}</span>
            </div>
          </div>
          <Tag
            :value="node.is_active ? '运行中' : '已停止'"
            :severity="node.is_active ? 'success' : 'danger'"
            class="px-2"
          />
        </div>

        <!-- 卡片主体: 网络和配置信息 -->
        <div
          class="grid grid-cols-2 gap-4 text-sm bg-surface-50 dark:bg-surface-800/50 p-3 rounded-md"
        >
          <div class="col-span-2">
            <div
              class="text-surface-500 dark:text-surface-400 text-[10px] uppercase tracking-wider mb-1"
            >
              连接地址
            </div>
            <div class="font-mono text-surface-900 dark:text-surface-0 break-all">
              {{ node.ip_address }}
            </div>
            <div v-if="node.domain" class="text-xs text-surface-500 mt-1 truncate">
              {{ node.domain }}
            </div>
          </div>

          <div>
            <div
              class="text-surface-500 dark:text-surface-400 text-[10px] uppercase tracking-wider mb-1"
            >
              端口配置
            </div>
            <div class="flex flex-col gap-1">
              <div class="flex items-center gap-1">
                <span class="text-[10px] bg-surface-200 dark:bg-surface-700 px-1 rounded">代理</span>
                <span class="font-mono">{{ node.proxy_port }}</span>
              </div>
              <div v-if="node.server_port" class="flex items-center gap-1">
                <span
                  class="text-[10px] bg-orange-100 dark:bg-orange-900/30 text-orange-600 dark:text-orange-400 px-1 rounded"
                  >服务</span
                >
                <span class="font-mono">{{ node.server_port }}</span>
              </div>
            </div>
          </div>

          <div>
            <div
              class="text-surface-500 dark:text-surface-400 text-[10px] uppercase tracking-wider mb-1"
            >
              费用 / 到期
            </div>
            <div class="text-primary-600 dark:text-primary-400 font-bold">
              {{ formatCurrency(node.fee) }}
            </div>
            <div class="flex flex-col mt-1">
              <span class="text-[10px]">{{ formatTimestamp(node.expire_ts) }}</span>
              <span
                v-if="node.expire_ts"
                :class="getExpireClass(node.expire_ts)"
                class="text-[10px] font-bold"
              >
                {{ getExpireStatus(node.expire_ts) }}
              </span>
            </div>
          </div>
        </div>

        <!-- 备注展示 -->
        <div v-if="node.note1 || node.note2 || node.note3 || node.note4" class="text-xs">
          <div
            class="text-surface-500 dark:text-surface-400 text-[10px] uppercase tracking-wider mb-1"
          >
            备注信息
          </div>
          <div
            class="p-2 bg-surface-50 dark:bg-surface-800/30 rounded border-l-2 border-primary-500 text-surface-600 dark:text-surface-300"
          >
            {{ getNotesTooltip(node) }}
          </div>
        </div>

        <!-- 操作按钮组 -->
        <div class="grid grid-cols-2 gap-2 pt-2">
          <Button
            icon="pi pi-pencil"
            label="编辑"
            severity="secondary"
            outlined
            size="small"
            @click="$emit('edit', node)"
          />
          <Button
            icon="pi pi-stop"
            :label="node.is_active ? '停用节点' : '已停用'"
            severity="danger"
            outlined
            size="small"
            :disabled="!node.is_active"
            @click="$emit('stop', node)"
          />
          <Button
            icon="pi pi-copy"
            label="快速复用"
            severity="info"
            outlined
            size="small"
            @click="$emit('copy', node)"
          />
          <Button
            icon="pi pi-trash"
            label="删除节点"
            severity="danger"
            size="small"
            @click="$emit('delete', node)"
          />
        </div>
      </div>
    </template>
    <div
      v-else
      class="flex flex-col items-center justify-center py-12 bg-surface-0 dark:bg-surface-900 rounded-lg border border-dashed border-surface-300 dark:border-surface-700"
    >
      <i class="pi pi-inbox text-4xl text-surface-300 dark:text-surface-600 mb-2"></i>
      <p class="text-surface-500 dark:text-surface-400">暂无节点数据</p>
    </div>
  </div>
</template>

<script setup lang="ts">
import type { NodeServer } from '@/composable/node'
import Tag from 'primevue/tag'
import Button from 'primevue/button'

defineProps<{
  nodes: NodeServer[]
  loading: boolean
}>()

defineEmits(['edit', 'delete', 'stop', 'copy'])

/**
 * 格式化货币
 * @param value 金额
 */
function formatCurrency(value: number) {
  return new Intl.NumberFormat('zh-CN', { style: 'currency', currency: 'CNY' }).format(value)
}

/**
 * 获取备注提示文本
 * @param data 节点数据
 */
function getNotesTooltip(data: NodeServer) {
  const notes = [data.note1, data.note2, data.note3, data.note4].filter(Boolean)
  return notes.join(' | ') || '无备注'
}

/**
 * 格式化时间戳
 * @param ts Unix 时间戳
 */
function formatTimestamp(ts: number) {
  if (!ts || ts === 0) return '长期有效'
  const date = new Date(ts * 1000)
  return date.toLocaleString('zh-CN', {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
  })
}

/**
 * 获取到期状态文字
 * @param ts Unix 时间戳
 */
function getExpireStatus(ts: number) {
  if (!ts) return ''
  const now = Math.floor(Date.now() / 1000)
  const diff = ts - now
  if (diff < 0) return '已过期'
  const days = Math.floor(diff / 86400)
  if (days === 0) return '今日到期'
  if (days < 7) return `${days} 天后到期`
  return ''
}

/**
 * 获取到期状态样式类
 * @param ts Unix 时间戳
 */
function getExpireClass(ts: number) {
  if (!ts) return ''
  const now = Math.floor(Date.now() / 1000)
  const diff = ts - now
  if (diff < 0) return 'text-red-500 font-medium'
  if (diff < 86400 * 7) return 'text-orange-500 font-medium'
  return 'text-surface-500'
}
</script>
