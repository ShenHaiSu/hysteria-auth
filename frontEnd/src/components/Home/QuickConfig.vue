<template>
  <Card class="h-full">
    <template #title>
      <div class="flex items-center gap-2">
        <i class="pi pi-bolt text-primary"></i>
        <span>快速配置</span>
      </div>
    </template>
    <template #content>
      <div v-if="nodeStore.loading" class="flex justify-center p-4">
        <i class="pi pi-spin pi-spinner text-2xl"></i>
      </div>
      <div v-else-if="nodeStore.proxyConfigs.length === 0" class="text-center p-4 text-surface-500">
        <i class="pi pi-inbox text-4xl mb-2"></i>
        <p>暂无可用配置</p>
      </div>
      <div v-else class="space-y-2">
        <div
          v-for="config in nodeStore.proxyConfigs.slice(0, 5)"
          :key="config.ip_address + config.proxy_port"
          class="flex items-center justify-between p-2 hover:bg-surface-50 rounded border border-surface-200 transition-colors"
        >
          <div class="flex flex-col">
            <span class="font-medium text-sm">{{ config.server_group }}</span>
            <span class="text-xs text-surface-400"
              >{{ config.domain || config.ip_address }}:{{ config.proxy_port }}</span
            >
          </div>
          <Button icon="pi pi-copy" v-tooltip="'复制配置链接'" @click="copyConfig(config)"></Button>
        </div>
        <div
          v-if="nodeStore.proxyConfigs.length > 5"
          class="text-center text-xs text-surface-400 mt-2"
        >
          更多配置请前往配置页面查看
        </div>
      </div>
      <div class="mt-4 flex justify-end">
        <Button
          label="查看全部"
          icon="pi pi-list"
          size="small"
          text
          @click="$router.push('/config-download')"
        />
      </div>
    </template>
  </Card>
</template>

<script setup lang="ts">
import type { ProxyConfigItem } from '@/composable/proxy'
import { onMounted } from 'vue'
import Card from 'primevue/card'
import Button from 'primevue/button'
import { useNodeStore } from '@/stores/node'
import { useConfigDownloadStore } from '@/stores/configDownload'
import { useToast } from 'primevue/usetoast'

// 常变量初始化
const nodeStore = useNodeStore()
const configDownloadStore = useConfigDownloadStore()
const toast = useToast()

// #region 方法 (Methods)
/**
 * 生成并复制配置链接
 * @param node 节点信息
 */
async function copyConfig(node: ProxyConfigItem) {
  try {
    const configLink = await configDownloadStore.generateSingleConfig(node)
    if (!configLink) {
      toast.add({ severity: 'error', summary: '失败', detail: '生成配置链接失败', life: 1000 })
      return
    }

    await navigator.clipboard.writeText(configLink)

    toast.add({
      severity: 'success',
      summary: '成功',
      detail: '配置链接已复制到剪贴板',
      life: 1000,
    })
  } catch (err) {
    console.error('复制失败:', err)
    toast.add({
      severity: 'error',
      summary: '失败',
      detail: '复制失败，请手动复制',
      life: 1000,
    })
  }
}
// #endregion

onMounted(() => {
  if (nodeStore.proxyConfigs.length === 0) {
    nodeStore.fetchProxyConfigs()
  }
})
</script>
