<!-- 节点列表筛选组件 -->
<template>
  <div
    class="bg-surface-0 dark:bg-surface-900 p-4 rounded-lg shadow-sm mb-4 border border-surface-200 dark:border-surface-700"
  >
    <div class="flex flex-wrap gap-4 items-end">
      <!-- 分组筛选 -->
      <div class="flex flex-col gap-2">
        <label for="server_group" class="text-sm font-medium">节点分组</label>
        <InputText
          id="server_group"
          v-model="filters.server_group"
          placeholder="搜索分组..."
          class="w-full md:w-48"
        />
      </div>

      <!-- IP 筛选 -->
      <div class="flex flex-col gap-2">
        <label for="ip_address" class="text-sm font-medium">IP 地址</label>
        <InputText
          id="ip_address"
          v-model="filters.ip_address"
          placeholder="搜索 IP..."
          class="w-full md:w-48"
        />
      </div>

      <!-- 域名筛选 -->
      <div class="flex flex-col gap-2">
        <label for="domain" class="text-sm font-medium">域名</label>
        <InputText
          id="domain"
          v-model="filters.domain"
          placeholder="搜索域名..."
          class="w-full md:w-48"
        />
      </div>

      <!-- 状态筛选 -->
      <div class="flex flex-col gap-2">
        <label for="is_active" class="text-sm font-medium">启用状态</label>
        <Select
          id="is_active"
          v-model="filters.is_active"
          :options="statusOptions"
          optionLabel="label"
          optionValue="value"
          placeholder="全部状态"
          class="w-full md:w-48"
          showClear
        />
      </div>

      <!-- 到期时间范围
      <div class="flex flex-col gap-2">
        <label class="text-sm font-medium">到期时间 (起)</label>
        <DatePicker
          v-model="expireFromDate"
          showIcon
          placeholder="开始日期"
          class="w-full md:w-48"
          @update:modelValue="onExpireFromChange"
        />
      </div>

      <div class="flex flex-col gap-2">
        <label class="text-sm font-medium">到期时间 (止)</label>
        <DatePicker
          v-model="expireToDate"
          showIcon
          placeholder="结束日期"
          class="w-full md:w-48"
          @update:modelValue="onExpireToChange"
        />
      </div> -->

      <!-- 操作按钮 -->
      <div class="flex gap-2 ml-auto">
        <Button label="重置" icon="pi pi-refresh" severity="secondary" @click="handleReset" />
        <Button label="查询" icon="pi pi-search" @click="$emit('search')" />
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

const props = defineProps<{
  filters: NodeQueryParams
}>()

const emit = defineEmits(['search', 'reset'])

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
  { deep: true }
)

/**
 * 处理到期时间(起)变化
 */
function onExpireFromChange(date: any) {
  const finalDate = date instanceof Date ? date : null
  props.filters.expire_from = finalDate ? Math.floor(finalDate.getTime() / 1000) : undefined
}

/**
 * 处理到期时间(止)变化
 */
function onExpireToChange(date: any) {
  const finalDate = date instanceof Date ? date : null
  props.filters.expire_to = finalDate ? Math.floor(finalDate.getTime() / 1000) : undefined
}

/**
 * 重置操作
 */
function handleReset() {
  expireFromDate.value = null
  expireToDate.value = null
  emit('reset')
}
</script>

<style lang="css" scoped>
:deep(.p-select-input) {
  width: 100%;
}
</style>
