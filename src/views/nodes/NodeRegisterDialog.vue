<script setup lang="ts">
import { ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useNodesStore } from '@/stores/nodes.store'
import { useToast } from '@/composables/useToast'
import type { PreRegisterNodeRequest, PreRegisterNodeResponse } from '@/types/node.types'

const props = withDefaults(
  defineProps<{
    visible: boolean
  }>(),
  {},
)

const emit = defineEmits<{
  'update:visible': [value: boolean]
  saved: []
}>()

const { t } = useI18n()
const nodesStore = useNodesStore()
const toast = useToast()

// 阶段：form | success
type Phase = 'form' | 'success'
const phase = ref<Phase>('form')

interface FormData {
  name: string
  location: string
  port: number | undefined
  trafficStatsPort: number | undefined
}

const formData = ref<FormData>({
  name: '',
  location: '',
  port: undefined,
  trafficStatsPort: undefined,
})

const fieldErrors = ref<Record<string, string>>({})
const registerResult = ref<PreRegisterNodeResponse | null>(null)

// 打开时重置
watch(
  () => props.visible,
  (visible) => {
    if (visible) {
      phase.value = 'form'
      formData.value = {
        name: '',
        location: '',
        port: undefined,
        trafficStatsPort: undefined,
      }
      fieldErrors.value = {}
      registerResult.value = null
    }
  },
)

function validate(): boolean {
  const errors: Record<string, string> = {}
  if (!formData.value.name.trim()) {
    errors.name = t('common.validation.required')
  }
  fieldErrors.value = errors
  return Object.keys(errors).length === 0
}

async function handleSubmit() {
  if (!validate()) return

  try {
    const payload: PreRegisterNodeRequest = {
      name: formData.value.name.trim(),
    }
    if (formData.value.location.trim()) {
      payload.location = formData.value.location.trim()
    }
    if (formData.value.port !== undefined && formData.value.port !== null) {
      payload.port = formData.value.port
    }
    if (formData.value.trafficStatsPort !== undefined && formData.value.trafficStatsPort !== null) {
      payload.trafficStatsPort = formData.value.trafficStatsPort
    }

    registerResult.value = await nodesStore.preRegisterNode(payload)
    phase.value = 'success'
    toast.success(t('nodes.toast.registerSuccess'))
    emit('saved')
  } catch {
    toast.error(t('common.toast.error'))
  }
}

async function copyToClipboard(text: string) {
  try {
    await navigator.clipboard.writeText(text)
    toast.success(t('nodes.toast.copySuccess'))
  } catch {
    // Fallback for non-HTTPS contexts
    const textarea = document.createElement('textarea')
    textarea.value = text
    textarea.style.position = 'fixed'
    textarea.style.opacity = '0'
    document.body.appendChild(textarea)
    textarea.select()
    try {
      document.execCommand('copy')
      toast.success(t('nodes.toast.copySuccess'))
    } catch {
      toast.error(t('nodes.toast.copyFailed'))
    }
    document.body.removeChild(textarea)
  }
}

function handleClose() {
  emit('update:visible', false)
}

function formatExpiresDate(iso: string): string {
  const d = new Date(iso)
  return d.toLocaleString('zh-CN', {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
  })
}
</script>

