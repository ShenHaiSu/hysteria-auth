<!-- 节点列表筛选组件 -->
<template>
  <div
    class="bg-surface-0 dark:bg-surface-900 p-4 rounded-lg shadow-sm mb-4 border border-surface-200 dark:border-surface-700"
  >
    <div class="flex flex-col md:flex-row md:flex-wrap md:items-end gap-4">
      <!-- 分组筛选 -->
      <div class="flex flex-col gap-2 w-full md:w-auto">
        <label for="server_group" class="text-sm font-medium text-surface-600 dark:text-surface-400"
          >节点分组</label
        >
        <InputText
          id="server_group"
          v-model="filters.server_group"
          placeholder="搜索分组..."
          class="w-full md:w-48"
          @keyup.enter="handleSearch"
        />
      </div>

      <!-- IP 筛选 -->
      <div class="flex flex-col gap-2 w-full md:w-auto">
        <label for="ip_address" class="text-sm font-medium text-surface-600 dark:text-surface-400"
          >IP 地址</label
        >
        <InputText
          id="ip_address"
          v-model="filters.ip_address"
          placeholder="搜索 IP..."
          class="w-full md:w-48"
          @keyup.enter="handleSearch"
        />
      </div>

      <!-- 域名筛选 -->
      <div class="flex flex-col gap-2 w-full md:w-auto">
        <label for="domain" class="text-sm font-medium text-surface-600 dark:text-surface-400"
          >域名</label
        >
        <InputText
          id="domain"
          v-model="filters.domain"
          placeholder="搜索域名..."
          class="w-full md:w-48"
          @keyup.enter="handleSearch"
        />
      </div>

      <!-- 状态筛选 -->
      <div class="flex flex-col gap-2 w-full md:w-auto">
        <label for="is_active" class="text-sm font-medium text-surface-600 dark:text-surface-400"
          >启用状态</label
        >
        <Select
          id="is_active"
          v-model="filters.is_active"
          :options="statusOptions"
          optionLabel="label"
          optionValue="value"
          placeholder="全部状态"
          class="w-full md:w-48"
          showClear
          @change="handleSearch"
        />
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
          />
          <Button
            label="查询"
            icon="pi pi-search"
            class="flex-1 md:flex-none"
            @click="handleSearch"
          />
        </div>
        <Button
          label="新增节点"
          icon="pi pi-plus"
          severity="primary"
          class="w-full md:w-auto"
          @click="$emit('add')"
        />
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue'
import type { NodeQueryParams } from '@/composable/node'
import InputText from 'primevue/inputtext'
import Select from 'primevue/select'
import Button from 'primevue/button'
import DatePicker from 'primevue/datepicker'

// #region 属性与事件定义
const props = defineProps<{
  filters: NodeQueryParams
}>()

const emit = defineEmits(['search', 'reset', 'add'])
// #endregion

// #region 状态定义
/**
 * 状态选项定义
 */
const statusOptions = ref([
  { label: '已启用', value: 1 },
  { label: '已禁用', value: 0 },
])

const expireFromDate = ref<Date | null>(null)
const expireToDate = ref<Date | null>(null)

// 监听 filters 变化同步内部 Date 对象 (主要用于重置)
watch(
  () => props.filters,
  (newVal) => {
    if (!newVal.expire_from) expireFromDate.value = null
    if (!newVal.expire_to) expireToDate.value = null
  },
  { deep: true },
)
// #endregion

// #region 逻辑处理
/**
 * 处理到期时间(起)变化
 * 将 Date 对象转换为秒级时间戳并更新 filters
 * @param date 选中的日期对象
 */
function onExpireFromChange(date: any) {
  const finalDate = date instanceof Date ? date : null
  props.filters.expire_from = finalDate ? Math.floor(finalDate.getTime() / 1000) : undefined
}

/**
 * 处理到期时间(止)变化
 * 将 Date 对象转换为秒级时间戳并更新 filters
 * @param date 选中的日期对象
 */
function onExpireToChange(date: any) {
  const finalDate = date instanceof Date ? date : null
  props.filters.expire_to = finalDate ? Math.floor(finalDate.getTime() / 1000) : undefined
}

/**
 * 触发查询事件
 * 通知父组件执行搜索逻辑
 */
function handleSearch() {
  emit('search')
}

/**
 * 执行重置操作
 * 清空内部日期状态并通知父组件重置筛选条件
 */
function handleReset() {
  expireFromDate.value = null
  expireToDate.value = null
  emit('reset')
}
// #endregion
</script>

<style lang="css" scoped>
:deep(.p-select-input) {
  width: 100%;
}
</style>
