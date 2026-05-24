<template>
  <div class="p-4 lg:p-6 space-y-6">
    <!-- 页面标题栏 -->
    <div class="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-3">
      <h1 class="text-2xl font-semibold text-[var(--text-primary)]">
        {{ t('dashboard.title') }}
      </h1>
      <button
        class="w-full sm:w-auto inline-flex items-center gap-2 px-4 py-2 border border-[var(--border-default)] text-[var(--text-secondary)] hover:bg-[var(--bg-secondary)] rounded-md transition-colors duration-100 text-sm font-medium"
        :disabled="dashboardStore.isLoading"
        @click="loadData"
      >
        <i class="pi pi-refresh text-sm" :class="{ 'animate-spin': dashboardStore.isLoading }" />
        {{ t('common.actions.refresh') }}
      </button>
    </div>

    <!-- 错误状态 -->
    <AppError
      v-if="hasError && !dashboardStore.overview"
      :message="errorMessage"
      @retry="handleRetry"
    />

    <!-- 加载中骨架 -->
    <template v-else-if="dashboardStore.isLoading && !dashboardStore.overview">
      <div class="grid grid-cols-2 md:grid-cols-3 xl:grid-cols-4 gap-4">
        <div
          v-for="i in 7"
          :key="i"
          class="bg-[var(--bg-elevated)] rounded-md border border-[var(--border-light)] shadow-md p-5"
        >
          <div class="h-4 w-20 bg-[var(--bg-secondary)] rounded animate-pulse mb-2" />
          <div class="h-6 w-16 bg-[var(--bg-secondary)] rounded animate-pulse" />
        </div>
      </div>
    </template>

    <!-- 统计卡片 -->
    <template v-else>
      <div class="grid grid-cols-2 md:grid-cols-3 xl:grid-cols-4 gap-4">
        <StatCard
          v-for="card in statCards"
          :key="card.key"
          :icon="card.icon"
          :label="card.label"
          :value="card.value"
          :icon-bg-class="card.iconBgClass"
          :icon-color-class="card.iconColorClass"
          :loading="dashboardStore.isLoading"
        />
      </div>

      <!-- 流量趋势图表 -->
      <div
        class="bg-[var(--bg-elevated)] rounded-md border border-[var(--border-light)] shadow-md p-5"
      >
        <h3 class="text-lg font-semibold text-[var(--text-primary)] mb-4">
          {{ t('dashboard.charts.trafficTrend') }}
        </h3>
        <BaseChart
          v-if="trafficChartOption"
          :option="trafficChartOption"
          :height="chartHeight"
        />
        <div
          v-else
          class="flex flex-col items-center justify-center text-[var(--text-muted)]"
          :style="{ minHeight: chartHeight }"
        >
          <i class="pi pi-inbox text-3xl mb-2" />
          <p class="text-sm">{{ t('common.empty.title') }}</p>
        </div>
      </div>
    </template>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { useDashboardStore } from '@/stores/dashboard.store'
import { formatFileSize } from '@/utils/format'
import { getChartColors } from '@/composables/useECharts'
import { useThemeStore } from '@/stores/theme.store'
import BaseChart from '@/components/common/BaseChart.vue'
import StatCard from './components/StatCard.vue'
import AppLoading from '@/components/common/AppLoading.vue'
import AppError from '@/components/common/AppError.vue'
import type { EChartsOption } from 'echarts'
import type { TrafficTrendPoint } from '@/types/dashboard.types'

const { t } = useI18n()
const dashboardStore = useDashboardStore()
const themeStore = useThemeStore()

const hasError = ref(false)
const errorMessage = ref('')

/** 移动端图表高度更低 */
const chartHeight = computed(() => '280px')

