<script setup lang="ts">
withDefaults(defineProps<{
  title?: string
  description?: string
}>(), {
  title: undefined,
  description: undefined,
})
</script>

<template>
  <div class="flex flex-col items-center justify-center py-12 text-[var(--text-muted)]">
    <i class="pi pi-inbox text-4xl mb-4" />
    <p class="text-lg font-medium">{{ title ?? $t('common.empty.title') }}</p>
    <p v-if="description" class="text-sm mt-1">{{ description }}</p>
    <p v-else class="text-sm mt-1">{{ $t('common.empty.description') }}</p>
  </div>
</template>
