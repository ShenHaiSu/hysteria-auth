<script setup lang="ts">
import { ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useNodesStore } from '@/stores/nodes.store'
import { useToast } from '@/composables/useToast'

const props = withDefaults(
  defineProps<{
    visible: boolean
    /** 节点 ID */
    nodeId: string
    /** 用户 ID */
    userId: number
    /** 用户名 (用于提示信息) */
    username?: string
  }>(),
  {
    username: undefined,
  },
)

const emit = defineEmits<{
  'update:visible': [value: boolean]
  /** 踢下线并禁用用户 */
  'kick-and-disable': [userId: number, nodeId: string]
  /** 仅踢下线 */
  kicked: []
}>()

const { t } = useI18n()
const nodesStore = useNodesStore()
const toast = useToast()

const isSubmitting = ref(false)

async function handleKickOnly() {
  isSubmitting.value = true
  try {
    await nodesStore.kickUser(props.nodeId, props.userId)
    toast.success(t('nodes.toast.kickUserSuccess'))
    emit('kicked')
    emit('update:visible', false)
  } catch {
    toast.error(t('common.toast.error'))
  } finally {
    isSubmitting.value = false
  }
}

function handleKickAndDisable() {
  emit('kick-and-disable', props.userId, props.nodeId)
  emit('update:visible', false)
}

function handleCancel() {
  emit('update:visible', false)
}
</script>

<template>
  <Dialog
    :visible="visible"
    :header="t('common.confirm.title')"
    :modal="true"
    :closable="true"
    :draggable="false"
    class="w-full max-w-lg"
    @update:visible="$emit('update:visible', $event)"
  >
    <div class="space-y-4">
      <!-- 警告图标 + 提示信息 -->
      <div class="flex items-start gap-3">
        <i class="pi pi-exclamation-triangle text-2xl shrink-0 text-[var(--status-warning)] mt-0.5" />
        <div>
          <p class="text-sm text-[var(--text-primary)] leading-relaxed">
            {{ t('nodes.toast.kickUserConfirm') }}
          </p>
          <p
            v-if="username"
            class="text-xs text-[var(--text-muted)] mt-1"
          >
            用户: <span class="font-medium text-[var(--text-primary)]">{{ username }}</span>
          </p>
        </div>
      </div>

      <!-- 操作提示 -->
      <div
        class="flex items-start gap-2 p-3 rounded-md text-sm"
        style="background-color: var(--status-info-bg); color: var(--status-info)"
      >
        <i class="pi pi-info-circle mt-0.5 shrink-0" />
        <span>该操作仅断开当前连接，客户端可能会自动重连。建议同时禁用该用户账号。</span>
      </div>
    </div>

    <template #footer>
      <Button
        :label="t('common.actions.cancel')"
        severity="secondary"
        :disabled="isSubmitting"
        @click="handleCancel"
      />
      <Button
        :label="'踢下线并禁用用户'"
        severity="danger"
        :disabled="isSubmitting"
        @click="handleKickAndDisable"
      />
      <Button
        :label="'仅踢下线'"
        severity="warn"
        :loading="isSubmitting"
        @click="handleKickOnly"
      />
    </template>
  </Dialog>
</template>
