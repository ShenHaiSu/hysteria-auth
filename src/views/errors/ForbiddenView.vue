<script setup lang="ts">
import { useRouter } from 'vue-router'

const router = useRouter()
</script>

<template>
  <div class="flex flex-col items-center justify-center min-h-screen bg-[var(--bg-primary)]">
    <i class="pi pi-lock text-6xl text-[var(--status-error)] mb-4" />
    <h1 class="text-2xl font-semibold text-[var(--text-primary)] mb-2">403</h1>
    <p class="text-[var(--text-secondary)] mb-4">无权限访问此页面</p>
    <Button
      :label="$t('common.actions.back')"
      severity="secondary"
      @click="router.push({ name: 'Dashboard' })"
    />
  </div>
</template>
