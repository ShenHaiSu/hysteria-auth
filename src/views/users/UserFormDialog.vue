<script setup lang="ts">
import { ref, watch, computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { useUsersStore } from '@/stores/users.store'
import { useToast } from '@/composables/useToast'
import { validators } from '@/utils/validators'
import type { CreateUserRequest, UpdateUserRequest, UserDto } from '@/types/user.types'

const props = withDefaults(
  defineProps<{
    visible: boolean
    /** 编辑模式传 user，创建模式传 null */
    user?: UserDto | null
  }>(),
  {
    user: null,
  },
)

const emit = defineEmits<{
  'update:visible': [value: boolean]
  saved: []
}>()

const { t } = useI18n()
const usersStore = useUsersStore()
const toast = useToast()

const isEditMode = computed(() => !!props.user)

interface FormData {
  username: string
  password: string
  email?: string
  totalTrafficBytes?: number
  isActive?: boolean
  expiresAt?: string | null
  allowedNodes?: string[]
  remark?: string
}

// DatePicker 使用的 Date 对象
const expiresAtDate = ref<Date | undefined>(undefined)

// 表单数据
const formData = ref<FormData>({
  username: '',
  password: '',
  email: undefined,
  totalTrafficBytes: undefined,
  isActive: true,
  expiresAt: undefined,
  allowedNodes: undefined,
  remark: undefined,
})

// 校验错误
const fieldErrors = ref<Record<string, string>>({})

// 观察 dialog 打开，填充表单
watch(
  () => props.visible,
  (visible) => {
    if (visible) {
      if (props.user) {
        formData.value = {
          username: props.user.username,
          password: '',
          email: props.user.email ?? undefined,
          totalTrafficBytes: props.user.totalTrafficBytes,
          isActive: props.user.isActive,
          expiresAt: props.user.expiresAt,
          allowedNodes: props.user.allowedNodes,
          remark: props.user.remark ?? undefined,
        }
        expiresAtDate.value = props.user.expiresAt ? new Date(props.user.expiresAt) : undefined
      } else {
        formData.value = {
          username: '',
          password: '',
          email: undefined,
          totalTrafficBytes: undefined,
          isActive: true,
          expiresAt: undefined,
          allowedNodes: undefined,
          remark: undefined,
        }
        expiresAtDate.value = undefined
      }
      fieldErrors.value = {}
    }
  },
)

// 同步 DatePicker 变更回 formData
watch(expiresAtDate, (date) => {
  formData.value.expiresAt = date ? date.toISOString() : null
})

function validate(): boolean {
  const errors: Record<string, string> = {}

  // 用户名：必填
  if (!formData.value.username) {
    errors.username = t('validation.required')
  }

  // 密码：创建模式必填
  if (!isEditMode.value) {
    if (!formData.value.password) {
      errors.password = t('validation.required')
    } else if (formData.value.password.length < 8) {
      errors.password = t('validation.password')
    }
  } else if (formData.value.password && formData.value.password.length < 8) {
    errors.password = t('validation.password')
  }

  // 邮箱：可选，格式校验
  if (formData.value.email) {
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/
    if (!emailRegex.test(formData.value.email)) {
      errors.email = t('validation.email')
    }
  }

  // 流量配额：可选，非负数
  if (formData.value.totalTrafficBytes !== undefined && formData.value.totalTrafficBytes !== null) {
    if (formData.value.totalTrafficBytes < 0) {
      errors.totalTrafficBytes = t('validation.trafficBytes')
    }
  }

  fieldErrors.value = errors
  return Object.keys(errors).length === 0
}

async function handleSubmit() {
  if (!validate()) return

  try {
    if (isEditMode.value && props.user) {
      const payload: UpdateUserRequest = {
        email: formData.value.email || undefined,
        totalTrafficBytes: formData.value.totalTrafficBytes,
        isActive: formData.value.isActive,
        expiresAt: formData.value.expiresAt ?? null,
        allowedNodes: formData.value.allowedNodes,
        remark: formData.value.remark || undefined,
      }
      if (formData.value.password) {
        payload.password = formData.value.password
      }
      await usersStore.updateUser(props.user.id, payload)
      toast.success(t('users.toast.updateSuccess'))
    } else {
      const payload: CreateUserRequest = {
        username: formData.value.username,
        password: formData.value.password,
        email: formData.value.email || undefined,
        totalTrafficBytes: formData.value.totalTrafficBytes,
        isActive: formData.value.isActive,
        expiresAt: formData.value.expiresAt ?? null,
        allowedNodes: formData.value.allowedNodes,
        remark: formData.value.remark || undefined,
      }
      await usersStore.createUser(payload)
      toast.success(t('users.toast.createSuccess'))
    }

    emit('update:visible', false)
    emit('saved')
  } catch {
    toast.error(isEditMode.value ? t('common.toast.error') : t('common.toast.error'))
  }
}

function handleCancel() {
  emit('update:visible', false)
}
</script>

<template>
  <Dialog
    :visible="visible"
    :header="isEditMode ? t('users.form.editTitle') : t('users.form.createTitle')"
    :modal="true"
    :closable="true"
    :draggable="false"
    class="w-full max-w-2xl"
    @update:visible="$emit('update:visible', $event)"
  >
    <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
      <!-- 用户名 -->
      <div>
        <label class="block text-sm font-medium text-[var(--text-primary)] mb-1">
          {{ t('users.form.fields.username.label') }}
          <span class="text-[var(--status-error)]">*</span>
        </label>
        <InputText
          v-model="formData.username"
          class="w-full"
          :class="{ 'border-[var(--status-error)]': fieldErrors.username }"
          :disabled="isEditMode"
          :placeholder="t('users.form.fields.username.label')"
        />
        <small v-if="fieldErrors.username" class="text-[var(--status-error)]">
          {{ fieldErrors.username }}
        </small>
      </div>

      <!-- 密码 -->
      <div>
        <label class="block text-sm font-medium text-[var(--text-primary)] mb-1">
          {{ t('users.form.fields.password.label') }}
          <span v-if="!isEditMode" class="text-[var(--status-error)]">*</span>
          <span v-else class="text-xs text-[var(--text-muted)] ml-1">({{ t('common.status.optional') }})</span>
        </label>
        <InputText
          v-model="formData.password"
          type="password"
          class="w-full"
          :class="{ 'border-[var(--status-error)]': fieldErrors.password }"
          :placeholder="isEditMode ? '••••••••' : t('users.form.fields.password.label')"
        />
        <small v-if="fieldErrors.password" class="text-[var(--status-error)]">
          {{ fieldErrors.password }}
        </small>
      </div>

      <!-- 邮箱 -->
      <div>
        <label class="block text-sm font-medium text-[var(--text-primary)] mb-1">
          {{ t('users.form.fields.email.label') }}
        </label>
        <InputText
          v-model="formData.email"
          class="w-full"
          :class="{ 'border-[var(--status-error)]': fieldErrors.email }"
          :placeholder="t('users.form.fields.email.label')"
        />
        <small v-if="fieldErrors.email" class="text-[var(--status-error)]">
          {{ fieldErrors.email }}
        </small>
      </div>

      <!-- 总流量配额 -->
      <div>
        <label class="block text-sm font-medium text-[var(--text-primary)] mb-1">
          {{ t('users.form.fields.totalTrafficBytes.label') }}
        </label>
        <InputNumber
          v-model="formData.totalTrafficBytes"
          class="w-full"
          :min="0"
          :use-grouping="false"
          :placeholder="t('users.form.fields.totalTrafficBytes.hint')"
        />
        <small class="text-[var(--text-muted)]">
          {{ t('users.form.fields.totalTrafficBytes.hint') }}
        </small>
      </div>

      <!-- 激活状态 -->
      <div>
        <label class="block text-sm font-medium text-[var(--text-primary)] mb-1">
          {{ t('users.form.fields.isActive.label') }}
        </label>
        <ToggleSwitch v-model="formData.isActive" />
      </div>

      <!-- 过期时间 -->
      <div>
        <label class="block text-sm font-medium text-[var(--text-primary)] mb-1">
          {{ t('users.form.fields.expiresAt.label') }}
        </label>
        <DatePicker
          v-model="expiresAtDate"
          class="w-full"
          show-icon
          :placeholder="t('users.form.fields.expiresAt.label')"
        />
      </div>

      <!-- 备注 -->
      <div class="md:col-span-2">
        <label class="block text-sm font-medium text-[var(--text-primary)] mb-1">
          {{ t('users.form.fields.remark.label') }}
        </label>
        <Textarea
          v-model="formData.remark"
          class="w-full"
          rows="3"
          :placeholder="t('users.form.fields.remark.label')"
        />
      </div>
    </div>

    <template #footer>
      <Button
        :label="t('common.actions.cancel')"
        severity="secondary"
        @click="handleCancel"
      />
      <Button
        :label="isEditMode ? t('common.actions.save') : t('common.actions.create')"
        :loading="usersStore.isSubmitting"
        @click="handleSubmit"
      />
    </template>
  </Dialog>
</template>
