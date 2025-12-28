<!-- 用户管理工具栏：包含搜索和新增按钮 -->
<template>
  <div class="flex flex-wrap items-center justify-end gap-4 mb-4">
    <div class="flex items-center gap-3">
      <IconField iconPosition="left">
        <InputIcon class="pi pi-search" />
        <InputText
          v-model="searchValue"
          placeholder="搜索用户名或邮箱..."
          class="w-full md:w-64"
          @keyup.enter="handleSearch"
        />
      </IconField>
      <Button label="搜索" icon="pi pi-search" severity="secondary" @click="handleSearch"></Button>
      <Button label="新增用户" icon="pi pi-plus" severity="primary" @click="$emit('add')"></Button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import Button from 'primevue/button'
import InputText from 'primevue/inputtext'
import IconField from 'primevue/iconfield'
import InputIcon from 'primevue/inputicon'

// #region 初始化与事件定义
const props = defineProps<{
  initialSearch?: string
}>()

const emit = defineEmits<{
  (e: 'search', value: string): void
  (e: 'add'): void
}>()

const searchValue = ref(props.initialSearch || '')
// #endregion

// #region 逻辑处理
/**
 * 触发搜索事件
 */
const handleSearch = () => {
  emit('search', searchValue.value)
}
// #endregion
</script>
