<script setup lang="ts">
withDefaults(defineProps<{
  text?: string
}>(), {
  text: undefined,
})
</script>

<template>
  <div class="flex items-center justify-center py-16">
    <div class="flex flex-col items-center gap-3">
      <i class="pi pi-spinner pi-spin text-3xl text-brand-500" />
      <span class="text-sm text-[var(--text-muted)]">{{ text ?? $t('common.status.loading') }}</span>
    </div>
  </div>
</template>
