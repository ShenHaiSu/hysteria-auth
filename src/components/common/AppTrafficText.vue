<script setup lang="ts">
import { computed } from 'vue'

const props = withDefaults(
  defineProps<{
    bytes: number
    showUnit?: boolean
    precise?: boolean
    class?: string
  }>(),
  {
    showUnit: true,
    precise: false,
    class: '',
  },
)

const formatted = computed(() => {
  if (props.bytes === 0) {
    return { value: '0', unit: 'B' }
  }

  const abs = Math.abs(props.bytes)
  let unitLabel = 'B'
  let divisor = 1
  let decimals = 0

  if (abs >= 1024 ** 5) {
    unitLabel = 'PB'
    divisor = 1024 ** 5
    decimals = 3
  } else if (abs >= 1024 ** 4) {
    unitLabel = 'TB'
    divisor = 1024 ** 4
    decimals = 3
  } else if (abs >= 1024 ** 3) {
    unitLabel = 'GB'
    divisor = 1024 ** 3
    decimals = 2
  } else if (abs >= 1024 ** 2) {
    unitLabel = 'MB'
    divisor = 1024 ** 2
    decimals = 2
  } else if (abs >= 1024) {
    unitLabel = 'KB'
    divisor = 1024
    decimals = 2
  }

  const value = props.bytes / divisor
  const finalDecimals = props.precise ? decimals : Math.min(decimals, 2)
  const formattedValue = value.toFixed(finalDecimals)

  return { value: formattedValue, unit: unitLabel }
})
</script>

<template>
  <span :class="class">
    {{ formatted.value }}
    <template v-if="showUnit">
      {{ formatted.unit }}
    </template>
  </span>
</template>
