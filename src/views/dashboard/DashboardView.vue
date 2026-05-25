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
      <div class="grid grid-cols-2 xl:grid-cols-4 gap-4">
        <div
          v-for="i in 4"
          :key="i"
          class="bg-[var(--bg-elevated)] rounded-md border border-[var(--border-light)] shadow-md p-5"
        >
          <div class="flex items-center gap-3">
            <div class="w-10 h-10 rounded-md bg-[var(--bg-secondary)] animate-pulse" />
            <div class="flex-1">
              <div class="h-4 w-20 bg-[var(--bg-secondary)] rounded animate-pulse mb-2" />
              <div class="h-6 w-32 bg-[var(--bg-secondary)] rounded animate-pulse" />
            </div>
          </div>
        </div>
      </div>
    </template>

    <!-- 信息卡片 -->
    <template v-else>
      <div class="grid grid-cols-2 xl:grid-cols-4 gap-4">
        <!-- 用户概览 -->
        <InfoCard
          :icon="'pi pi-users'"
          :title="t('dashboard.cards.userOverview')"
          :value="userValue"
          icon-bg-class="bg-brand-100"
          icon-color-class="text-brand-500"
          :loading="dashboardStore.isLoading"
        />

        <!-- 节点概览 -->
        <InfoCard
          :icon="'pi pi-server'"
          :title="t('dashboard.cards.nodeOverview')"
          :value="nodeValue"
          icon-bg-class="bg-[var(--status-info-bg)]"
          icon-color-class="text-[var(--status-info)]"
          :loading="dashboardStore.isLoading"
        />

        <!-- 今日流量 -->
        <InfoCard
          :icon="'pi pi-arrow-down'"
          :title="t('dashboard.cards.todayTrafficOverview')"
          :value="todayTrafficValue"
          icon-bg-class="bg-brand-100"
          icon-color-class="text-brand-500"
          :loading="dashboardStore.isLoading"
        />

        <!-- 本月流量 -->
        <InfoCard
          :icon="'pi pi-calendar'"
          :title="t('dashboard.cards.monthTrafficOverview')"
          :value="monthTrafficValue"
          icon-bg-class="bg-[var(--status-info-bg)]"
          icon-color-class="text-[var(--status-info)]"
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
        <BaseChart v-if="trafficChartOption" :option="trafficChartOption" :height="chartHeight" />
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
import InfoCard from './components/InfoCard.vue'
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

/** 用户概览卡片数据：总计 / 活跃 / 在线 */
const userValue = computed(() => {
  const overview = dashboardStore.overview
  const total = overview?.totalUsers?.toLocaleString('zh-CN') ?? '--'
  const active = overview?.activeUsers?.toLocaleString('zh-CN') ?? '--'
  const online = overview?.onlineUsersNow?.toLocaleString('zh-CN') ?? '--'
  return `总计 ${total} / 活跃 ${active} / 在线 ${online}`
})

/** 节点概览卡片数据：总计 / 活跃 */
const nodeValue = computed(() => {
  const overview = dashboardStore.overview
  const total = overview?.totalNodes?.toLocaleString('zh-CN') ?? '--'
  const active = overview?.activeNodes?.toLocaleString('zh-CN') ?? '--'
  return `总计 ${total} / 活跃 ${active}`
})

/** 今日流量卡片数据 */
const todayTrafficValue = computed(() => {
  const overview = dashboardStore.overview
  return overview ? formatFileSize(dashboardStore.totalTrafficToday) : '--'
})

/** 本月流量卡片数据 */
const monthTrafficValue = computed(() => {
  const overview = dashboardStore.overview
  return overview ? formatFileSize(dashboardStore.totalTrafficThisMonth) : '--'
})

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
            x: 0,
            y: 0,
            x2: 0,
            y2: 1,
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
            x: 0,
            y: 0,
            x2: 0,
            y2: 1,
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
