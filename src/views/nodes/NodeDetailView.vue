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
          {{ node?.name ?? t('nodes.detail.basicInfo') }}
        </h1>
        <AppStatusBadge v-if="node" :type="isOnline(node) ? 'online' : 'offline'" />
      </div>

      <div v-if="canEdit" class="flex items-center gap-2">
        <Button
          :label="t('nodes.config.editButton')"
          icon="pi pi-cog"
          severity="secondary"
          size="small"
          class="hidden sm:flex"
          @click="showConfigForm = true"
        />
        <Button
          icon="pi pi-cog"
          severity="secondary"
          text
          rounded
          size="small"
          :title="t('nodes.config.editButton')"
          class="sm:hidden"
          @click="showConfigForm = true"
        />
        <Button
          :label="t('nodes.toast.rotateSecretConfirm')"
          icon="pi pi-sync"
          severity="secondary"
          size="small"
          class="hidden sm:flex"
          @click="handleRotateSecret"
        />
        <Button
          icon="pi pi-sync"
          severity="secondary"
          text
          rounded
          :title="t('nodes.detail.secretVersion')"
          class="sm:hidden"
          @click="handleRotateSecret"
        />
      </div>
    </div>

    <!-- 加载状态 -->
    <AppLoading v-if="nodesStore.isLoading && !node" />

    <!-- 节点信息 -->
    <div v-else-if="node" class="grid grid-cols-1 lg:grid-cols-3 gap-6">
      <!-- 基本信息卡片 -->
      <div
        class="lg:col-span-1 bg-[var(--bg-elevated)] rounded-md border border-[var(--border-light)] shadow-sm p-5 space-y-4"
      >
        <h3 class="text-lg font-semibold text-[var(--text-primary)]">
          {{ t('nodes.detail.basicInfo') }}
        </h3>

        <div class="space-y-3">
          <div>
            <span class="text-xs text-[var(--text-muted)] uppercase tracking-wide">ID</span>
            <p class="text-sm text-[var(--text-primary)] font-mono">{{ node.id }}</p>
          </div>

          <div>
            <span class="text-xs text-[var(--text-muted)] uppercase tracking-wide">{{
              t('nodes.table.columns.name')
            }}</span>
            <p class="text-sm text-[var(--text-primary)] font-medium">{{ node.name }}</p>
          </div>

          <div>
            <span class="text-xs text-[var(--text-muted)] uppercase tracking-wide">{{
              t('nodes.table.columns.ipAddress')
            }}</span>
            <p class="text-sm text-[var(--text-primary)] font-mono">{{ node.ipAddress ?? '-' }}</p>
          </div>

          <div>
            <span class="text-xs text-[var(--text-muted)] uppercase tracking-wide">{{
              t('nodes.table.columns.port')
            }}</span>
            <p class="text-sm text-[var(--text-primary)]">{{ node.port }}</p>
          </div>

          <div>
            <span class="text-xs text-[var(--text-muted)] uppercase tracking-wide">{{
              t('nodes.table.columns.location')
            }}</span>
            <p class="text-sm text-[var(--text-primary)]">{{ node.location ?? '-' }}</p>
          </div>

          <div>
            <span class="text-xs text-[var(--text-muted)] uppercase tracking-wide">{{
              t('nodes.config.fields.domainName.label')
            }}</span>
            <p class="text-sm text-[var(--text-primary)]">{{ node.domainName ?? '-' }}</p>
          </div>

          <div>
            <span class="text-xs text-[var(--text-muted)] uppercase tracking-wide">{{
              t('nodes.config.fields.remark.label')
            }}</span>
            <p class="text-sm text-[var(--text-primary)]">{{ node.remark ?? '-' }}</p>
          </div>

          <div>
            <span class="text-xs text-[var(--text-muted)] uppercase tracking-wide">{{
              t('nodes.table.columns.provisionStatus')
            }}</span>
            <div class="mt-1">
              <AppStatusBadge
                :type="node.provisionStatus === 'pending' ? 'pending' : 'provisioned'"
              />
            </div>
          </div>

          <div>
            <span class="text-xs text-[var(--text-muted)] uppercase tracking-wide">{{
              t('nodes.table.columns.lastHeartbeat')
            }}</span>
            <p class="text-sm text-[var(--text-primary)]">
              {{ formatHeartbeat(node.lastHeartbeat) }}
            </p>
          </div>

          <div>
            <span class="text-xs text-[var(--text-muted)] uppercase tracking-wide">{{
              t('nodes.table.columns.createdAt')
            }}</span>
            <p class="text-sm text-[var(--text-primary)]">{{ formatDate(node.createdAt) }}</p>
          </div>
        </div>

        <!-- 密钥信息 (仅 admin 可见) -->
        <div v-if="canEdit" class="mt-4 pt-4 border-t border-[var(--border-light)]">
          <h4 class="text-sm font-semibold text-[var(--text-primary)] mb-3">
            {{ t('nodes.detail.secretInfo') }}
          </h4>
          <div class="space-y-2">
            <div>
              <span class="text-xs text-[var(--text-muted)]">{{
                t('nodes.detail.secretVersion')
              }}</span>
              <p class="text-sm text-[var(--text-primary)] font-mono">{{ node.secretVersion }}</p>
            </div>
            <div v-if="node.trafficStatsSecret">
              <span class="text-xs text-[var(--text-muted)]"
                >{{ t('nodes.form.fields.trafficStatsPort.label') }} Secret</span
              >
              <p class="text-sm text-[var(--text-primary)] font-mono font-medium break-all">
                {{ node.trafficStatsSecret }}
              </p>
            </div>
          </div>
        </div>

        <!-- 配置版本信息 (仅 admin 可见) -->
        <div v-if="canEdit" class="mt-4 pt-4 border-t border-[var(--border-light)]">
          <h4 class="text-sm font-semibold text-[var(--text-primary)] mb-3">
            {{ t('nodes.config.title') }}
          </h4>
          <div class="space-y-2">
            <div>
              <span class="text-xs text-[var(--text-muted)]">{{
                t('nodes.config.fields.configVersion.label')
              }}</span>
              <p class="text-sm text-[var(--text-primary)] font-mono">
                {{ node.configVersion ?? '-' }}
              </p>
            </div>
            <div>
              <span class="text-xs text-[var(--text-muted)]">{{
                t('nodes.config.fields.configUpdatedAt.label')
              }}</span>
              <p class="text-sm text-[var(--text-primary)]">
                {{ node.configUpdatedAt ? formatDate(node.configUpdatedAt) : '-' }}
              </p>
            </div>
          </div>
        </div>
      </div>

      <!-- 状态历史卡片 -->
      <div
        class="lg:col-span-2 bg-[var(--bg-elevated)] rounded-md border border-[var(--border-light)] shadow-sm p-5 space-y-4"
      >
        <div class="flex items-center justify-between">
          <h3 class="text-lg font-semibold text-[var(--text-primary)]">
            {{ t('nodes.detail.statusHistory') }}
          </h3>
          <!-- 时间范围选择 -->
          <SelectButton
            v-model="historyHours"
            :options="historyPeriodOptions"
            option-label="label"
            option-value="value"
            size="small"
          />
        </div>

        <!-- 最新状态概览 -->
        <div class="grid grid-cols-2 sm:grid-cols-4 gap-3">
          <div class="bg-[var(--bg-secondary)] rounded-md p-3">
            <p class="text-xs text-[var(--text-muted)] mb-1">{{ t('nodes.status.cpu') }}</p>
            <p class="text-lg font-bold text-[var(--text-primary)]">
              {{ latestStatus ? formatPercent(latestStatus.cpuUsagePercent) : '-' }}
            </p>
          </div>
          <div class="bg-[var(--bg-secondary)] rounded-md p-3">
            <p class="text-xs text-[var(--text-muted)] mb-1">{{ t('nodes.status.memory') }}</p>
            <p class="text-lg font-bold text-[var(--text-primary)]">
              {{ latestStatus ? formatPercent(latestStatus.memoryUsagePercent) : '-' }}
            </p>
          </div>
          <div class="bg-[var(--bg-secondary)] rounded-md p-3">
            <p class="text-xs text-[var(--text-muted)] mb-1">{{ t('nodes.status.networkIn') }}</p>
            <p class="text-lg font-bold text-[var(--text-primary)]">
              {{ latestStatus ? formatMbps(latestStatus.networkInMbps) : '-' }}
            </p>
          </div>
          <div class="bg-[var(--bg-secondary)] rounded-md p-3">
            <p class="text-xs text-[var(--text-muted)] mb-1">
              {{ t('nodes.status.activeConnections') }}
            </p>
            <p class="text-lg font-bold text-[var(--text-primary)]">
              {{ latestStatus ? latestStatus.activeConnections.toLocaleString() : '-' }}
            </p>
          </div>
        </div>

        <!-- CPU 使用率图表 -->
        <BaseChart
          v-if="cpuChartOption"
          :option="cpuChartOption"
          :height="chartHeight"
        />
        <div
          v-else-if="!nodesStore.statusHistoryLoading"
          class="flex items-center justify-center text-sm text-[var(--text-muted)] bg-[var(--bg-secondary)] rounded-md"
          :style="{ minHeight: chartHeight }"
        >
          <div class="text-center">
            <i class="pi pi-chart-bar text-3xl mb-2 block text-[var(--text-muted)]" />
            <p>{{ t('nodes.detail.noStatusHistory') }}</p>
          </div>
        </div>

        <!-- 内存使用率图表 -->
        <BaseChart
          v-if="memoryChartOption"
          :option="memoryChartOption"
          :height="chartHeight"
        />

        <!-- 网络速率图表 -->
        <BaseChart
          v-if="networkChartOption"
          :option="networkChartOption"
          :height="chartHeight"
        />

        <!-- 活跃连接数图表 -->
        <BaseChart
          v-if="connectionsChartOption"
          :option="connectionsChartOption"
          :height="chartHeight"
        />

        <!-- 状态历史表格 -->
        <div v-if="nodesStore.statusHistory.length > 0">
          <h4 class="text-sm font-semibold text-[var(--text-primary)] mb-2">
            {{ t('nodes.detail.statusHistory') }}
          </h4>
          <div class="overflow-x-auto">
            <table class="w-full text-sm">
              <thead>
                <tr class="border-b border-[var(--border-light)]">
                  <th class="text-left py-2 px-3 text-xs text-[var(--text-muted)] font-medium">
                    {{ t('nodes.status.cpu') }}
                  </th>
                  <th class="text-left py-2 px-3 text-xs text-[var(--text-muted)] font-medium">
                    {{ t('nodes.status.memory') }}
                  </th>
                  <th class="text-left py-2 px-3 text-xs text-[var(--text-muted)] font-medium">
                    {{ t('nodes.status.networkIn') }}
                  </th>
                  <th class="text-left py-2 px-3 text-xs text-[var(--text-muted)] font-medium">
                    {{ t('nodes.status.networkOut') }}
                  </th>
                  <th class="text-left py-2 px-3 text-xs text-[var(--text-muted)] font-medium">
                    {{ t('nodes.status.activeConnections') }}
                  </th>
                  <th class="text-right py-2 px-3 text-xs text-[var(--text-muted)] font-medium">
                    时间
                  </th>
                </tr>
              </thead>
              <tbody>
                <tr
                  v-for="(record, idx) in [...nodesStore.statusHistory].reverse().slice(0, 20)"
                  :key="record.id ?? idx"
                  class="border-b border-[var(--border-light)] hover:bg-[var(--bg-secondary)] transition-colors duration-100"
                >
                  <td class="py-2 px-3 text-[var(--text-primary)]">
                    {{ formatPercent(record.cpuUsagePercent) }}
                  </td>
                  <td class="py-2 px-3 text-[var(--text-primary)]">
                    {{ formatPercent(record.memoryUsagePercent) }}
                  </td>
                  <td class="py-2 px-3 text-[var(--text-primary)]">
                    {{ formatMbps(record.networkInMbps) }}
                  </td>
                  <td class="py-2 px-3 text-[var(--text-primary)]">
                    {{ formatMbps(record.networkOutMbps) }}
                  </td>
                  <td class="py-2 px-3 text-[var(--text-primary)]">
                    {{ record.activeConnections }}
                  </td>
                  <td class="py-2 px-3 text-[var(--text-secondary)] text-right whitespace-nowrap">
                    {{ formatDate(record.reportedAt) }}
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>

        <!-- 无状态历史 -->
        <div
          v-else-if="!nodesStore.statusHistoryLoading"
          class="flex flex-col items-center justify-center py-8 text-[var(--text-muted)]"
        >
          <i class="pi pi-info-circle text-2xl mb-2" />
          <p class="text-sm">{{ t('nodes.detail.noStatusHistory') }}</p>
        </div>
      </div>
    </div>

    <!-- 错误状态 -->
    <AppError v-else-if="!nodesStore.isLoading && !node" @retry="loadNode" />

    <!-- 配置编辑对话框 -->
    <NodeConfigForm
      v-if="node"
      v-model:visible="showConfigForm"
      :node-id="node.id"
      :current-config="node"
      @config-updated="onConfigUpdated"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, watch, computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useNodesStore, HEARTBEAT_TIMEOUT_MS } from '@/stores/nodes.store'
