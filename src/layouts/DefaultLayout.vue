<template>
  <div class="flex h-screen bg-[var(--bg-primary)] overflow-hidden">
    <!-- 桌面端：侧边栏 (≥1024px) -->
    <AppSidebar />

    <!-- 移动端：覆盖式抽屉菜单 -->
    <Drawer
      v-model:visible="appStore.mobileMenuVisible"
      position="left"
      class="lg:hidden !w-60"
      :pt="{
        content: { class: 'p-0 bg-[var(--sidebar-bg)]' },
        header: {
          class:
            'bg-[var(--sidebar-bg)] text-[var(--sidebar-text-active)] border-b border-[var(--sidebar-border)]',
        },
      }"
    >
      <template #header>
        <div class="flex items-center gap-2">
          <i class="pi pi-bolt text-xl text-brand-400" />
          <span class="text-lg font-semibold">Hysteria Auth</span>
        </div>
      </template>

      <nav class="mt-2">
        <router-link
          v-for="item in mobileMenuItems"
          :key="item.name"
          :to="{ name: item.name }"
          class="flex items-center gap-3 px-4 py-3 mx-2 mb-1 text-[var(--sidebar-text)] hover:bg-[var(--sidebar-bg-hover)] transition-colors duration-100 rounded-md"
          :class="{
            'bg-[var(--sidebar-bg-active)] text-[var(--sidebar-text-active)]':
              item.path && isMobileItemActive(item.path),
          }"
          @click="onMobileNav(item.name)"
        >
          <i :class="item.meta?.icon" class="text-lg shrink-0" />
          <span class="text-sm font-medium">{{ item.meta?.title }}</span>
        </router-link>
      </nav>
    </Drawer>

    <!-- 主内容区 -->
    <main class="flex-1 flex flex-col min-w-0 overflow-auto">
      <AppHeader />

      <!-- 面包屑 -->
      <div class="px-4 lg:px-6 pt-3">
        <AppBreadcrumb />
      </div>

      <!-- 内容区 -->
      <div class="flex-1 overflow-auto p-4 lg:p-6">
        <RouterView />
      </div>

      <AppFooter />
    </main>
  </div>
</template>

<script setup lang="ts">
import { computed, watch } from 'vue'
import { useRoute } from 'vue-router'
import { useAuthStore } from '@/stores/auth.store'
import { useAppStore } from '@/stores/app.store'
import { routes as allRoutes } from '@/router/routes'
import AppSidebar from './DefaultLayout/components/AppSidebar.vue'
import AppHeader from './DefaultLayout/components/AppHeader.vue'
import AppFooter from './DefaultLayout/components/AppFooter.vue'
import AppBreadcrumb from './DefaultLayout/components/AppBreadcrumb.vue'
import type { AdminRole } from '@/types/common.types'

const route = useRoute()
const authStore = useAuthStore()
const appStore = useAppStore()

// 移动端菜单项（与 AppSidebar 逻辑一致）
const mobileMenuItems = computed(() => {
  const defaultLayout = allRoutes.find((r) => r.path === '/')
  const role: AdminRole | null = authStore.role
  return (
    defaultLayout?.children?.filter(
      (r) => !r.meta?.hidden && (!r.meta?.roles || (role && r.meta.roles.includes(role))),
    ) ?? []
  )
})

function isMobileItemActive(path: string) {
  return route.path.startsWith(`/${path}`)
}

function onMobileNav(name: string | symbol | undefined) {
  if (name) {
    appStore.closeMobileMenu()
  }
}

// 路由变化时关闭移动菜单
watch(
  () => route.fullPath,
  () => {
    if (appStore.mobileMenuVisible) {
      appStore.closeMobileMenu()
    }
  },
)
</script>
