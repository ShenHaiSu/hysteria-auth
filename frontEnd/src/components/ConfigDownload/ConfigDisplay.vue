<template>
  <div class="mt-6">
    <div class="flex items-center justify-between mb-2">
      <h3 class="text-lg font-semibold">生成的配置内容</h3>
      <span class="text-xs text-surface-500 italic">单击全选, 双击复制</span>
    </div>
    <Textarea
      v-model="store.generatedConfig"
      readonly
      rows="15"
      class="w-full font-mono text-sm p-4 bg-surface-50 dark:bg-surface-950"
      placeholder="点击上方生成按钮获取配置..."
      @click="handleSelectAll"
      @dblclick="handleCopy"
    />
  </div>
</template>

<script setup lang="ts">
import { useConfigDownloadStore } from '@/stores/configDownload'
import Textarea from 'primevue/textarea'
import { useToast } from 'primevue/usetoast'

/**
 * 配置展示组件逻辑
 */
const store = useConfigDownloadStore()
const toast = useToast()

/**
 * 鼠标单击时全选 textarea 内容
 * @param event 鼠标事件
 */
const handleSelectAll = (event: MouseEvent) => {
  const target = event.target as HTMLTextAreaElement
  if (target && store.generatedConfig) {
    target.select()
  }
}

/**
 * 鼠标双击时复制内容到剪贴板
 */
const handleCopy = async () => {
  if (!store.generatedConfig) return

  try {
    await navigator.clipboard.writeText(store.generatedConfig)
    toast.add({
      severity: 'success',
      summary: '复制成功',
      detail: '配置已复制到剪贴板',
      life: 3000
    })
  } catch (err) {
    toast.add({
      severity: 'error',
      summary: '复制失败',
      detail: '请手动复制内容',
      life: 3000
    })
  }
}
</script>

<style scoped>
textarea {
  resize: vertical;
  line-height: 1.5;
}
</style>
