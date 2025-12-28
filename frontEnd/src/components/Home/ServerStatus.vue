<template>
  <Card class="h-full">
    <template #title>
      <div class="flex items-center gap-2">
        <i class="pi pi-server text-primary"></i>
        <span>服务器状态</span>
      </div>
    </template>
    <template #content>
      <div v-if="loading" class="flex justify-center p-4">
        <i class="pi pi-spin pi-spinner text-2xl"></i>
      </div>
      <div v-else-if="status" class="space-y-3">
        <div class="flex justify-between items-center">
          <span class="text-surface-500">主机名</span>
          <span class="font-medium">{{ status.os.hostname }}</span>
        </div>
        <div class="flex justify-between items-center">
          <span class="text-surface-500">操作系统</span>
          <span class="font-medium">{{ status.os.platform }}</span>
        </div>
        <div class="flex justify-between items-center">
          <span class="text-surface-500">运行时间</span>
          <span class="font-medium">{{ formatUptime(status.os.uptime_sec) }}</span>
        </div>
        <div class="flex justify-between items-center">
          <span class="text-surface-500">CPU</span>
          <span class="font-medium text-right text-xs"
            >{{ status.cpu.model }} ({{ status.cpu.count }}核)</span
          >
        </div>
        <div class="flex flex-col gap-1">
          <div class="flex justify-between text-sm">
            <span class="text-surface-500">内存使用率</span>
            <span>{{ memoryUsage }}%</span>
          </div>
          <ProgressBar :value="memoryUsage" :show-value="false" style="height: 6px" />
          <div class="flex justify-between text-xs text-surface-400">
            <span>{{ formatBytes(status.memory.total_bytes - status.memory.free_bytes) }}</span>
            <span>{{ formatBytes(status.memory.total_bytes) }}</span>
          </div>
        </div>
      </div>
    </template>
  </Card>
</template>

<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import Card from 'primevue/card'
import ProgressBar from 'primevue/progressbar'
import { getServerStatus } from '@/fetch/status'
import type { ServerStatus } from '@/composable/status'

// #region 状态 (State)
const status = ref<ServerStatus | null>(null)
const loading = ref(true)
// #endregion

// #region 计算属性 (Computed)
/**
 * 计算内存使用率百分比
 */
const memoryUsage = computed(() => {
  if (!status.value) return 0
  const used = status.value.memory.total_bytes - status.value.memory.free_bytes
  return Math.round((used / status.value.memory.total_bytes) * 100)
})
// #endregion

// #region 方法 (Methods)
/**
 * 格式化字节数为人类可读格式
 * @param bytes 字节数
 */
function formatBytes(bytes: number) {
  if (bytes === 0) return '0 B'
  const k = 1024
  const sizes = ['B', 'KB', 'MB', 'GB', 'TB']
  const i = Math.floor(Math.log(bytes) / Math.log(k))
  return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i]
}

/**
 * 格式化运行时间（秒）为人类可读格式
 * @param seconds 秒数
 */
function formatUptime(seconds: number) {
  const days = Math.floor(seconds / (24 * 3600))
  const hours = Math.floor((seconds % (24 * 3600)) / 3600)
  const minutes = Math.floor((seconds % 3600) / 60)

  const parts = []
  if (days > 0) parts.push(`${days}天`)
  if (hours > 0) parts.push(`${hours}小时`)
  if (minutes > 0) parts.push(`${minutes}分钟`)

  return parts.join(' ') || '< 1分钟'
}

/**
 * 获取服务器状态数据
 */
async function fetchStatus() {
  loading.value = true
  try {
    status.value = await getServerStatus()
  } catch (error) {
    console.error('获取服务器状态失败:', error)
  } finally {
    loading.value = false
  }
}
// #endregion

onMounted(() => {
  fetchStatus()
})
</script>
