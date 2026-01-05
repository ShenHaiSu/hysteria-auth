<!-- 用户管理工具栏：包含搜索和新增按钮 -->
<template>
  <div
    class="bg-surface-0 dark:bg-surface-900 p-4 rounded-lg shadow-sm mb-4 border border-surface-200 dark:border-surface-700"
  >
    <div class="flex flex-col md:flex-row md:items-end gap-4">
      <!-- 搜索筛选 -->
      <div class="flex flex-col gap-2 grow">
        <label for="user_search" class="text-sm font-medium text-surface-600 dark:text-surface-400"
          >用户搜索</label
        >
        <IconField iconPosition="left" class="w-full">
          <InputIcon class="pi pi-search" />
          <InputText
            id="user_search"
            v-model="searchValue"
            placeholder="搜索用户名或邮箱..."
            class="w-full md:w-64"
            @keyup.enter="handleSearch"
          />
        </IconField>
      </div>

      <!-- 操作按钮 -->
      <div class="flex flex-wrap items-center gap-2 w-full md:w-auto md:ml-auto">
        <div class="flex gap-2 flex-1 md:flex-none">
          <Button
            label="重置"
            icon="pi pi-refresh"
            severity="secondary"
            class="flex-1 md:flex-none"
            @click="handleReset"
          ></Button>
          <Button
            label="查询"
            icon="pi pi-search"
            class="flex-1 md:flex-none"
            @click="handleSearch"
          ></Button>
        </div>
        <Button
          label="新增用户"
          icon="pi pi-plus"
          severity="primary"
          class="w-full md:w-auto"
          @click="$emit('add')"
        ></Button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import Button from 'primevue/button'
import InputText from 'primevue/inputtext'
import IconField from 'primevue/iconfield'
import InputIcon from 'primevue/inputicon'

// #region 属性与事件定义
const props = defineProps<{
  initialSearch?: string
}>()

const emit = defineEmits<{
  (e: 'search', value: string): void
  (e: 'add'): void
}>()
// #endregion

// #region 状态定义
const searchValue = ref(props.initialSearch || '')
// #endregion

// #region 逻辑处理
/**
 * 处理搜索操作
 * 触发 'search' 事件并传递当前搜索关键词
 */
const handleSearch = () => {
  emit('search', searchValue.value)
}

/**
 * 处理重置操作
 * 清空搜索关键词并触发 'search' 事件
 */
const handleReset = () => {
  searchValue.value = ''
  emit('search', '')
}
// #endregion
</script>
