<script setup lang="ts">
import { ref, onMounted, watch } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useUsersStore } from '@/stores/users.store'
import { useToast } from '@/composables/useToast'
import { useConfirm } from '@/composables/useConfirm'
import { useDebounce } from '@/composables/useDebounce'
import { usePermission } from '@/composables/usePermission'
import { useExportExcel } from '@/composables/useExportExcel'
import { formatFileSize } from '@/utils/format'
import UserFormDialog from './UserFormDialog.vue'
import UserImportDialog from './UserImportDialog.vue'
import AppStatusBadge from '@/components/common/AppStatusBadge.vue'
import AppTrafficText from '@/components/common/AppTrafficText.vue'
import type { UserDto } from '@/types/user.types'
import { DEFAULT_PAGE_SIZE, PAGE_SIZE_OPTIONS } from '@/utils/constants'

const router = useRouter()
const { t } = useI18n()
const usersStore = useUsersStore()
const toast = useToast()
const confirm = useConfirm()
const { canEdit } = usePermission()

// 对话框控制
const showFormDialog = ref(false)
const editingUser = ref<UserDto | null>(null)
const showImportDialog = ref(false)

const { exportToExcel } = useExportExcel()

// 搜索
const searchInput = ref('')
const debouncedSearch = useDebounce(searchInput, 300)

// 筛选
const activeFilter = ref<'all' | 'active' | 'inactive'>('all')

// 页面加载
onMounted(() => {
  usersStore.fetchUsers()
})

// 搜索防抖
watch(debouncedSearch, (val) => {
  usersStore.setSearch(val)
  usersStore.fetchUsers()
})

// 状态筛选
watch(activeFilter, (val) => {
  if (val === 'all') {
    usersStore.filters.isActive = undefined
  } else {
    usersStore.filters.isActive = val === 'active'
  }
  usersStore.page = 1
  usersStore.fetchUsers()
})

// 翻页
function onPageChange(event: { page: number; rows: number }) {
  usersStore.page = event.page + 1
  usersStore.pageSize = event.rows
  usersStore.fetchUsers()
}

// 打开创建对话框
function openCreateDialog() {
  editingUser.value = null
  showFormDialog.value = true
}

// 打开编辑对话框
function openEditDialog(user: UserDto) {
  editingUser.value = user
  showFormDialog.value = true
}

// 对话框保存成功
function onDialogSaved() {
  usersStore.fetchUsers()
}

// 查看详情
function viewDetail(user: UserDto) {
  router.push({ name: 'UserDetail', params: { id: user.id } })
}

// 删除用户
async function handleDelete(user: UserDto) {
  const confirmed = await confirm.confirmDelete(
    t('users.toast.deleteConfirm'),
    t('common.confirm.deleteTitle'),
  )
  if (confirmed) {
    try {
      await usersStore.deleteUser(user.id)
      toast.success(t('users.toast.deleteSuccess'))
      usersStore.fetchUsers()
    } catch {
      toast.error(t('users.toast.deleteFailed'))
    }
  }
}

// 重置流量
async function handleResetTraffic(user: UserDto) {
  const confirmed = await confirm.danger(
    t('users.toast.resetConfirm', '确定要重置该用户的流量吗？此操作不可恢复。'),
    t('common.confirm.title'),
  )
  if (confirmed) {
    try {
      await usersStore.resetTraffic(user.id)
      toast.success(t('users.toast.resetTrafficSuccess'))
      usersStore.fetchUsers()
    } catch {
      toast.error(t('common.toast.error'))
    }
  }
}

// 格式化日期
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

