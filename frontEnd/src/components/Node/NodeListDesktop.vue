<!-- 节点列表 - 桌面端表格视图 -->
<template>
  <div
    class="bg-surface-0 dark:bg-surface-900 rounded-lg shadow-sm border border-surface-200 dark:border-surface-700 overflow-hidden"
  >
    <DataTable
      :value="nodes"
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
          <span>未找到匹配的节点</span>
        </div>
      </template>

      <Column field="server_group" header="分组" sortable>
        <template #body="{ data }">
          <Tag :value="data.server_group" severity="info" class="px-2" />
        </template>
      </Column>

      <Column field="ip_address" header="IP 地址" sortable>
        <template #body="{ data }">
          <span class="font-mono text-sm">{{ data.ip_address }}</span>
        </template>
      </Column>

      <Column field="domain" header="域名" sortable>
        <template #body="{ data }">
          <span class="text-sm">{{ data.domain || '-' }}</span>
        </template>
      </Column>

      <Column field="idc_name" header="IDC" sortable>
        <template #body="{ data }">
          <div class="flex items-center gap-2">
            <i class="pi pi-building text-surface-400 text-xs"></i>
            <span class="text-sm">{{ data.idc_name || '-' }}</span>
          </div>
        </template>
      </Column>

      <Column field="proxy_port" header="端口" sortable>
        <template #body="{ data }">
          <div class="flex flex-col gap-1">
            <div class="flex items-center gap-1">
              <Tag value="代理" severity="secondary" class="scale-75 origin-left" />
              <span class="text-sm font-mono">{{ data.proxy_port }}</span>
            </div>
            <div v-if="data.server_port" class="flex items-center gap-1">
              <Tag value="服务" severity="warn" class="scale-75 origin-left" />
              <span class="text-sm font-mono">{{ data.server_port }}</span>
            </div>
          </div>
        </template>
      </Column>

      <Column field="fee" header="费用" sortable>
        <template #body="{ data }">
          <span class="text-sm font-medium text-surface-700 dark:text-surface-200">
            {{ formatCurrency(data.fee) }}
          </span>
        </template>
      </Column>

      <Column field="is_active" header="状态" sortable>
        <template #body="{ data }">
          <Tag
            :value="data.is_active ? '启用' : '停用'"
            :severity="data.is_active ? 'success' : 'danger'"
          />
        </template>
      </Column>

      <Column field="expire_ts" header="到期时间" sortable>
        <template #body="{ data }">
          <div class="flex flex-col">
            <span class="text-sm">{{ formatTimestamp(data.expire_ts) }}</span>
            <small
              v-if="data.expire_ts"
              :class="getExpireClass(data.expire_ts)"
              class="text-[10px] mt-1"
            >
              {{ getExpireStatus(data.expire_ts) }}
            </small>
          </div>
        </template>
      </Column>

      <Column header="备注">
        <template #body="{ data }">
          <div class="flex gap-1">
            <i
              v-if="data.note1 || data.note2 || data.note3 || data.note4"
              class="pi pi-info-circle text-surface-400 cursor-help"
              v-tooltip.top="getNotesTooltip(data)"
            >
            </i>
            <span v-else class="text-surface-400">-</span>
          </div>
        </template>
      </Column>

      <Column header="操作" :exportable="false">
        <template #body="{ data }">
          <div class="flex gap-3 justify-end">
            <Button
              icon="pi pi-pencil"
              label="编辑"
              severity="secondary"
              size="small"
              v-tooltip.top="'编辑'"
              @click="$emit('edit', data)"
            ></Button>
            <Button
              icon="pi pi-stop"
              :label="data.is_active ? '停用' : '已停用'"
              severity="danger"
              size="small"
              :disabled="!data.is_active"
              v-tooltip.top="data.is_active ? '停用' : '已停用'"
              @click="$emit('stop', data)"
            ></Button>
            <Button
              icon="pi pi-copy"
              label="复用"
              severity="info"
              size="small"
              v-tooltip.top="'复用'"
              @click="$emit('copy', data)"
            ></Button>
            <Button
              icon="pi pi-trash"
              label="删除"
              severity="danger"
              size="small"
              v-tooltip.top="'删除'"
              @click="$emit('delete', data)"
            ></Button>
          </div>
        </template>
      </Column>
    </DataTable>
  </div>
</template>

<script setup lang="ts">
import type { NodeServer } from '@/composable/node'
import DataTable from 'primevue/datatable'
import Column from 'primevue/column'
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
