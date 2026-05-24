<script setup lang="ts">
import { ref, onMounted, watch, computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useUsersStore } from '@/stores/users.store'
import { useToast } from '@/composables/useToast'
import { useConfirm } from '@/composables/useConfirm'
import { usePermission } from '@/composables/usePermission'
import { useExportExcel } from '@/composables/useExportExcel'
import { getChartColors } from '@/composables/useECharts'
import { useThemeStore } from '@/stores/theme.store'
import AppStatusBadge from '@/components/common/AppStatusBadge.vue'
import AppTrafficText from '@/components/common/AppTrafficText.vue'
import BaseChart from '@/components/common/BaseChart.vue'
import UserFormDialog from './UserFormDialog.vue'
import type { Period } from '@/types/common.types'
import type { TrafficDataPoint } from '@/types/user.types'
import type { EChartsOption } from 'echarts'

const route = useRoute()
const router = useRouter()
const { t } = useI18n()
const usersStore = useUsersStore()
const toast = useToast()
const confirm = useConfirm()
const { canEdit } = usePermission()

const userId = computed(() => Number(route.params.id))
const user = computed(() => usersStore.currentUser)
const themeStore = useThemeStore()

const { exportToExcel } = useExportExcel()

const showEditDialog = ref(false)
const selectedPeriod = ref<Period>('month')

/** 移动端图表高度 */
const chartHeight = computed(() => '260px')

/** 流量趋势图表配置 */
const trafficChartOption = computed<EChartsOption | null>(() => {
  const stats = usersStore.trafficStats
  if (!stats || !stats.dataPoints || stats.dataPoints.length === 0) return null

  const colors = getChartColors(themeStore.mode)
  const dates = stats.dataPoints.map((p: TrafficDataPoint) => p.date)
  const inData = stats.dataPoints.map((p: TrafficDataPoint) => +(p.bytesIn / (1024 * 1024)).toFixed(2))
  const outData = stats.dataPoints.map((p: TrafficDataPoint) => +(p.bytesOut / (1024 * 1024)).toFixed(2))

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
        data: inData,
        smooth: true,
        symbol: 'none',
        areaStyle: {
          color: { type: 'linear' as const, x: 0, y: 0, x2: 0, y2: 1,
            colorStops: [{ offset: 0, color: colors.brand }, { offset: 1, color: 'rgba(59,130,246,0.05)' }] },
        },
        lineStyle: { color: colors.brand, width: 2 },
        itemStyle: { color: colors.brand },
      },
      {
        name: t('dashboard.charts.bytesOut'),
        type: 'line' as const,
        data: outData,
        smooth: true,
        symbol: 'none',
        areaStyle: {
          color: { type: 'linear' as const, x: 0, y: 0, x2: 0, y2: 1,
            colorStops: [{ offset: 0, color: colors.green }, { offset: 1, color: 'rgba(34,197,94,0.05)' }] },
        },
        lineStyle: { color: colors.green, width: 2 },
        itemStyle: { color: colors.green },
      },
    ],
    grid: { left: '3%', right: '4%', bottom: '3%', top: '15%', containLabel: true },
  }
})

onMounted(() => {
  loadUser()
})

watch(userId, () => {
  loadUser()
})

async function loadUser() {
  await usersStore.fetchUserDetail(userId.value)
  loadTrafficStats()
}

async function loadTrafficStats() {
  await usersStore.fetchTrafficStats(userId.value, selectedPeriod.value)
}

watch(selectedPeriod, () => {
  loadTrafficStats()
})

// 删除用户
async function handleDelete() {
  if (!user.value) return
  const confirmed = await confirm.confirmDelete(
    t('users.toast.deleteConfirm'),
    t('common.confirm.deleteTitle'),
  )
  if (confirmed) {
    try {
      await usersStore.deleteUser(user.value.id)
      toast.success(t('users.toast.deleteSuccess'))
      router.push({ name: 'Users' })
    } catch {
      toast.error(t('users.toast.deleteFailed'))
    }
  }
}

// 重置流量
async function handleResetTraffic() {
  if (!user.value) return
  const confirmed = await confirm.danger(
    t('users.toast.resetConfirm'),
    t('common.confirm.title'),
  )
  if (confirmed) {
    try {
      await usersStore.resetTraffic(user.value.id)
      toast.success(t('users.toast.resetTrafficSuccess'))
      loadUser()
    } catch {
      toast.error(t('common.toast.error'))
    }
  }
}

function goBack() {
  router.push({ name: 'Users' })
}

