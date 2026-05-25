<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from '@/stores/auth.store'
import { useAdminsStore } from '@/stores/admins.store'
import { useToast } from '@/composables/useToast'
import { useConfirm } from '@/composables/useConfirm'
import { formatDate } from '@/utils/format'
import type { AdminDto, UpdateAdminRequest } from '@/types/admin.types'
import type { AdminRole } from '@/types/common.types'
import AppStatusBadge from '@/components/common/AppStatusBadge.vue'
import AdminFormDialog from '@/views/admins/AdminFormDialog.vue'

// PrimeVue
import DataTable from 'primevue/datatable'
import Column from 'primevue/column'
import Paginator from 'primevue/paginator'
import InputText from 'primevue/inputtext'
import Select from 'primevue/select'
import Button from 'primevue/button'
import Menu from 'primevue/menu'
import Tag from 'primevue/tag'

const { t } = useI18n()
const authStore = useAuthStore()
const adminsStore = useAdminsStore()
const toast = useToast()
const confirm = useConfirm()

const searchQuery = ref('')
const roleFilter = ref<AdminRole | ''>('')
const statusFilter = ref<'all' | 'active' | 'inactive'>('all')

const dialogVisible = ref(false)
const editingAdmin = ref<AdminDto | null>(null)
const isEdit = ref(false)

const menuRef = ref<InstanceType<typeof Menu>>()
const selectedAdmin = ref<AdminDto | null>(null)

const roleOptions = computed(() => [
  { label: t('admins.roles.super_admin'), value: 'super_admin' as AdminRole },
  { label: t('admins.roles.admin'), value: 'admin' as AdminRole },
  { label: t('admins.roles.readonly'), value: 'readonly' as AdminRole },
])

const statusOptions = computed(() => [
  { label: t('common.status.all'), value: 'all' },
  { label: t('common.status.active'), value: 'active' },
  { label: t('common.status.inactive'), value: 'inactive' },
])

function isCurrentUser(admin: AdminDto): boolean {
  return admin.id === authStore.adminInfo?.id
}

function getRoleSeverity(role: AdminRole): 'success' | 'info' | 'warn' | 'danger' | 'secondary' | 'contrast' | undefined {
  switch (role) {
    case 'super_admin': return 'danger'
    case 'admin': return 'info'
    case 'readonly': return 'secondary'
    default: return undefined
  }
}

async function loadData() {
  await adminsStore.fetchAdmins()
}

async function handleSearch() {
  adminsStore.setSearch(searchQuery.value)
  await adminsStore.fetchAdmins()
}

async function handleFilterChange() {
  adminsStore.filters = {
    ...adminsStore.filters,
    role: roleFilter.value || undefined,
    isActive: statusFilter.value === 'all' ? undefined : statusFilter.value === 'active',
  }
  adminsStore.page = 1
  await adminsStore.fetchAdmins()
}

function openCreateDialog() {
  editingAdmin.value = null
  isEdit.value = false
  dialogVisible.value = true
}

function openEditDialog(admin: AdminDto) {
  editingAdmin.value = admin
  isEdit.value = true
  dialogVisible.value = true
}

function openRoleMenu(event: Event, admin: AdminDto) {
  selectedAdmin.value = admin
  menuRef.value?.show(event)
}

async function handleChangeRole(role: AdminRole) {
  if (!selectedAdmin.value) return
  try {
    await adminsStore.updateAdmin(selectedAdmin.value.id, { role })
    toast.success(t('admins.toast.updateSuccess'))
    await adminsStore.fetchAdmins()
  } catch {
    toast.error(t('common.toast.error'))
  } finally {
    selectedAdmin.value = null
  }
}

async function handleToggleActive(admin: AdminDto) {
  if (isCurrentUser(admin)) return
  const action = admin.isActive ? t('common.status.disabled') : t('common.status.enabled')
  const confirmed = await confirm.danger(
    `${action}管理员「${admin.username}」？`,
    t('common.confirm.title'),
  )
  if (!confirmed) return

  try {
    await adminsStore.updateAdmin(admin.id, { isActive: !admin.isActive })
    toast.success(t('admins.toast.updateSuccess'))
    await adminsStore.fetchAdmins()
  } catch {
    toast.error(t('common.toast.error'))
  }
}

function handleDialogClose() {
  dialogVisible.value = false
  editingAdmin.value = null
  isEdit.value = false
}

async function handleDialogSaved() {
  dialogVisible.value = false
  editingAdmin.value = null
  isEdit.value = false
  await adminsStore.fetchAdmins()
}

onMounted(() => {
  loadData()
})

watch([roleFilter, statusFilter], () => {
  handleFilterChange()
})
</script>