const statCards = computed(() => [
  {
    key: 'totalUsers',
    icon: 'pi pi-users',
    label: t('dashboard.cards.totalUsers'),
    value: dashboardStore.overview?.totalUsers?.toLocaleString('zh-CN') ?? '--',
    iconBgClass: 'bg-brand-100',
    iconColorClass: 'text-brand-500',
  },
  {
    key: 'activeUsers',
    icon: 'pi pi-user-check',
    label: t('dashboard.cards.activeUsers'),
    value: dashboardStore.overview?.activeUsers?.toLocaleString('zh-CN') ?? '--',
    iconBgClass: 'bg-[var(--status-active-bg)]',
    iconColorClass: 'text-[var(--status-active)]',
  },
  {
    key: 'onlineUsers',
    icon: 'pi pi-circle-fill',
    label: t('dashboard.cards.onlineUsers'),
    value: dashboardStore.overview?.onlineUsersNow?.toLocaleString('zh-CN') ?? '--',
    iconBgClass: 'bg-[var(--status-active-bg)]',
    iconColorClass: 'text-[var(--status-active)]',
  },
  {
    key: 'totalNodes',
    icon: 'pi pi-server',
    label: t('dashboard.cards.totalNodes'),
    value: dashboardStore.overview?.totalNodes?.toLocaleString('zh-CN') ?? '--',
    iconBgClass: 'bg-[var(--status-info-bg)]',
    iconColorClass: 'text-[var(--status-info)]',
  },
  {
    key: 'activeNodes',
    icon: 'pi pi-verified',
    label: t('dashboard.cards.activeNodes'),
    value: dashboardStore.overview?.activeNodes?.toLocaleString('zh-CN') ?? '--',
    iconBgClass: 'bg-[var(--status-active-bg)]',
    iconColorClass: 'text-[var(--status-active)]',
  },
  {
    key: 'todayTraffic',
    icon: 'pi pi-arrow-down',
    label: t('dashboard.cards.todayTraffic'),
    value: dashboardStore.overview ? formatFileSize(dashboardStore.totalTrafficToday) : '--',
    iconBgClass: 'bg-brand-100',
    iconColorClass: 'text-brand-500',
  },
  {
    key: 'monthTraffic',
    icon: 'pi pi-calendar',
    label: t('dashboard.cards.monthTraffic'),
    value: dashboardStore.overview ? formatFileSize(dashboardStore.totalTrafficThisMonth) : '--',
    iconBgClass: 'bg-[var(--status-info-bg)]',
    iconColorClass: 'text-[var(--status-info)]',
  },
])

/** 流量趋势图表配置 */
const trafficChartOption = computed<EChartsOption | null>(() => {
  const data = dashboardStore.overview?.trafficTrend
  if (!data || data.length === 0) return null

  const colors = getChartColors(themeStore.mode)
  const dates = data.map((p: TrafficTrendPoint) => p.date)
  const bytesInData = data.map((p: TrafficTrendPoint) => (p.bytesIn / (1024 * 1024)).toFixed(2))
  const bytesOutData = data.map((p: TrafficTrendPoint) => (p.bytesOut / (1024 * 1024)).toFixed(2))

  return {
    tooltip: {
      trigger: 'axis' as const,
      valueFormatter: (value: unknown) => `${value} MB`,
    },
    legend: {
      data: [t('dashboard.charts.bytesIn'), t('dashboard.charts.bytesOut')],
      top: 0,
    },
    xAxis: {
      type: 'category' as const,
      data: dates,
      axisLabel: { rotate: 30 },
    },
    yAxis: {
      type: 'value' as const,
      name: 'MB',
      nameTextStyle: { color: colors.textSecondary, fontSize: 11 },
    },
    series: [
      {
        name: t('dashboard.charts.bytesIn'),
        type: 'line' as const,
        data: bytesInData,
        smooth: true,
        symbol: 'none',
        areaStyle: {
          color: {
            type: 'linear' as const,
            x: 0, y: 0, x2: 0, y2: 1,
            colorStops: [
              { offset: 0, color: colors.brand },
              { offset: 1, color: 'rgba(59, 130, 246, 0.05)' },
            ],
          },
        },
        lineStyle: { color: colors.brand, width: 2 },
        itemStyle: { color: colors.brand },
      },
      {
        name: t('dashboard.charts.bytesOut'),
        type: 'line' as const,
        data: bytesOutData,
        smooth: true,
        symbol: 'none',
        areaStyle: {
          color: {
            type: 'linear' as const,
            x: 0, y: 0, x2: 0, y2: 1,
            colorStops: [
              { offset: 0, color: colors.green },
              { offset: 1, color: 'rgba(34, 197, 94, 0.05)' },
            ],
          },
        },
        lineStyle: { color: colors.green, width: 2 },
        itemStyle: { color: colors.green },
      },
    ],
    grid: {
      left: '3%',
      right: '4%',
      bottom: '3%',
      top: '15%',
      containLabel: true,
    },
  }
})

async function loadData() {
  hasError.value = false
  errorMessage.value = ''
  try {
    await dashboardStore.fetchOverview(true)
  } catch (err) {
    hasError.value = true
    errorMessage.value = t('common.error.loadFailed')
    console.error('Dashboard load failed:', err)
  }
}

function handleRetry() {
  loadData()
}

onMounted(() => {
  loadData()
})
</script>
