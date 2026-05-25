<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useAdminsStore } from '@/stores/admins.store'
import { useToast } from '@/composables/useToast'
import type { AdminDto, CreateAdminRequest, UpdateAdminRequest } from '@/types/admin.types'
import type { AdminRole } from '@/types/common.types'

// PrimeVue
import Dialog from 'primevue/dialog'
import Button from 'primevue/button'
import InputText from 'primevue/inputtext'
import Select from 'primevue/select'
import Message from 'primevue/message'

const { t } = useI18n()
const adminsStore = useAdminsStore()
const toast = useToast()

const props = defineProps<{
  visible: boolean
  isEdit: boolean
  admin: AdminDto | null
}>()

const emit = defineEmits<{
  'update:visible': [value: boolean]
  saved: []
  close: []
}>()

const formData = ref({
  username: '',
  password: '',
  role: 'admin' as AdminRole,
})

const formErrors = ref<Record<string, string>>({})

const roleOptions = computed(() => [
  { label: t('admins.roles.super_admin'), value: 'super_admin' as AdminRole },
  { label: t('admins.roles.admin'), value: 'admin' as AdminRole },
  { label: t('admins.roles.readonly'), value: 'readonly' as AdminRole },
])

const title = computed(() =>
  props.isEdit ? t('admins.form.editTitle') : t('admins.form.createTitle'),
)

function initForm() {
  if (props.isEdit && props.admin) {
    formData.value = {
      username: props.admin.username,
      password: '',
      role: props.admin.role,
    }
  } else {
    formData.value = {
      username: '',
      password: '',
      role: 'admin',
    }
  }
  formErrors.value = {}
}

watch(
  () => props.visible,
  (newVal) => {
    if (newVal) initForm()
  },
)

function validate(): boolean {
  formErrors.value = {}

  if (!props.isEdit && !formData.value.username.trim()) {
    formErrors.value.username = t('common.validation.required')
  }
  if (!props.isEdit && !formData.value.password.trim()) {
    formErrors.value.password = t('common.validation.required')
  }

  return Object.keys(formErrors.value).length === 0
}

async function handleSubmit() {
  if (!validate()) return

  try {
    if (props.isEdit && props.admin) {
      const updateData: UpdateAdminRequest = {
        role: formData.value.role,
      }
      if (formData.value.password.trim()) {
        updateData.password = formData.value.password
      }
      await adminsStore.updateAdmin(props.admin.id, updateData)
      toast.success(t('admins.toast.updateSuccess'))
    } else {
      const createData: CreateAdminRequest = {
        username: formData.value.username,
        password: formData.value.password,
        role: formData.value.role,
      }
      await adminsStore.createAdmin(createData)
      toast.success(t('admins.toast.createSuccess'))
    }
    emit('saved')
  } catch {
    toast.error(t('common.toast.error'))
  }
}

function handleCancel() {
  emit('close')
}
</script>

<template>
  <Dialog
    :visible="visible"
    :header="title"
    :modal="true"
    class="w-full max-w-lg"
    :pt="{
      root: { class: 'rounded-lg' },
      content: { class: 'bg-[var(--bg-elevated)]' },
    }"
    @update:visible="emit('update:visible', $event)"
  >
    <div class="flex flex-col gap-4 py-2">
      <!-- 用户名 -->
      <div class="flex flex-col gap-1.5">
        <label class="text-sm font-medium text-[var(--text-primary)]">
          {{ t('admins.form.fields.username.label') }}
        </label>
        <InputText
          v-model="formData.username"
          :disabled="isEdit"
          :class="{
            'border-[var(--status-error)]': formErrors.username,
          }"
          class="w-full"
        />
        <small v-if="formErrors.username" class="text-[var(--status-error)] text-xs">
          {{ formErrors.username }}
        </small>
      </div>

      <!-- 密码 -->
      <div class="flex flex-col gap-1.5">
        <label class="text-sm font-medium text-[var(--text-primary)]">
          {{ t('admins.form.fields.password.label') }}
          <span v-if="isEdit" class="text-[var(--text-muted)]">({{ t('common.status.optional') }})</span>
        </label>
        <InputText
          v-model="formData.password"
          type="password"
          :placeholder="isEdit ? '留空则不修改密码' : ''"
          :class="{
            'border-[var(--status-error)]': formErrors.password,
          }"
          class="w-full"
        />
        <small v-if="formErrors.password" class="text-[var(--status-error)] text-xs">
          {{ formErrors.password }}
        </small>
      </div>

      <!-- 角色 -->
      <div class="flex flex-col gap-1.5">
        <label class="text-sm font-medium text-[var(--text-primary)]">
          {{ t('admins.form.fields.role.label') }}
        </label>
        <Select
          v-model="formData.role"
          :options="roleOptions"
          option-label="label"
          option-value="value"
          class="w-full"
        />
      </div>
    </div>

    <template #footer>
      <div class="flex justify-end gap-3">
        <Button
          :label="t('common.actions.cancel')"
          severity="secondary"
          @click="handleCancel"
        />
        <Button
          :label="isEdit ? t('common.actions.save') : t('common.actions.create')"
          :loading="adminsStore.isSubmitting"
          @click="handleSubmit"
        />
      </div>
    </template>
  </Dialog>
</template>
