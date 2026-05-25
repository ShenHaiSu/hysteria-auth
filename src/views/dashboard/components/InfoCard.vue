<template>
  <div class="bg-[var(--bg-elevated)] rounded-md border border-[var(--border-light)] shadow-md p-5">
    <div class="flex items-center gap-3">
      <!-- 图标 -->
      <div
        class="flex-shrink-0 w-10 h-10 rounded-md flex items-center justify-center"
        :class="iconBgClass"
      >
        <i :class="[icon, iconColorClass]" class="text-xl" />
      </div>

      <!-- 内容区 -->
      <div class="flex-1 min-w-0">
        <!-- 标题 -->
        <p class="text-sm text-[var(--text-secondary)] mb-1">{{ title }}</p>

        <!-- 合并展示的值 -->
        <template v-if="loading">
          <div class="h-6 w-24 bg-[var(--bg-secondary)] rounded animate-pulse" />
        </template>
        <template v-else>
          <p class="text-lg font-bold text-[var(--text-primary)] truncate">
            {{ value }}
          </p>
        </template>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
defineProps<{
  icon: string
  title: string
  value: string
  iconBgClass?: string
  iconColorClass?: string
  loading?: boolean
}>()
</script>