import { useToast } from '@/composables/useToast'
import { useConfirm } from '@/composables/useConfirm'
import { usePermission } from '@/composables/usePermission'
import { getChartColors } from '@/composables/useECharts'
import { useThemeStore } from '@/stores/theme.store'
import AppStatusBadge from '@/components/common/AppStatusBadge.vue'
import BaseChart from '@/components/common/BaseChart.vue'
import NodeConfigForm from './components/NodeConfigForm.vue'
import type { NodeDto, NodeStatusRecord } from '@/types/node.types'
import type { EChartsOption } from 'echarts'

const route = useRoute()
const router = useRouter()
const { t } = useI18n()
const nodesStore = useNodesStore()
const toast = useToast()
const confirm = useConfirm()
const { canEdit } = usePermission()

const nodeId = computed(() => route.params.id as string)
const node = computed(() => nodesStore.currentNode)
const themeStore = useThemeStore()

// 配置编辑对话框可见性
const showConfigForm = ref(false)

// 状态历史时间范围选择 (小时)
const historyHours = ref<string>('24')

/** 移动端图表高度 */
const chartHeight = computed(() => '260px')

/** 最新状态记录 */
const latestStatus = computed<NodeStatusRecord | null>(() => {
  if (nodesStore.statusHistory.length === 0) return null
  return nodesStore.statusHistory[nodesStore.statusHistory.length - 1] ?? null
})

