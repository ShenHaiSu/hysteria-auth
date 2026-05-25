<template>
  <div class="p-6 space-y-6">
    <!-- 页面标题 -->
    <div class="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-3">
      <h1 class="text-2xl font-semibold text-[var(--text-primary)]">
        {{ t('nodes.title') }}
      </h1>
      <div class="flex items-center gap-2">
        <Button
          :label="t('common.actions.export')"
          icon="pi pi-download"
          severity="secondary"
          class="hidden sm:flex"
          @click="handleExport"
        />
        <Button
          icon="pi pi-download"
          severity="secondary"
          text
          rounded
          :title="t('common.actions.export')"
          class="sm:hidden"
          @click="handleExport"
        />
        <Button
          v-permission="['super_admin', 'admin']"
          :label="t('nodes.form.registerTitle')"
          icon="pi pi-plus"
          @click="openRegisterDialog"
        />
      </div>
    </div>

    <!-- 搜索 + 筛选栏 -->
    <div class="flex flex-col sm:flex-row items-start sm:items-center gap-3 flex-wrap">
      <IconField class="w-full sm:w-72">
        <InputIcon>
          <i class="pi pi-search" />
        </InputIcon>
        <InputText v-model="searchInput" :placeholder="t('common.actions.search')" class="w-full" />
      </IconField>

      <SelectButton
        v-model="activeFilter"
        :options="activeFilterOptions"
        option-label="label"
        option-value="value"
        class="shrink-0"
      />

      <SelectButton
        v-model="provisionFilter"
        :options="provisionFilterOptions"
        option-label="label"
        option-value="value"
        class="shrink-0"
      />
    </div>

    <!-- 节点列表 -->
    <div class="bg-[var(--bg-elevated)] rounded-md border border-[var(--border-light)]">
      <DataTable
        :value="nodesStore.items"
        :loading="nodesStore.isLoading"
        :lazy="true"
        :total-records="nodesStore.total"
        paginator
        :rows="nodesStore.pageSize"
        :first="(nodesStore.page - 1) * nodesStore.pageSize"
        :rows-per-page-options="PAGE_SIZE_OPTIONS"
        striped-rows
        sort-field="name"
        :sort-order="1"
        @page="onPageChange"
        @row-click="viewDetail($event.data)"
        class="p-datatable-sm cursor-pointer"
      >
        <!-- 节点名称 -->
        <Column
          field="name"
          :header="t('nodes.table.columns.name')"
          :sortable="true"
          style="min-width: 140px"
        >
          <template #body="{ data }: { data: NodeDto }">
            <span class="text-sm font-medium text-[var(--text-primary)]">{{ data.name }}</span>
          </template>
        </Column>

        <!-- IP 地址 -->
        <Column
          field="ipAddress"
          :header="t('nodes.table.columns.ipAddress')"
          style="min-width: 140px"
        >
          <template #body="{ data }: { data: NodeDto }">
            <span class="text-sm text-[var(--text-secondary)] font-mono">{{
              data.ipAddress ?? '-'
            }}</span>
          </template>
        </Column>

        <!-- 端口 -->
        <Column
          field="port"
          :header="t('nodes.table.columns.port')"
          style="width: 80px"
          class="text-center"
        >
          <template #body="{ data }: { data: NodeDto }">
            <span class="text-sm text-[var(--text-primary)]">{{ data.port }}</span>
          </template>
        </Column>

        <!-- 地区 -->
        <Column
          field="location"
          :header="t('nodes.table.columns.location')"
          style="min-width: 100px"
        >
          <template #body="{ data }: { data: NodeDto }">
            <span class="text-sm text-[var(--text-secondary)]">{{ data.location ?? '-' }}</span>
          </template>
        </Column>

        <!-- 在线状态 -->
        <Column header="在线状态" style="width: 100px" class="text-center">
          <template #body="{ data }: { data: NodeDto }">
            <AppStatusBadge :type="getOnlineStatusType(data)" />
          </template>
        </Column>

        <!-- 注册状态 -->
        <Column
          field="provisionStatus"
          :header="t('nodes.table.columns.provisionStatus')"
          style="width: 110px"
          class="text-center"
        >
          <template #body="{ data }: { data: NodeDto }">
            <AppStatusBadge
              :type="data.provisionStatus === 'pending' ? 'pending' : 'provisioned'"
            />
          </template>
        </Column>

        <!-- 最后心跳 -->
        <Column
          field="lastHeartbeat"
          :header="t('nodes.table.columns.lastHeartbeat')"
          style="min-width: 120px"
          class="text-center"
        >
          <template #body="{ data }: { data: NodeDto }">
            <span class="text-sm text-[var(--text-secondary)]">
              {{ formatHeartbeat(data.lastHeartbeat) }}
            </span>
          </template>
        </Column>

        <!-- 配置版本 (默认隐藏) -->
        <Column
          field="configVersion"
          :header="t('nodes.table.columns.configVersion')"
          style="width: 90px"
          class="text-center"
          :hidden="true"
        >
          <template #body="{ data }: { data: NodeDto }">
            <span class="text-sm text-[var(--text-primary)]">{{
              data.configVersion ?? '-'
            }}</span>
          </template>
        </Column>

        <!-- 域名 (默认隐藏) -->
        <Column
          field="domainName"
          :header="t('nodes.table.columns.domainName')"
          style="min-width: 120px"
          :hidden="true"
        >
          <template #body="{ data }: { data: NodeDto }">
            <span class="text-sm text-[var(--text-secondary)]">{{ data.domainName ?? '-' }}</span>
          </template>
        </Column>

        <!-- 创建时间 -->
        <Column
          field="createdAt"
          :header="t('nodes.table.columns.createdAt')"
          :sortable="true"
          style="min-width: 150px"
        >
          <template #body="{ data }: { data: NodeDto }">
            <span class="text-sm text-[var(--text-secondary)]">{{
              formatDate(data.createdAt)
            }}</span>
          </template>
        </Column>

        <!-- 操作 -->
        <Column
          :header="t('nodes.table.columns.actions')"
          style="min-width: 130px"
          class="text-center"
        >
          <template #body="{ data }: { data: NodeDto }">
            <div class="flex items-center justify-center gap-1">
              <Button
                icon="pi pi-eye"
                severity="secondary"
                text
                rounded
                :title="t('common.actions.view')"
                size="small"
                @click.stop="viewDetail(data)"
              />
              <Button
                v-permission="['super_admin', 'admin']"
                icon="pi pi-sync"
                severity="secondary"
                text
                rounded
                :title="t('nodes.detail.secretVersion')"
                size="small"
                @click.stop="handleRotateSecret(data)"
              />
            </div>
          </template>
        </Column>

        <!-- 空状态 -->
        <template #empty>
          <AppEmpty />
        </template>
      </DataTable>
    </div>

    <!-- 预注册对话框 -->
    <NodeRegisterDialog v-model:visible="showRegisterDialog" @saved="onRegisterSuccess" />
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, watch, computed } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useNodesStore, HEARTBEAT_TIMEOUT_MS } from '@/stores/nodes.store'
import { useToast } from '@/composables/useToast'
import { useConfirm } from '@/composables/useConfirm'
import { useDebounce } from '@/composables/useDebounce'
import { usePermission } from '@/composables/usePermission'
import { useExportExcel } from '@/composables/useExportExcel'
import NodeRegisterDialog from './NodeRegisterDialog.vue'
import AppStatusBadge from '@/components/common/AppStatusBadge.vue'
import type { NodeDto } from '@/types/node.types'
import type { ProvisionStatus } from '@/types/common.types'
import { DEFAULT_PAGE_SIZE, PAGE_SIZE_OPTIONS } from '@/utils/constants'

