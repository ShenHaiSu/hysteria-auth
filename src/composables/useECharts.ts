import * as echarts from 'echarts/core'
import { CanvasRenderer } from 'echarts/renderers'
import { LineChart, BarChart, PieChart } from 'echarts/charts'
import {
  TitleComponent,
  TooltipComponent,
  LegendComponent,
  GridComponent,
  ToolboxComponent,
  DataZoomComponent,
} from 'echarts/components'
import type { EChartsOption, ECharts } from 'echarts'
import type { ThemeMode } from '@/stores/theme.store'

// 按需注册
echarts.use([
  CanvasRenderer,
  LineChart,
  BarChart,
  PieChart,
  TitleComponent,
  TooltipComponent,
  LegendComponent,
  GridComponent,
  ToolboxComponent,
  DataZoomComponent,
])

/**
 * 亮色主题下的图表配色 (与 design-tokens 一致)
 */
const LIGHT_COLORS = {
  textPrimary: '#374151',
  textSecondary: '#6B7280',
  borderLight: '#E5E7EB',
  gridLine: '#F3F4F6',
  bgColor: '#FFFFFF',
  brand: '#3B82F6',
  brandLight: '#60A5FA',
  green: '#22C55E',
  greenLight: '#4ADE80',
  amber: '#F59E0B',
  amberLight: '#FBBF24',
  red: '#EF4444',
  redLight: '#F87171',
  purple: '#8B5CF6',
  purpleLight: '#A78BFA',
}

/**
 * 暗色主题下的图表配色
 */
const DARK_COLORS = {
  textPrimary: '#9CA3AF',
  textSecondary: '#6B7280',
  borderLight: '#374151',
  gridLine: '#374151',
  bgColor: '#1F2937',
  brand: '#60A5FA',
  brandLight: '#93C5FD',
  green: '#4ADE80',
  greenLight: '#86EFAC',
  amber: '#FBBF24',
  amberLight: '#FCD34D',
  red: '#F87171',
  redLight: '#FCA5A5',
  purple: '#A78BFA',
  purpleLight: '#C4B5FD',
}

/**
 * 根据主题模式获取图表颜色配置
 */
export function getChartColors(theme: ThemeMode | string) {
  return theme === 'dark' ? DARK_COLORS : LIGHT_COLORS
}

/**
 * ECharts 统一 tooltip 样式
 */
export function createTooltip(colors: typeof LIGHT_COLORS): Record<string, unknown> {
  return {
    trigger: 'axis',
    backgroundColor: colors.bgColor,
    borderColor: colors.borderLight,
    borderWidth: 1,
    textStyle: {
      color: colors.textPrimary,
      fontSize: 12,
    },
  }
}

/**
 * ECharts 统一 Grid 配置
 */
export function createGrid(): Record<string, unknown> {
  return {
    left: '3%',
    right: '4%',
    bottom: '3%',
    top: '12%',
    containLabel: true,
  }
}

export function useECharts() {
  let instance: ECharts | null = null

  function initChart(el: HTMLElement) {
    // 始终使用 light theme 初始化，由外部通过 setOption 控制颜色
    instance = echarts.init(el) as unknown as ECharts
  }

  function setOption(option: EChartsOption, notMerge = false) {
    instance?.setOption(option, { notMerge })
  }

  function resize() {
    instance?.resize()
  }

  function dispose() {
    instance?.dispose()
    instance = null
  }

  function getInstance() {
    return instance
  }

  return { initChart, setOption, resize, dispose, getInstance }
}