/** 状态历史记录中时间轴标签 */
const statusTimeLabels = computed(() =>
  nodesStore.statusHistory.map((r) => {
    const d = new Date(r.reportedAt)
    return `${d.getHours().toString().padStart(2, '0')}:${d.getMinutes().toString().padStart(2, '0')}`
  }),
)

/** CPU 使用率图表 */
const cpuChartOption = computed<EChartsOption | null>(() => {
  if (nodesStore.statusHistory.length === 0) return null
  const colors = getChartColors(themeStore.mode)
  return {
    legend: { data: [t('nodes.status.cpu')], top: 0 },
    xAxis: { type: 'category' as const, data: statusTimeLabels.value },
    yAxis: { type: 'value' as const, name: '%', max: 100 },
    series: [{
      name: t('nodes.status.cpu'),
      type: 'line' as const,
      data: nodesStore.statusHistory.map((r) => r.cpuUsagePercent),
      smooth: true,
      symbol: 'none',
      lineStyle: { color: colors.brand, width: 2 },
      itemStyle: { color: colors.brand },
      areaStyle: {
        color: { type: 'linear' as const, x: 0, y: 0, x2: 0, y2: 1,
          colorStops: [{ offset: 0, color: colors.brand }, { offset: 1, color: 'rgba(59,130,246,0.05)' }] },
      },
    }],
    grid: { left: '3%', right: '4%', bottom: '3%', top: '15%', containLabel: true },
  }
})