const router = useRouter()
const { t } = useI18n()
const nodesStore = useNodesStore()
const toast = useToast()
const confirm = useConfirm()
const { canEdit } = usePermission()

// 对话框控制
const showRegisterDialog = ref(false)

const { exportToExcel } = useExportExcel()

// 搜索
const searchInput = ref('')
const debouncedSearch = useDebounce(searchInput, 300)

// 筛选
const activeFilter = ref<'all' | 'active' | 'inactive'>('all')
const provisionFilter = ref<'all' | 'pending' | 'provisioned'>('all')

// 页面加载
onMounted(() => {
  nodesStore.fetchNodes()
})

// 搜索防抖
watch(debouncedSearch, (val) => {
  nodesStore.setSearch(val)
  nodesStore.fetchNodes()
})

// 状态筛选
watch([activeFilter, provisionFilter], () => {
  nodesStore.activeFilter = activeFilter.value
  nodesStore.provisionFilter = provisionFilter.value
  nodesStore.page = 1
  nodesStore.fetchNodes()
})

// 翻页
function onPageChange(event: { page: number; rows: number }) {
  nodesStore.page = event.page + 1
  nodesStore.pageSize = event.rows
  nodesStore.fetchNodes()
}

// 判断在线状态
function isOnline(node: NodeDto): boolean {
  if (!node.isActive || !node.lastHeartbeat) return false
  const now = Date.now()
  const hb = new Date(node.lastHeartbeat).getTime()
  return now - hb < HEARTBEAT_TIMEOUT_MS
}

