<script setup lang="ts">
const emit = defineEmits<{
  retry: []
}>()

withDefaults(defineProps<{
  title?: string
  description?: string
  showRetry?: boolean
}>(), {
  title: undefined,
  description: undefined,
  showRetry: true,
})

function handleRetry() {
  emit('retry')
}
</script>

<template>
  <div class="flex flex-col items-center justify-center py-12 text-[var(--status-error)]">
    <i class="pi pi-exclamation-triangle text-4xl mb-4" />
    <p class="text-lg font-medium">{{ title ?? $t('common.error.loadFailed') }}</p>
    <p class="text-sm mt-1 text-[var(--text-muted)]">
      {{ description ?? $t('common.error.loadFailedDetail') }}
    </p>
    <Button
      v-if="showRetry"
      :label="$t('common.actions.retry')"
      severity="secondary"
      class="mt-4"
      @click="handleRetry"
    />
  </div>
</template>
