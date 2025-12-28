<template>
  <Card class="h-full">
    <template #title>
      <div class="flex items-center gap-2">
        <i class="pi pi-info-circle text-primary"></i>
        <span>访问信息</span>
      </div>
    </template>
    <template #content>
      <div v-if="loading" class="flex justify-center p-4">
        <i class="pi pi-spin pi-spinner text-2xl"></i>
      </div>
      <div v-else-if="info" class="space-y-3">
        <div class="flex flex-col gap-1">
          <span class="text-surface-500 text-sm">您的出口 IP</span>
          <div class="flex items-center gap-2">
            <span class="font-mono font-bold text-lg text-primary">{{ info.ip }}</span>
            <Tag severity="info" value="IPv4" v-if="!info.ip.includes(':')" />
            <Tag severity="info" value="IPv6" v-else />
          </div>
        </div>

        <Divider />

        <div class="flex flex-col gap-1">
          <span class="text-surface-500 text-sm">浏览器 User-Agent</span>
          <p class="text-xs break-all text-surface-600 leading-relaxed bg-surface-50 p-2 rounded">
            {{ info.ua }}
          </p>
        </div>

        <div class="flex justify-between items-center text-sm">
          <span class="text-surface-500">请求来源</span>
          <span class="font-medium">{{ info.origin || '直接访问' }}</span>
        </div>

        <div class="flex justify-between items-center text-sm">
          <span class="text-surface-500">请求时间</span>
          <span class="font-medium">{{ new Date(info.time.now_iso).toLocaleString() }}</span>
        </div>
      </div>
    </template>
  </Card>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import Card from 'primevue/card'
import Tag from 'primevue/tag'
import Divider from 'primevue/divider'
import { getClientStatus } from '@/fetch/status'
import type { ClientStatus } from '@/composable/status'

// #region 状态 (State)
const info = ref<ClientStatus | null>(null)
const loading = ref(true)
// #endregion

// #region 方法 (Methods)
/**
 * 获取客户端状态信息
 */
async function fetchClientInfo() {
  loading.value = true
  try {
    info.value = await getClientStatus()
  } catch (error) {
    console.error('获取客户端信息失败:', error)
  } finally {
    loading.value = false
  }
}
// #endregion

onMounted(() => {
  fetchClientInfo()
})
</script>
