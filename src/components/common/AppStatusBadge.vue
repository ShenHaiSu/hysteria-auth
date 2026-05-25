<script setup lang="ts">
import { computed } from 'vue'

type StatusType = 'active' | 'inactive' | 'online' | 'offline' | 'pending' | 'provisioned'

const props = withDefaults(
  defineProps<{
    /** 状态类型 */
    type: StatusType
    /** 自定义文字（不传则自动匹配） */
    label?: string
  }>(),
  {
    label: undefined,
  },
)

interface StatusConfig {
  textColor: string
  bgColor: string
  icon: string
  defaultLabel: string
}

const statusMap: Record<StatusType, StatusConfig> = {
  active: {
    textColor: 'var(--status-active)',
    bgColor: 'var(--status-active-bg)',
    icon: '●',
    defaultLabel: '激活',
  },
  inactive: {
    textColor: 'var(--status-inactive)',
    bgColor: 'var(--status-inactive-bg)',
    icon: '○',
    defaultLabel: '禁用',
  },
  online: {
    textColor: 'var(--status-active)',
    bgColor: 'var(--status-active-bg)',
    icon: '●',
    defaultLabel: '在线',
  },
  offline: {
    textColor: 'var(--status-inactive)',
    bgColor: 'var(--status-inactive-bg)',
    icon: '○',
    defaultLabel: '离线',
  },
  pending: {
    textColor: 'var(--status-pending)',
    bgColor: 'var(--status-pending-bg)',
    icon: '◉',
    defaultLabel: '待注册',
  },
  provisioned: {
    textColor: 'var(--status-active)',
    bgColor: 'var(--status-active-bg)',
    icon: '●',
    defaultLabel: '已注册',
  },
}

const config = computed(() => statusMap[props.type])
</script>

<template>
  <span
    class="inline-flex items-center gap-1 px-2 py-0.5 rounded-sm text-xs font-medium whitespace-nowrap"
    :style="{
      color: config.textColor,
      backgroundColor: config.bgColor,
    }"
  >
    {{ config.icon }}
    {{ label ?? config.defaultLabel }}
  </span>
</template>