<template>
  <Dialog
    :visible="visible"
    :header="phase === 'form' ? t('nodes.form.registerTitle') : t('nodes.register.successTitle')"
    :modal="true"
    :closable="true"
    :draggable="false"
    class="w-full max-w-2xl"
    @update:visible="$emit('update:visible', $event)"
    @hide="handleClose"
  >
    <!-- 表单阶段 -->
    <template v-if="phase === 'form'">
      <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
        <!-- 节点名称 -->
        <div class="md:col-span-2">
          <label class="block text-sm font-medium text-[var(--text-primary)] mb-1">
            {{ t('nodes.form.fields.name.label') }}
            <span class="text-[var(--status-error)]">*</span>
          </label>
          <InputText
            v-model="formData.name"
            class="w-full"
            :class="{ 'border-[var(--status-error)]': fieldErrors.name }"
            :placeholder="t('nodes.form.fields.name.label')"
          />
          <small v-if="fieldErrors.name" class="text-[var(--status-error)]">
            {{ fieldErrors.name }}
          </small>
        </div>

        <!-- 地区 -->
        <div>
          <label class="block text-sm font-medium text-[var(--text-primary)] mb-1">
            {{ t('nodes.form.fields.location.label') }}
            <span class="text-xs text-[var(--text-muted)] ml-1">({{ t('common.status.optional') }})</span>
          </label>
          <InputText
            v-model="formData.location"
            class="w-full"
            :placeholder="t('nodes.form.fields.location.label')"
          />
        </div>

        <!-- 端口 -->
        <div>
          <label class="block text-sm font-medium text-[var(--text-primary)] mb-1">
            {{ t('nodes.form.fields.port.label') }}
            <span class="text-xs text-[var(--text-muted)] ml-1">({{ t('common.status.optional') }})</span>
          </label>
          <InputNumber
            v-model="formData.port"
            class="w-full"
            :min="1"
            :max="65535"
            :use-grouping="false"
            :placeholder="t('nodes.form.fields.port.label')"
          />
        </div>

        <!-- 流量统计端口 -->
        <div>
          <label class="block text-sm font-medium text-[var(--text-primary)] mb-1">
            {{ t('nodes.form.fields.trafficStatsPort.label') }}
            <span class="text-xs text-[var(--text-muted)] ml-1">({{ t('common.status.optional') }})</span>
          </label>
          <InputNumber
            v-model="formData.trafficStatsPort"
            class="w-full"
            :min="1"
            :max="65535"
            :use-grouping="false"
            :placeholder="t('nodes.form.fields.trafficStatsPort.label')"
          />
        </div>
      </div>

      <template #footer>
        <Button
          :label="t('common.actions.cancel')"
          severity="secondary"
          @click="handleClose"
        />
        <Button
          :label="t('common.actions.submit')"
          :loading="nodesStore.isSubmitting"
          @click="handleSubmit"
        />
      </template>
    </template>

    <!-- 成功阶段 -->
    <template v-else-if="phase === 'success' && registerResult">
      <!-- 警告 -->
      <div
        class="flex items-start gap-3 p-3 mb-5 rounded-md text-sm"
        style="background-color: var(--status-warning-bg); color: var(--status-warning)"
      >
        <i class="pi pi-exclamation-triangle mt-0.5 shrink-0" />
        <span>{{ t('nodes.register.tokenWarning') }}</span>
      </div>

      <!-- Master Server URL -->
      <div class="mb-4">
        <label class="block text-xs text-[var(--text-muted)] uppercase tracking-wide mb-1">
          {{ t('nodes.register.masterServerUrl') }}
        </label>
        <p class="text-sm text-[var(--text-primary)] font-mono">{{ registerResult.masterServerUrl }}</p>
      </div>

      <!-- 预注册令牌 -->
      <div class="mb-4">
        <div class="flex items-center justify-between mb-1">
          <label class="text-xs text-[var(--text-muted)] uppercase tracking-wide">
            {{ t('nodes.register.provisionToken') }}
          </label>
          <Button
            :label="t('nodes.register.copy')"
            icon="pi pi-copy"
            severity="secondary"
            size="small"
            text
            @click="copyToClipboard(registerResult.provisionToken)"
          />
        </div>
        <div
          class="bg-[var(--bg-secondary)] border border-[var(--border-light)] rounded-md p-3 font-mono text-sm text-[var(--text-primary)] break-all select-all"
        >
          {{ registerResult.provisionToken }}
        </div>
      </div>

      <!-- 启动命令 -->
      <div class="mb-4">
        <div class="flex items-center justify-between mb-1">
          <label class="text-xs text-[var(--text-muted)] uppercase tracking-wide">
            {{ t('nodes.register.startupCommand') }}
          </label>
          <Button
            :label="t('nodes.register.copy')"
            icon="pi pi-copy"
            severity="secondary"
            size="small"
            text
            @click="copyToClipboard(registerResult.startupCommand)"
          />
        </div>
        <div
          class="bg-[var(--bg-secondary)] border border-[var(--border-light)] rounded-md p-3 font-mono text-sm text-[var(--text-primary)] break-all select-all"
        >
          {{ registerResult.startupCommand }}
        </div>
      </div>

      <!-- 令牌有效期 -->
      <div class="mb-2">
        <label class="block text-xs text-[var(--text-muted)] uppercase tracking-wide mb-1">
          {{ t('nodes.register.expiresAt') }}
        </label>
        <p class="text-sm text-[var(--text-primary)]">{{ formatExpiresDate(registerResult.expiresAt) }}</p>
      </div>

      <template #footer>
        <Button
          :label="t('common.actions.close')"
          severity="secondary"
          @click="handleClose"
        />
      </template>
    </template>
  </Dialog>
</template>
