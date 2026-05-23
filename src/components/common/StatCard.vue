<script setup lang="ts">
import type { Component } from 'vue'

defineProps<{
  icon: string
  label: string
  value: string
  iconBgClass?: string
  iconColorClass?: string
  loading?: boolean
}>()

const skeletonWidth = 'w-16'
</script>

<template>
  <div class="bg-[var(--bg-elevated)] rounded-md border border-[var(--border-light)] shadow-md p-5">
    <div class="flex items-center justify-between">
      <div class="flex-1 min-w-0">
        <p class="text-sm text-[var(--text-secondary)] mb-1">{{ label }}</p>
        <template v-if="loading">
          <div class="h-6 w-16 bg-[var(--bg-secondary)] rounded animate-pulse" />
        </template>
        <template v-else>
          <p class="text-xl font-bold text-[var(--text-primary)] truncate">{{ value }}</p>
        </template>
      </div>
      <div
        class="flex-shrink-0 w-10 h-10 rounded-md flex items-center justify-center"
        :class="iconBgClass || 'bg-brand-100'"
      >
        <i
          :class="[icon, iconColorClass || 'text-brand-500']"
          class="text-xl"
        />
      </div>
    </div>
  </div>
</template>