function formatDate(dateStr: string | null): string {
  if (!dateStr) return '-'
  const d = new Date(dateStr)
  return d.toLocaleString('zh-CN', {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
  })
}

/** 导出用户流量统计 */
function handleExportTraffic() {
  const stats = usersStore.trafficStats
  if (!stats || !stats.dataPoints.length) return

  exportToExcel(
    stats.dataPoints.map((p: TrafficDataPoint) => ({
      date: p.date,
      bytesIn: +(p.bytesIn / (1024 * 1024)).toFixed(2),
      bytesOut: +(p.bytesOut / (1024 * 1024)).toFixed(2),
    })),
    [
      { header: t('audit.table.columns.createdAt'), key: 'date' },
      { header: t('dashboard.charts.bytesIn') + ' (MB)', key: 'bytesIn' },
      { header: t('dashboard.charts.bytesOut') + ' (MB)', key: 'bytesOut' },
    ],
    `${t('users.detail.trafficStats')}_${user.value?.username ?? ''}`,
  )
}
</script>

<template>
  <div class="p-6 space-y-6">
    <!-- 返回 + 标题 + 操作 -->
    <div class="flex items-center justify-between">
      <div class="flex items-center gap-3">
        <Button
          icon="pi pi-arrow-left"
          severity="secondary"
          text
          rounded
          :title="t('common.actions.back')"
          @click="goBack"
        />
        <h1 class="text-2xl font-semibold text-[var(--text-primary)]">
          {{ user?.username ?? t('users.detail.basicInfo') }}
        </h1>
      </div>

      <div v-if="canEdit" class="flex items-center gap-2">
        <Button
          :label="t('common.actions.edit')"
          icon="pi pi-pencil"
          severity="secondary"
          @click="showEditDialog = true"
        />
        <Button
          :label="t('users.traffic.title')"
          icon="pi pi-refresh"
          severity="secondary"
          @click="handleResetTraffic"
        />
        <Button
          :label="t('common.actions.delete')"
          icon="pi pi-trash"
          severity="danger"
          @click="handleDelete"
        />
      </div>
    </div>

    <!-- 加载状态 -->
    <AppLoading v-if="usersStore.isLoading && !user" />

    <!-- 用户信息 -->
    <div v-else-if="user" class="grid grid-cols-1 lg:grid-cols-3 gap-6">
      <!-- 基本信息卡片 -->
      <div class="lg:col-span-1 bg-[var(--bg-elevated)] rounded-md border border-[var(--border-light)] shadow-sm p-5 space-y-4">
        <h3 class="text-lg font-semibold text-[var(--text-primary)]">
          {{ t('users.detail.basicInfo') }}
        </h3>

        <div class="space-y-3">
          <div>
            <span class="text-xs text-[var(--text-muted)] uppercase tracking-wide">ID</span>
            <p class="text-sm text-[var(--text-primary)] font-medium">#{{ user.id }}</p>
          </div>

          <div>
            <span class="text-xs text-[var(--text-muted)] uppercase tracking-wide">{{ t('users.table.columns.username') }}</span>
            <p class="text-sm text-[var(--text-primary)] font-medium">{{ user.username }}</p>
          </div>

          <div>
            <span class="text-xs text-[var(--text-muted)] uppercase tracking-wide">{{ t('users.table.columns.email') }}</span>
            <p class="text-sm text-[var(--text-primary)]">{{ user.email ?? '-' }}</p>
          </div>

          <div>
            <span class="text-xs text-[var(--text-muted)] uppercase tracking-wide">{{ t('users.table.columns.isActive') }}</span>
            <div class="mt-1">
              <AppStatusBadge :type="user.isActive ? 'active' : 'inactive'" />
            </div>
          </div>

          <div>
            <span class="text-xs text-[var(--text-muted)] uppercase tracking-wide">{{ t('users.table.columns.createdAt') }}</span>
            <p class="text-sm text-[var(--text-primary)]">{{ formatDate(user.createdAt) }}</p>
          </div>

          <div>
            <span class="text-xs text-[var(--text-muted)] uppercase tracking-wide">{{ t('users.table.columns.expiresAt') }}</span>
            <p class="text-sm text-[var(--text-primary)]">{{ user.expiresAt ? formatDate(user.expiresAt) : '-' }}</p>
          </div>

          <div>
            <span class="text-xs text-[var(--text-muted)] uppercase tracking-wide">{{ t('users.table.columns.remark') }}</span>
            <p class="text-sm text-[var(--text-primary)]">{{ user.remark ?? '-' }}</p>
          </div>

          <div>
            <span class="text-xs text-[var(--text-muted)] uppercase tracking-wide">{{ t('users.table.columns.allowedNodes') }}</span>
            <div class="flex flex-wrap gap-1 mt-1">
              <span
                v-if="user.allowedNodes && user.allowedNodes.length > 0"
                v-for="node in user.allowedNodes"
                :key="node"
                class="inline-flex items-center px-2 py-0.5 rounded-sm text-xs font-medium bg-brand-100 dark:bg-brand-900 text-brand-700 dark:text-brand-300"
              >
                {{ node }}
              </span>
              <span v-else class="text-sm text-[var(--text-muted)]">-</span>
            </div>
          </div>
        </div>
      </div>

      <!-- 流量统计卡片 -->
      <div class="lg:col-span-2 bg-[var(--bg-elevated)] rounded-md border border-[var(--border-light)] shadow-sm p-5 space-y-4">
        <div class="flex items-center justify-between">
          <h3 class="text-lg font-semibold text-[var(--text-primary)]">
            {{ t('users.detail.trafficStats') }}
          </h3>
          <div class="flex items-center gap-2">
            <Button
              :label="t('common.actions.export')"
              icon="pi pi-download"
              severity="secondary"
              size="small"
              class="hidden sm:flex"
              @click="handleExportTraffic"
            />
            <Button
              icon="pi pi-download"
              severity="secondary"
              text
              rounded
              size="small"
              :title="t('common.actions.export')"
              class="sm:hidden"
              @click="handleExportTraffic"
            />
            <!-- 周期选择 -->
            <SelectButton
              v-model="selectedPeriod"
              :options="[
                { label: t('users.traffic.period.day'), value: 'day' },
                { label: t('users.traffic.period.week'), value: 'week' },
                { label: t('users.traffic.period.month'), value: 'month' },
                { label: t('users.traffic.period.all'), value: 'all' },
              ]"
              option-label="label"
              option-value="value"
              size="small"
            />
          </div>

        <!-- 流量概览 -->
        <div class="grid grid-cols-1 sm:grid-cols-3 gap-4">
          <div class="bg-[var(--bg-secondary)] rounded-md p-4">
            <p class="text-xs text-[var(--text-muted)] mb-1">{{ t('users.traffic.totalUsed') }}</p>
            <p class="text-2xl font-bold text-[var(--text-primary)]">
              <AppTrafficText :bytes="usersStore.trafficStats?.totalBytesIn ?? 0" />
            </p>
          </div>
          <div class="bg-[var(--bg-secondary)] rounded-md p-4">
            <p class="text-xs text-[var(--text-muted)] mb-1">{{ t('users.traffic.totalQuota') }}</p>
            <p class="text-2xl font-bold text-[var(--text-primary)]">
              <template v-if="user.totalTrafficBytes === 0">
                {{ t('common.status.unlimited', '不限') }}
              </template>
              <AppTrafficText v-else :bytes="user.totalTrafficBytes" :precise="true" />
            </p>
          </div>
          <div class="bg-[var(--bg-secondary)] rounded-md p-4">
            <p class="text-xs text-[var(--text-muted)] mb-1">{{ t('users.traffic.usageRate') }}</p>
            <p class="text-2xl font-bold" :class="user.totalTrafficBytes > 0 && (user.usedTrafficBytes / user.totalTrafficBytes) > 0.9 ? 'text-[var(--status-error)]' : 'text-[var(--text-primary)]'">
              {{ user.totalTrafficBytes > 0
                ? ((user.usedTrafficBytes / user.totalTrafficBytes) * 100).toFixed(1) + '%'
                : '-' }}
            </p>
          </div>
        </div>

        <!-- 流量趋势图表 -->
        <BaseChart
          v-if="trafficChartOption"
          :option="trafficChartOption"
          :height="chartHeight"
        />
        <div
          v-else-if="usersStore.trafficStats && !usersStore.trafficStats.dataPoints.length"
          class="flex items-center justify-center text-sm text-[var(--text-muted)] bg-[var(--bg-secondary)] rounded-md"
          :style="{ minHeight: chartHeight }"
        >
          <div class="text-center">
            <i class="pi pi-inbox text-3xl mb-2 block text-[var(--text-muted)]" />
            <p>{{ t('common.empty.title') }}</p>
          </div>
        </div>
      </div>
    </div>

    <!-- 错误状态 -->
    <AppError v-else-if="!usersStore.isLoading && !user" @retry="loadUser" />

    <!-- 编辑对话框 -->
    <UserFormDialog
      v-if="user"
      v-model:visible="showEditDialog"
      :user="user"
      @saved="loadUser"
    />
  </div>
</template>