<template>
  <div class="p-4 lg:p-6 space-y-6">
    <!-- 页面标题栏 -->
    <div class="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-3">
      <h1 class="text-2xl font-semibold text-[var(--text-primary)]">
        {{ t('admins.title') }}
      </h1>
      <Button
        v-permission="['super_admin']"
        icon="pi pi-plus"
        :label="t('common.actions.create')"
        class="w-full sm:w-auto"
        @click="openCreateDialog"
      />
    </div>

    <!-- 筛选栏 -->
    <div class="flex flex-col sm:flex-row items-start sm:items-center gap-3">
      <div class="relative w-full sm:w-64">
        <i class="pi pi-search absolute left-3 top-1/2 -translate-y-1/2 text-[var(--text-muted)] text-sm" />
        <InputText
          v-model="searchQuery"
          :placeholder="t('common.actions.search')"
          class="w-full pl-9"
          @keyup.enter="handleSearch"
        />
      </div>
      <Select
        v-model="roleFilter"
        :options="roleOptions"
        option-label="label"
        option-value="value"
        :placeholder="t('admins.form.fields.role.label')"
        show-clear
        class="w-full sm:w-44"
      />
      <Select
        v-model="statusFilter"
        :options="statusOptions"
        option-label="label"
        option-value="value"
        class="w-full sm:w-36"
      />
    </div>

    <!-- 表格 -->
    <div class="bg-[var(--bg-elevated)] rounded-md border border-[var(--border-light)] overflow-hidden">
      <DataTable
        :value="adminsStore.items"
        :loading="adminsStore.isLoading"
        striped-rows
        paginator
        :rows="adminsStore.pageSize"
        :first="(adminsStore.page - 1) * adminsStore.pageSize"
        :total-records="adminsStore.total"
        @page="adminsStore.setPage($event.page + 1)"
      >
        <Column field="id" :header="t('admins.table.columns.id')" class-name="w-20" />
        <Column field="username" :header="t('admins.table.columns.username')" />
        <Column :header="t('admins.table.columns.role')" class-name="w-36">
          <template #body="{ data }">
            <Tag
              :value="t(`admins.roles.${(data as AdminDto).role}`)"
              :severity="getRoleSeverity((data as AdminDto).role)"
            />
          </template>
        </Column>
        <Column :header="t('admins.table.columns.isActive')" class-name="w-24 text-center">
          <template #body="{ data }">
            <AppStatusBadge
              :type="(data as AdminDto).isActive ? 'active' : 'inactive'"
              :label="(data as AdminDto).isActive ? t('common.status.active') : t('common.status.inactive')"
            />
          </template>
        </Column>
        <Column :header="t('admins.table.columns.createdAt')" class-name="w-40">
          <template #body="{ data }">
            <span class="text-sm text-[var(--text-secondary)]">
              {{ formatDate((data as AdminDto).createdAt) }}
            </span>
          </template>
        </Column>
        <Column :header="t('admins.table.columns.lastLoginAt')" class-name="w-40">
          <template #body="{ data }">
            <span class="text-sm text-[var(--text-secondary)]">
              {{ (data as AdminDto).lastLoginAt ? formatDate((data as AdminDto).lastLoginAt!) : '--' }}
            </span>
          </template>
        </Column>
        <Column :header="t('admins.table.columns.actions')" class-name="w-48 text-center">
          <template #body="{ data }">
            <div class="flex items-center justify-center gap-2">
              <Button
                v-permission="['super_admin']"
                icon="pi pi-pencil"
                size="small"
                severity="secondary"
                text
                :aria-label="t('common.actions.edit')"
                @click="openEditDialog(data as AdminDto)"
              />
              <Button
                v-if="!isCurrentUser(data as AdminDto)"
                v-permission="['super_admin']"
                icon="pi pi-user-edit"
                size="small"
                severity="info"
                text
                :aria-label="t('admins.form.fields.role.label')"
                @click="openRoleMenu($event, data as AdminDto)"
              />
              <Button
                v-if="!isCurrentUser(data as AdminDto)"
                v-permission="['super_admin']"
                :icon="(data as AdminDto).isActive ? 'pi pi-ban' : 'pi pi-check-circle'"
                size="small"
                :severity="(data as AdminDto).isActive ? 'warn' : 'success'"
                text
                :aria-label="(data as AdminDto).isActive ? t('common.status.disabled') : t('common.status.enabled')"
                @click="handleToggleActive(data as AdminDto)"
              />
            </div>
          </template>
        </Column>
      </DataTable>
    </div>

    <!-- 角色切换菜单 -->
    <Menu ref="menuRef" :model="roleOptions.map(r => ({
      label: r.label,
      command: () => handleChangeRole(r.value),
    }))" />

    <!-- 创建/编辑对话框 -->
    <AdminFormDialog
      v-model:visible="dialogVisible"
      :is-edit="isEdit"
      :admin="editingAdmin"
      @saved="handleDialogSaved"
      @close="handleDialogClose"
    />
  </div>
</template>
