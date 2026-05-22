<script setup lang="ts">
import type { RouteRecordRaw } from 'vue-router'
import { computed } from 'vue'
import { useRoute } from 'vue-router'
import { useAuthStore } from '@/stores/auth.store'
import { useAppStore } from '@/stores/app.store'
import { routes as allRoutes } from '@/router/routes'

const route = useRoute()
const authStore = useAuthStore()
const appStore = useAppStore()

const isCollapsed = computed(() => appStore.sidebarCollapsed)

const menuItems = computed(() => {
  const defaultLayout = allRoutes.find((r) => r.path === '/')
  const role = authStore.role
  return (
    defaultLayout?.children?.filter(
      (r) => !r.meta?.hidden && (!r.meta?.roles || (role && r.meta.roles.includes(role))),
    ) ?? []
  )
})

function isActive(item: RouteRecordRaw) {
  if (!item.path) return false
  return route.path.startsWith(`/${item.path}`)
}
</script>

<template>
  <aside
    class="hidden lg:flex lg:flex-col bg-[var(--sidebar-bg)] transition-all duration-300 overflow-hidden"
    :class="isCollapsed ? 'w-16' : 'w-60'"
  >
    <!-- Logo 区域 -->
    <div class="flex items-center h-14 px-4 shrink-0 border-b border-[var(--sidebar-border)]">
      <i class="pi pi-bolt text-xl text-brand-400" />
      <span
        v-show="!isCollapsed"
        class="ml-3 text-[var(--sidebar-text-active)] text-lg font-semibold whitespace-nowrap"
      >
        Hysteria Auth
      </span>
    </div>

    <!-- 导航菜单 -->
    <nav class="flex-1 mt-2 overflow-y-auto">
      <router-link
        v-for="item in menuItems"
        :key="item.name"
        :to="{ name: item.name }"
        class="flex items-center gap-3 px-4 py-3 mx-2 mb-1 text-[var(--sidebar-text)] hover:bg-[var(--sidebar-bg-hover)] transition-colors duration-100 rounded-md whitespace-nowrap"
        :class="{
          'bg-[var(--sidebar-bg-active)] text-[var(--sidebar-text-active)]': isActive(item),
        }"
      >
        <i :class="item.meta?.icon" class="text-lg shrink-0" />
        <span v-show="!isCollapsed" class="text-sm font-medium">{{ item.meta?.title }}</span>
      </router-link>
    </nav>

    <!-- 折叠按钮 -->
    <div class="p-3 border-t border-[var(--sidebar-border)]">
      <button
        class="flex items-center justify-center w-full py-2 text-[var(--sidebar-text)] hover:bg-[var(--sidebar-bg-hover)] rounded-md transition-colors duration-100"
        :title="isCollapsed ? '展开侧边栏' : '折叠侧边栏'"
        @click="appStore.toggleSidebar()"
      >
        <i
          class="pi text-lg transition-transform duration-300"
          :class="isCollapsed ? 'pi-angle-right' : 'pi-angle-left'"
        />
      </button>
    </div>
  </aside>
</template>
