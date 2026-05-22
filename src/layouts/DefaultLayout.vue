<script setup lang="ts">
import { ref, computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth.store'
import { useAppStore } from '@/stores/app.store'
import { routes as allRoutes } from '@/router/routes'

const route = useRoute()
const router = useRouter()
const authStore = useAuthStore()
const appStore = useAppStore()

const isSidebarCollapsed = computed(() => appStore.sidebarCollapsed)

const menuItems = computed(() => {
  const defaultLayout = allRoutes.find((r) => r.path === '/')
  const role = authStore.role
  return (
    defaultLayout?.children?.filter(
      (r) => !r.meta?.hidden && (!r.meta?.roles || (role && r.meta.roles.includes(role))),
    ) ?? []
  )
})

function isActive(item: (typeof menuItems.value)[number]) {
  return route.path.startsWith(`/${item.path}`)
}
</script>

<template>
  <div class="flex h-screen bg-[var(--bg-primary)]">
    <!-- Phase 1 — 完整侧边栏 + 顶部栏实现 -->
    <aside
      class="hidden lg:block w-60 bg-[var(--sidebar-bg)] transition-width duration-300"
      :class="{ 'w-16': isSidebarCollapsed }"
    >
      <div class="flex items-center h-14 px-4">
        <span
          class="text-[var(--sidebar-text-active)] text-lg font-semibold"
          v-show="!isSidebarCollapsed"
        >
          Hysteria Auth
        </span>
      </div>
      <nav class="mt-2">
        <router-link
          v-for="item in menuItems"
          :key="item.name"
          :to="{ name: item.name }"
          class="flex items-center gap-3 px-4 py-3 text-[var(--sidebar-text)] hover:bg-[var(--sidebar-bg-hover)] transition-colors duration-100 rounded-md mx-2"
          :class="{
            'bg-[var(--sidebar-bg-active)] text-[var(--sidebar-text-active)]': isActive(item),
          }"
        >
          <i :class="item.meta?.icon" class="text-lg" />
          <span v-show="!isSidebarCollapsed" class="text-sm font-medium">{{
            item.meta?.title
          }}</span>
        </router-link>
      </nav>
    </aside>

    <main class="flex-1 flex flex-col overflow-auto">
      <header
        class="h-14 bg-[var(--bg-elevated)] border-b border-[var(--border-light)] flex items-center justify-between px-4 lg:px-6"
      >
        <div class="flex items-center gap-3">
          <Button
            icon="pi pi-bars"
            severity="secondary"
            text
            @click="appStore.toggleSidebar()"
            class="lg:inline-flex"
          />
          <span class="text-lg font-semibold text-[var(--text-primary)]">
            {{ route.meta?.title }}
          </span>
        </div>
        <div class="flex items-center gap-2">
          <span class="text-sm text-[var(--text-secondary)]">
            {{ authStore.adminInfo?.username ?? 'Loading...' }}
          </span>
        </div>
      </header>

      <div class="flex-1 overflow-auto">
        <RouterView />
      </div>

      <footer
        class="h-10 border-t border-[var(--border-light)] flex items-center justify-center text-xs text-[var(--text-muted)]"
      >
        Hysteria Auth &copy; 2026
      </footer>
    </main>
  </div>
</template>
