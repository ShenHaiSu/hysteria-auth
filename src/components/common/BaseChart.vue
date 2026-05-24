<template>
  <div ref="chartRef" :style="{ width: '100%', height: height }" />
</template>

<script setup lang="ts">
import { ref, onMounted, onBeforeUnmount, watch } from 'vue'
import { useECharts, getChartColors, createTooltip, createGrid } from '@/composables/useECharts'
import { useThemeStore } from '@/stores/theme.store'
import type { ThemeMode } from '@/stores/theme.store'
import type { EChartsOption, ECharts } from 'echarts'

const props = withDefaults(
  defineProps<{
    option: EChartsOption
    height?: string
  }>(),
  {
    height: '320px',
  },
)

const chartRef = ref<HTMLElement>()
const { initChart, setOption, resize, dispose, getInstance } = useECharts()
const themeStore = useThemeStore()

/**
 * 根据当前主题模式注入颜色到 option
 */
function buildThemedOption(baseOption: EChartsOption): EChartsOption {
  const colors = getChartColors(themeStore.mode)

  // 深拷贝 option 并注入主题色
  const themed: EChartsOption = JSON.parse(JSON.stringify(baseOption))

  // 设置工具提示统一样式
  if (!themed.tooltip || (themed.tooltip as Record<string, unknown>).trigger === undefined) {
    themed.tooltip = createTooltip(colors)
  }

  // 设置 Grid 统一样式
  if (!themed.grid) {
    themed.grid = createGrid()
  }

  // 设置全局配色
  themed.color = themed.color ?? [
    colors.brand,
    colors.green,
    colors.amber,
    colors.red,
    colors.purple,
  ]

  // 设置图例样式
  if (themed.legend && typeof themed.legend === 'object') {
    const legend = themed.legend as Record<string, unknown>
    legend.textStyle = { color: colors.textSecondary, fontSize: 12 }
    legend.top = legend.top ?? 0
  }

  // 设置 X 轴样式
  const xAxisArr = Array.isArray(themed.xAxis) ? themed.xAxis : [themed.xAxis]
  for (const axis of xAxisArr) {
    if (axis && typeof axis === 'object') {
      const ax = axis as Record<string, unknown>
      ax.axisLine = ax.axisLine ?? { lineStyle: { color: colors.borderLight } }
      ax.axisTick = ax.axisTick ?? { show: false }
      ax.axisLabel = {
        ...((ax.axisLabel as Record<string, unknown>) ?? {}),
        color: colors.textSecondary,
        fontSize: 11,
      }
      ax.splitLine = ax.splitLine ?? {
        show: true,
        lineStyle: { color: colors.gridLine, type: 'dashed' as const },
      }
    }
  }

  // 设置 Y 轴样式
  const yAxisArr = Array.isArray(themed.yAxis) ? themed.yAxis : [themed.yAxis]
  for (const axis of yAxisArr) {
    if (axis && typeof axis === 'object') {
      const ax = axis as Record<string, unknown>
      ax.axisLine = ax.axisLine ?? { show: false }
      ax.axisTick = ax.axisTick ?? { show: false }
      ax.axisLabel = {
        ...((ax.axisLabel as Record<string, unknown>) ?? {}),
        color: colors.textSecondary,
        fontSize: 11,
      }
      ax.splitLine = ax.splitLine ?? {
        show: true,
        lineStyle: { color: colors.gridLine, type: 'dashed' as const },
      }
    }
  }

  // 设置 backgroundColor
  themed.backgroundColor = themed.backgroundColor ?? colors.bgColor

  return themed
}

function applyOption() {
  if (!getInstance()) return
  const themed = buildThemedOption(props.option)
  setOption(themed, true)
}

onMounted(() => {
  if (chartRef.value) {
    initChart(chartRef.value)
    applyOption()
    window.addEventListener('resize', resize)
  }
})

onBeforeUnmount(() => {
  window.removeEventListener('resize', resize)
  dispose()
})

// 监听 option 变化
watch(
  () => props.option,
  () => {
    applyOption()
  },
  { deep: true },
)

// 监听主题变化
watch(
  () => themeStore.mode,
  () => {
    if (getInstance()) {
      dispose()
      if (chartRef.value) {
        initChart(chartRef.value)
        applyOption()
      }
    }
  },
)

defineExpose({ getInstance, resize })
</script>