/** 导出当前列表 */
function handleExport() {
  const data = usersStore.items.map((u) => ({
    id: u.id,
    username: u.username,
    email: u.email ?? '',
    isActive: u.isActive ? t('common.status.active') : t('common.status.inactive'),
    totalTrafficBytes: u.totalTrafficBytes,
    usedTrafficBytes: u.usedTrafficBytes,
    createdAt: u.createdAt,
    expiresAt: u.expiresAt ?? '',
    allowedNodes: u.allowedNodes?.join(', ') ?? '',
    remark: u.remark ?? '',
  }))

  exportToExcel(data, [
    { header: 'ID', key: 'id' },
    { header: t('users.table.columns.username'), key: 'username' },
    { header: t('users.table.columns.email'), key: 'email' },
    { header: t('users.table.columns.isActive'), key: 'isActive' },
    { header: t('users.table.columns.totalTraffic'), key: 'totalTrafficBytes', format: (v) => formatFileSize(Number(v)) },
    { header: t('users.table.columns.usedTraffic'), key: 'usedTrafficBytes', format: (v) => formatFileSize(Number(v)) },
    { header: t('users.table.columns.createdAt'), key: 'createdAt' },
    { header: t('users.table.columns.expiresAt'), key: 'expiresAt' },
    { header: t('users.table.columns.allowedNodes'), key: 'allowedNodes' },
    { header: t('users.table.columns.remark'), key: 'remark' },
  ], t('users.title'))

  if (usersStore.items.length === 0) {
    toast.warning(t('common.empty.title'))
  }
}
</script>

