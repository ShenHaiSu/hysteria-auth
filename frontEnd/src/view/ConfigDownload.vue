<template>
  <div class="config-download p-4">
    <!-- 1. 标题区域 -->
    <div class="mb-6">
      <h1 class="text-2xl font-bold">代理配置下载</h1>
    </div>

    <!-- 2. 筛选过滤以及生成的触发按钮区域 -->
    <ConfigFilter />

    <!-- 3. 配置内容展示区域 -->
    <ConfigDisplay />

    <!-- 全局通知组件 -->
    <Toast />
  </div>
</template>

<script setup lang="ts">
import ConfigFilter from '@/components/ConfigDownload/ConfigFilter.vue'
import ConfigDisplay from '@/components/ConfigDownload/ConfigDisplay.vue'
import Toast from 'primevue/toast'

/**
 * 代理配置下载页面
 * 负责组装筛选器和展示器
 */
</script>

<style scoped>
.config-download {
  margin: 0 auto;
}
</style>