/** 内存使用率图表 */
const memoryChartOption = computed<EChartsOption | null>(() => {
  if (nodesStore.statusHistory.length === 0) return null
  const colors = getChartColors(themeStore.mode)
  return {
    legend: { data: [t('nodes.status.memory')], top: 0 },
    xAxis: { type: 'category' as const, data: statusTimeLabels.value },
    yAxis: { type: 'value' as const, name: '%', max: 100 },
    series: [{
      name: t('nodes.status.memory'),
      type: 'line' as const,
      data: nodesStore.statusHistory.map((r) => r.memoryUsagePercent),
      smooth: true,
      symbol: 'none',
      lineStyle: { color: colors.amber, width: 2 },
      itemStyle: { color: colors.amber },
      areaStyle: {
        color: { type: 'linear' as const, x: 0, y: 0, x2: 0, y2: 1,
          colorStops: [{ offset: 0, color: colors.amber }, { offset: 1, color: 'rgba(245,158,11,0.05)' }] },
      },
    }],
    grid: { left: '3%', right: '4%', bottom: '3%', top: '15%', containLabel: true },
  }
})

/** 网络速率图表（双 Y 轴） */
const networkChartOption = computed<EChartsOption | null>(() => {
  if (nodesStore.statusHistory.length === 0) return null
  const colors = getChartColors(themeStore.mode)
  return {
    legend: { data: [t('nodes.status.networkIn'), t('nodes.status.networkOut')], top: 0 },
    xAxis: { type: 'category' as const, data: statusTimeLabels.value },
    yAxis: [
      { type: 'value' as const, name: 'Mbps' },
      { type: 'value' as const, name: 'Mbps' },
    ],
    series: [
      {
        name: t('nodes.status.networkIn'),
        type: 'line' as const,
        yAxisIndex: 0,
        data: nodesStore.statusHistory.map((r) => +r.networkInMbps.toFixed(2)),
        smooth: true,
        symbol: 'none',
        lineStyle: { color: colors.green, width: 2 },
        itemStyle: { color: colors.green },
      },
      {
        name: t('nodes.status.networkOut'),
        type: 'line' as const,
        yAxisIndex: 1,
        data: nodesStore.statusHistory.map((r) => +r.networkOutMbps.toFixed(2)),
        smooth: true,
        symbol: 'none',
        lineStyle: { color: colors.brand, width: 2 },
        itemStyle: { color: colors.brand },
      },
    ],
    grid: { left: '3%', right: '4%', bottom: '3%', top: '15%', containLabel: true },
  }
})

