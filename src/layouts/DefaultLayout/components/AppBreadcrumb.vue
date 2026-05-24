<script setup lang="ts">
import { computed } from 'vue'
import { useRoute } from 'vue-router'

const route = useRoute()

interface BreadcrumbItem {
  label: string
  to?: { name: string }
}

const items = computed<BreadcrumbItem[]>(() => {
  const matched = route.matched.filter((r) => r.meta?.title)
  return matched.map((r) => ({
    label: (r.meta?.title as string) ?? '',
    to: r.name ? { name: r.name as string } : undefined,
  }))
})
</script>

<template>
  <nav v-if="items.length > 1" class="flex items-center gap-2 text-sm text-[var(--text-muted)]" aria-label="面包屑导航">
    <template v-for="(item, index) in items" :key="index">
      <span v-if="index > 0" class="text-[var(--text-muted)]">/</span>
      <router-link
        v-if="item.to && index < items.length - 1"
        :to="item.to"
        class="hover:text-[var(--text-primary)] transition-colors duration-100"
      >
        {{ item.label }}
      </router-link>
      <span
        v-else
        class="text-[var(--text-primary)] font-medium"
      >
        {{ item.label }}
      </span>
    </template>
  </nav>
</template>
