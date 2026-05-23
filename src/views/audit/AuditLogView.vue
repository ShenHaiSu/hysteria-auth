<script setup lang="ts">
import { ref, watch, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { auditLogApi } from '@/api/modules/audit-logs'
import { useDebounce } from '@/composables/useDebounce'
import { formatDate } from '@/utils/format'
import type { AuditLogEntry, AuditLogFilters } from '@/types/audit.types'
import type { AuditAction, AuditTargetType } from '@/types/common.types'

// PrimeVue
import DataTable from 'primevue/datatable'
import Column from 'primevue/column'
import InputText from 'primevue/inputtext'
import Select from 'primevue/select'
import DatePicker from 'primevue/datepicker'
import Tag from 'primevue/tag'
import Button from 'primevue/button'

const { t } = useI18n()

const items = ref<AuditLogEntry[]>([])
const total = ref(0)
const page = ref(1)
const pageSize = ref(50)
const isLoading = ref(false)

const actionFilter = ref<AuditAction | ''>('')
const targetTypeFilter = ref<AuditTargetType | ''>('')
const adminSearch = ref('')
const startDate = ref<Date | null>(null)
const endDate = ref<Date | null>(null)
const expandedRows = ref<Record<number, boolean>>({})

const debouncedAdminSearch = useDebounce(adminSearch, 300)

const actionOptions = computedAllActionOptions()
const targetTypeOptions = computedTargetTypeOptions()

function computedAllActionOptions() {
  return [
    { label: t('audit.filters.allActions'), value: '' },
    { label: t('audit.actions.create'), value: 'create' },
    { label: t('audit.actions.update'), value: 'update' },
    { label: t('audit.actions.delete'), value: 'delete' },
    { label: t('audit.actions.login'), value: 'login' },
    { label: t('audit.actions.logout'), value: 'logout' },
    { label: t('audit.actions.kick_user'), value: 'kick_user' },
  ]
}

function computedTargetTypeOptions() {
  return [
    { label: t('audit.filters.allTargets'), value: '' },
    { label: t('audit.targets.user'), value: 'user' },
    { label: t('audit.targets.node'), value: 'node' },
    { label: t('audit.targets.admin'), value: 'admin' },
    { label: t('audit.targets.system'), value: 'system' },
  ]
}

function getActionSeverity(action: AuditAction): 'success' | 'info' | 'warn' | 'danger' | 'secondary' | 'contrast' | undefined {
  switch (action) {
    case 'create': return 'success'
    case 'update': return 'info'
    case 'delete': return 'danger'
    case 'login': return 'info'
    case 'logout': return 'secondary'
    case 'kick_user': return 'warn'
    default: return undefined
  }
}

function getTargetTypeIcon(targetType: AuditTargetType): string {
  switch (targetType) {
    case 'user': return 'pi pi-user'
    case 'node': return 'pi pi-server'
    case 'admin': return 'pi pi-shield'
    case 'system': return 'pi pi-cog'
    default: return 'pi pi-circle'
  }
}

function formatDetailJson(detail: string | null): string {
  if (!detail) return '--'
  try {
    const parsed = JSON.parse(detail)
    return JSON.stringify(parsed, null, 2)
  } catch {
    return detail
  }
}

async function fetchLogs() {
  isLoading.value = true
  try {
    const params: AuditLogFilters & { page: number; pageSize: number } = {
      page: page.value,
      pageSize: pageSize.value,
    }
    if (actionFilter.value) params.action = actionFilter.value as AuditAction
    if (targetTypeFilter.value) params.targetType = targetTypeFilter.value as AuditTargetType
    if (adminSearch.value) params.search = adminSearch.value
    if (startDate.value) params.startDate = formatDate(startDate.value.toISOString(), 'yyyy-MM-dd')
    if (endDate.value) params.endDate = formatDate(endDate.value.toISOString(), 'yyyy-MM-dd')

    const res = await auditLogApi.getList(params)
    items.value = res.data.items
    total.value = res.data.total
  } finally {
    isLoading.value = false
  }
}

function handlePageChange(event: { page: number }) {
  page.value = event.page + 1
  fetchLogs()
}

function handleFilterChange() {
  page.value = 1
  fetchLogs()
}

function toggleDetail(id: number) {
  if (expandedRows.value[id]) {
    delete expandedRows.value[id]
  } else {
    expandedRows.value = { [id]: true }
  }
}

watch([actionFilter, targetTypeFilter], () => {
  handleFilterChange()
})

watch(debouncedAdminSearch, () => {
  handleFilterChange()
})

watch([startDate, endDate], () => {
  handleFilterChange()
})

onMounted(() => {
  fetchLogs()
})
</script>

<template>
  <div class="p-4 lg:p-6 space-y-6">
    <!-- 页面标题栏 -->
    <div class="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-3">
      <h1 class="text-2xl font-semibold text-[var(--text-primary)]">
        {{ t('audit.title') }}
      </h1>
      <Button
        icon="pi pi-refresh"
        :label="t('common.actions.refresh')"
        severity="secondary"
        class="w-full sm:w-auto"
        @click="fetchLogs"
      />
    </div>

    <!-- 筛选栏 -->
    <div class="flex flex-wrap items-center gap-3">
      <Select
        v-model="actionFilter"
        :options="actionOptions"
        option-label="label"
        option-value="value"
        :placeholder="t('audit.filters.action')"
        class="w-full sm:w-36"
      />
      <Select
        v-model="targetTypeFilter"
        :options="targetTypeOptions"
        option-label="label"
        option-value="value"
        :placeholder="t('audit.filters.targetType')"
        class="w-full sm:w-36"
      />
      <div class="relative w-full sm:w-48">
        <i class="pi pi-search absolute left-3 top-1/2 -translate-y-1/2 text-[var(--text-muted)] text-sm" />
        <InputText
          v-model="adminSearch"
          :placeholder="t('audit.filters.adminId')"
          class="w-full pl-9"
        />
      </div>
      <DatePicker
        v-model="startDate"
        :placeholder="t('audit.filters.startDate')"
        show-icon
        class="w-full sm:w-44"
      />
      <DatePicker
        v-model="endDate"
        :placeholder="t('audit.filters.endDate')"
        show-icon
        class="w-full sm:w-44"
      />
    </div>

    <!-- 表格 -->
    <div class="bg-[var(--bg-elevated)] rounded-md border border-[var(--border-light)] overflow-hidden">
      <DataTable
        :value="items"
        :loading="isLoading"
        striped-rows
        paginator
        :rows="pageSize"
        :first="(page - 1) * pageSize"
        :total-records="total"
        @page="handlePageChange"
      >
        <Column field="id" :header="t('audit.table.columns.id')" class-name="w-16" />
        <Column :header="t('audit.table.columns.adminUsername')" class-name="w-28">
          <template #body="{ data }">
            <span class="text-sm text-[var(--text-primary)] font-medium">
              {{ (data as AuditLogEntry).adminUsername }}
            </span>
          </template>
        </Column>
        <Column :header="t('audit.table.columns.action')" class-name="w-24">
          <template #body="{ data }">
            <Tag
              :value="t(`audit.actions.${(data as AuditLogEntry).action}`)"
              :severity="getActionSeverity((data as AuditLogEntry).action)"
            />
          </template>
        </Column>
        <Column :header="t('audit.table.columns.targetType')" class-name="w-24">
          <template #body="{ data }">
            <div class="flex items-center gap-1.5">
              <i
                :class="getTargetTypeIcon((data as AuditLogEntry).targetType)"
                class="text-[var(--text-secondary)] text-sm"
              />
              <span class="text-sm text-[var(--text-secondary)]">
                {{ t(`audit.targets.${(data as AuditLogEntry).targetType}`) }}
              </span>
            </div>
          </template>
        </Column>
        <Column field="targetId" :header="t('audit.table.columns.targetName')" class-name="w-24">
          <template #body="{ data }">
            <span class="text-sm text-[var(--text-secondary)] font-mono">
              {{ (data as AuditLogEntry).targetId || '--' }}
            </span>
          </template>
        </Column>
        <Column :header="t('audit.table.columns.detail')" class-name="min-w-[200px]">
          <template #body="{ data }">
            <div v-if="(data as AuditLogEntry).detail" class="max-w-[300px]">
              <button
                class="text-sm text-brand-500 hover:text-brand-600 inline-flex items-center gap-1"
                @click="toggleDetail((data as AuditLogEntry).id)"
              >
                <i
                  :class="expandedRows[(data as AuditLogEntry).id] ? 'pi pi-chevron-down' : 'pi pi-chevron-right'"
                  class="text-xs"
                />
                {{ expandedRows[(data as AuditLogEntry).id] ? '收起' : '展开' }}
              </button>
              <div
                v-if="expandedRows[(data as AuditLogEntry).id]"
                class="mt-2 p-3 bg-[var(--bg-secondary)] rounded-md overflow-x-auto"
              >
                <pre class="text-xs text-[var(--text-secondary)] whitespace-pre-wrap font-mono">{{
                  formatDetailJson((data as AuditLogEntry).detail)
                }}</pre>
              </div>
            </div>
            <span v-else class="text-sm text-[var(--text-muted)]">--</span>
          </template>
        </Column>
        <Column :header="t('audit.table.columns.ipAddress')" class-name="w-36">
          <template #body="{ data }">
            <span class="text-sm text-[var(--text-secondary)] font-mono">
              {{ (data as AuditLogEntry).ipAddress || '--' }}
            </span>
          </template>
        </Column>
        <Column :header="t('audit.table.columns.createdAt')" class-name="w-40">
          <template #body="{ data }">
            <span class="text-sm text-[var(--text-secondary)]">
              {{ formatDate((data as AuditLogEntry).createdAt) }}
            </span>
          </template>
        </Column>
      </DataTable>
    </div>
  </div>
</template>