/** 活跃连接数图表 */
const connectionsChartOption = computed<EChartsOption | null>(() => {
  if (nodesStore.statusHistory.length === 0) return null
  const colors = getChartColors(themeStore.mode)
  return {
    legend: { data: [t('nodes.status.activeConnections')], top: 0 },
    xAxis: { type: 'category' as const, data: statusTimeLabels.value },
    yAxis: { type: 'value' as const, name: t('nodes.status.activeConnections'), minInterval: 1 },
    series: [{
      name: t('nodes.status.activeConnections'),
      type: 'line' as const,
      data: nodesStore.statusHistory.map((r) => r.activeConnections),
      smooth: true,
      symbol: 'none',
      lineStyle: { color: colors.purple, width: 2 },
      itemStyle: { color: colors.purple },
      areaStyle: {
        color: { type: 'linear' as const, x: 0, y: 0, x2: 0, y2: 1,
          colorStops: [{ offset: 0, color: colors.purple }, { offset: 1, color: 'rgba(139,92,246,0.05)' }] },
      },
    }],
    grid: { left: '3%', right: '4%', bottom: '3%', top: '15%', containLabel: true },
  }
})

// 加载
onMounted(() => {
  loadNode()
})

watch(nodeId, () => {
  loadNode()
})

async function loadNode() {
  await nodesStore.fetchNodeDetail(nodeId.value)
  await loadStatusHistory()
}

async function loadStatusHistory() {
  // 根据选择的小时数计算起始时间
  const hours = Number(historyHours.value)
  await nodesStore.fetchStatusHistory(nodeId.value, {
    page: 1,
    pageSize: 200,
    startDate: new Date(Date.now() - hours * 3_600_000).toISOString(),
    endDate: new Date().toISOString(),
  })
}

watch(historyHours, () => {
  loadStatusHistory()
})

// 配置更新成功回调
function onConfigUpdated() {
  showConfigForm.value = false
  toast.success(t('nodes.toast.updateConfigSuccess'))
}

// 判断在线状态
function isOnline(n: NodeDto | null): boolean {
  if (!n || !n.isActive || !n.lastHeartbeat) return false
  return Date.now() - new Date(n.lastHeartbeat).getTime() < HEARTBEAT_TIMEOUT_MS
}

// 轮换密钥
async function handleRotateSecret() {
  const confirmed = await confirm.confirmDelete(
    t('nodes.toast.rotateSecretConfirm'),
    t('common.confirm.title'),
  )
  if (confirmed) {
    try {
      await nodesStore.rotateSecret(nodeId.value)
      toast.success(t('nodes.toast.rotateSecretSuccess'))
    } catch {
      toast.error(t('common.toast.error'))
    }
  }
}

function goBack() {
  router.push({ name: 'Nodes' })
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

function formatHeartbeat(dateStr: string | null): string {
  if (!dateStr) return '-'
  const elapsed = Date.now() - new Date(dateStr).getTime()
  if (elapsed < 60_000) return `${Math.floor(elapsed / 1000)}秒前`
  if (elapsed < 3_600_000) return `${Math.floor(elapsed / 60_000)}分钟前`
  if (elapsed < 86_400_000) return `${Math.floor(elapsed / 3_600_000)}小时前`
  return `${Math.floor(elapsed / 86_400_000)}天前`
}

function formatPercent(val: number): string {
  return val.toFixed(1) + '%'
}

function formatMbps(val: number): string {
  return val.toFixed(2) + ' Mbps'
}

function formatMb(val: number): string {
  return val.toFixed(0) + ' MB'
}

// 状态历史时间范围选项
const historyPeriodOptions = computed(() => [
  { label: t('nodes.status.period.1h'), value: '1' },
  { label: t('nodes.status.period.6h'), value: '6' },
  { label: t('nodes.status.period.24h'), value: '24' },
  { label: t('nodes.status.period.7d'), value: '168' },
])

</script>