<template>
  <div class="p-6 space-y-6">
    <!-- 页面标题 -->
    <div class="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-3">
      <h1 class="text-2xl font-semibold text-[var(--text-primary)]">
        {{ t('users.title') }}
      </h1>
      <div class="flex items-center gap-2">
        <Button
          v-permission="['super_admin', 'admin']"
          :label="t('common.actions.import')"
          icon="pi pi-upload"
          severity="secondary"
          class="hidden sm:flex"
          @click="showImportDialog = true"
        />
        <Button
          v-permission="['super_admin', 'admin']"
          icon="pi pi-upload"
          severity="secondary"
          text
          rounded
          :title="t('common.actions.import')"
          class="sm:hidden"
          @click="showImportDialog = true"
        />
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
          :label="t('common.actions.create')"
          icon="pi pi-plus"
          @click="openCreateDialog"
        />
      </div>
    </div>

    <!-- 搜索 + 筛选栏 -->
    <div class="flex flex-col sm:flex-row items-start sm:items-center gap-3">
      <IconField class="w-full sm:w-72">
        <InputIcon>
          <i class="pi pi-search" />
        </InputIcon>
        <InputText
          v-model="searchInput"
          :placeholder="t('common.actions.search')"
          class="w-full"
        />
      </IconField>

      <SelectButton
        v-model="activeFilter"
        :options="[
          { label: t('common.status.all'), value: 'all' },
          { label: t('common.status.active'), value: 'active' },
          { label: t('common.status.inactive'), value: 'inactive' },
        ]"
        option-label="label"
        option-value="value"
        class="shrink-0"
      />
    </div>

    <!-- 用户列表 -->
    <div class="bg-[var(--bg-elevated)] rounded-md border border-[var(--border-light)]">
      <DataTable
        :value="usersStore.items"
        :loading="usersStore.isLoading"
        :lazy="true"
        :total-records="usersStore.total"
        paginator
        :rows="usersStore.pageSize"
        :first="(usersStore.page - 1) * usersStore.pageSize"
        :rows-per-page-options="PAGE_SIZE_OPTIONS"
        striped-rows
        sort-field="id"
        :sort-order="-1"
        @page="onPageChange"
        @row-click="viewDetail($event.data)"
        class="p-datatable-sm cursor-pointer"
      >
        <!-- ID -->
        <Column field="id" header="ID" :sortable="true" style="width: 80px">
          <template #body="{ data }: { data: UserDto }">
            <span class="text-sm text-[var(--text-secondary)]">{{ data.id }}</span>
          </template>
        </Column>

        <!-- 用户名 -->
        <Column field="username" :header="t('users.table.columns.username')" :sortable="true" style="min-width: 130px">
          <template #body="{ data }: { data: UserDto }">
            <span class="text-sm font-medium text-[var(--text-primary)]">{{ data.username }}</span>
          </template>
        </Column>

        <!-- 邮箱 -->
        <Column field="email" :header="t('users.table.columns.email')" style="min-width: 160px">
          <template #body="{ data }: { data: UserDto }">
            <span class="text-sm text-[var(--text-secondary)]">{{ data.email ?? '-' }}</span>
          </template>
        </Column>

        <!-- 状态 -->
        <Column field="isActive" :header="t('users.table.columns.isActive')" style="width: 100px" class="text-center">
          <template #body="{ data }: { data: UserDto }">
            <AppStatusBadge :type="data.isActive ? 'active' : 'inactive'" />
          </template>
        </Column>

        <!-- 总流量配额 -->
        <Column field="totalTrafficBytes" :header="t('users.table.columns.totalTraffic')" :sortable="true" style="min-width: 120px" class="text-right">
          <template #body="{ data }: { data: UserDto }">
            <span class="text-sm text-[var(--text-primary)]">
              <template v-if="data.totalTrafficBytes === 0">
                {{ t('common.status.unlimited', '不限') }}
              </template>
              <AppTrafficText v-else :bytes="data.totalTrafficBytes" />
            </span>
          </template>
        </Column>

        <!-- 已用流量 -->
        <Column field="usedTrafficBytes" :header="t('users.table.columns.usedTraffic')" :sortable="true" style="min-width: 120px" class="text-right">
          <template #body="{ data }: { data: UserDto }">
            <AppTrafficText :bytes="data.usedTrafficBytes" class="text-sm text-[var(--text-primary)]" />
          </template>
        </Column>

        <!-- 使用率 -->
        <Column header="使用率" style="min-width: 100px" class="text-center">
          <template #body="{ data }: { data: UserDto }">
            <span class="text-sm text-[var(--text-secondary)]">
              {{ data.totalTrafficBytes > 0
                ? ((data.usedTrafficBytes / data.totalTrafficBytes) * 100).toFixed(1) + '%'
                : '-' }}
            </span>
          </template>
        </Column>

        <!-- 创建时间 -->
        <Column field="createdAt" :header="t('users.table.columns.createdAt')" :sortable="true" style="min-width: 150px">
          <template #body="{ data }: { data: UserDto }">
            <span class="text-sm text-[var(--text-secondary)]">{{ formatDate(data.createdAt) }}</span>
          </template>
        </Column>

        <!-- 操作 -->
        <Column :header="t('users.table.columns.actions')" style="min-width: 160px" class="text-center">
          <template #body="{ data }: { data: UserDto }">
            <div class="flex items-center justify-center gap-1">
              <Button
                icon="pi pi-eye"
                severity="secondary"
                text
                rounded
                :title="t('common.actions.view', '查看')"
                size="small"
                @click.stop="viewDetail(data)"
              />
              <Button
                v-permission="['super_admin', 'admin']"
                icon="pi pi-pencil"
                severity="secondary"
                text
                rounded
                :title="t('common.actions.edit')"
                size="small"
                @click.stop="openEditDialog(data)"
              />
              <Button
                v-permission="['super_admin', 'admin']"
                icon="pi pi-refresh"
                severity="secondary"
                text
                rounded
                :title="t('users.traffic.title')"
                size="small"
                @click.stop="handleResetTraffic(data)"
              />
              <Button
                v-permission="['super_admin', 'admin']"
                icon="pi pi-trash"
                severity="danger"
                text
                rounded
                :title="t('common.actions.delete')"
                size="small"
                @click.stop="handleDelete(data)"
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

    <!-- 创建/编辑对话框 -->
    <UserFormDialog
      v-model:visible="showFormDialog"
      :user="editingUser"
      @saved="onDialogSaved"
    />

    <!-- 批量导入对话框 -->
    <UserImportDialog
      v-model:visible="showImportDialog"
      @imported="usersStore.fetchUsers()"
    />
  </div>
</template>
