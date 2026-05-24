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

        <!-- 图表占位 (Phase 5 补充) -->
        <div
          class="bg-[var(--bg-secondary)] rounded-md p-6 flex items-center justify-center text-sm text-[var(--text-muted)]"
          style="min-height: 280px"
        >
          <div class="text-center">
            <i class="pi pi-chart-bar text-3xl mb-2 block text-[var(--text-muted)]" />
            <p>
              {{ t('nodes.status.cpu') }} / {{ t('nodes.status.memory') }} /
              {{ t('nodes.table.columns.name') }}
            </p>
            <p class="text-xs mt-1">(Phase 5 补充图表)</p>
          </div>
        </div>

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
import AppStatusBadge from '@/components/common/AppStatusBadge.vue'
import type { NodeDto, NodeStatusRecord } from '@/types/node.types'

const route = useRoute()
const router = useRouter()
const { t } = useI18n()
const nodesStore = useNodesStore()
const toast = useToast()
const confirm = useConfirm()
const { canEdit } = usePermission()

const nodeId = computed(() => route.params.id as string)
const node = computed(() => nodesStore.currentNode)

// 状态历史时间范围选择 (小时)
const historyHours = ref<string>('24')

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
  const endDate = new Date().toISOString()
  const startDate = new Date(Date.now() - hours * 3_600_000).toISOString()
  await nodesStore.fetchStatusHistory(nodeId.value, {
    page: 1,
    pageSize: 200,
    startDate,
    endDate,
  })
}

watch(historyHours, () => {
  loadStatusHistory()
})

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

// 最新状态记录
const latestStatus = computed<NodeStatusRecord | null>(() => {
  if (nodesStore.statusHistory.length === 0) return null
  return nodesStore.statusHistory[nodesStore.statusHistory.length - 1] ?? null
})
</script>
