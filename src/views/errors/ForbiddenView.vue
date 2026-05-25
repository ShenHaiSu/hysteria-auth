<script setup lang="ts">
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'

const router = useRouter()
const { t } = useI18n()
</script>

<template>
  <div
    class="flex flex-col items-center justify-center min-h-screen bg-[var(--bg-primary)] px-4 transition-colors duration-200"
  >
    <i class="pi pi-lock text-6xl text-[var(--status-error)] mb-6" />
    <h1 class="text-6xl font-bold text-[var(--text-primary)] mb-3">403</h1>
    <p class="text-lg text-[var(--text-secondary)] mb-2">{{ $t('errors.forbidden.title') }}</p>
    <p class="text-sm text-[var(--text-muted)] mb-8">{{ $t('errors.forbidden.description') }}</p>
    <div class="flex gap-3">
      <Button
        :label="$t('common.actions.back')"
        icon="pi pi-arrow-left"
        severity="secondary"
        @click="router.back()"
      />
      <Button
        label="Dashboard"
        icon="pi pi-home"
        @click="router.push({ name: 'Dashboard' })"
      />
    </div>
  </div>
</template>