// 查看详情
function viewDetail(node: NodeDto) {
  router.push({ name: 'NodeDetail', params: { id: node.id } })
}

// 打开预注册对话框
function openRegisterDialog() {
  showRegisterDialog.value = true
}

// 预注册成功
function onRegisterSuccess() {
  nodesStore.fetchNodes()
}

// 轮换密钥
async function handleRotateSecret(node: NodeDto) {
  const confirmed = await confirm.confirmDelete(
    t('nodes.toast.rotateSecretConfirm'),
    t('common.confirm.title'),
  )
  if (confirmed) {
    try {
      await nodesStore.rotateSecret(node.id)
      toast.success(t('nodes.toast.rotateSecretSuccess'))
    } catch {
      toast.error(t('common.toast.error'))
    }
  }
}

// 格式化心跳时间
function formatHeartbeat(dateStr: string | null): string {
  if (!dateStr) return '-'
  const elapsed = Date.now() - new Date(dateStr).getTime()
  if (elapsed < 60_000) return `${Math.floor(elapsed / 1000)}秒前`
  if (elapsed < 3_600_000) return `${Math.floor(elapsed / 60_000)}分钟前`
  if (elapsed < 86_400_000) return `${Math.floor(elapsed / 3_600_000)}小时前`
  return `${Math.floor(elapsed / 86_400_000)}天前`
}

// 格式化日期
function formatDate(dateStr: string): string {
  const d = new Date(dateStr)
  return d.toLocaleString('zh-CN', {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
  })
}

// 获取在线状态类型
function getOnlineStatusType(node: NodeDto): 'online' | 'offline' {
  return isOnline(node) ? 'online' : 'offline'
}

// 筛选选项
const activeFilterOptions = computed(() => [
  { label: t('common.status.all'), value: 'all' },
  { label: t('common.status.online'), value: 'active' },
  { label: t('common.status.offline'), value: 'inactive' },
])

const provisionFilterOptions = computed(() => [
  { label: t('common.status.all'), value: 'all' },
  { label: t('common.status.pending'), value: 'pending' },
  { label: t('common.status.provisioned'), value: 'provisioned' },
])

/** 导出当前节点列表 */
function handleExport() {
  exportToExcel(
    nodesStore.items.map((n) => ({
      name: n.name,
      ipAddress: n.ipAddress ?? '',
      port: n.port,
      location: n.location ?? '',
      isActive: isOnline(n) ? t('common.status.online') : t('common.status.offline'),
      provisionStatus: n.provisionStatus,
      lastHeartbeat: n.lastHeartbeat ?? '',
      createdAt: n.createdAt,
    })),
    [
      { header: t('nodes.table.columns.name'), key: 'name' },
      { header: t('nodes.table.columns.ipAddress'), key: 'ipAddress' },
      { header: t('nodes.table.columns.port'), key: 'port' },
      { header: t('nodes.table.columns.location'), key: 'location' },
      { header: t('nodes.table.columns.isActive'), key: 'isActive' },
      { header: t('nodes.table.columns.provisionStatus'), key: 'provisionStatus' },
      { header: t('nodes.table.columns.lastHeartbeat'), key: 'lastHeartbeat' },
      { header: t('nodes.table.columns.createdAt'), key: 'createdAt' },
    ],
    t('nodes.title'),
  )
}
</script>
