<script setup lang="ts">
import { computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth.store'
import { useAppStore } from '@/stores/app.store'
import { useThemeStore } from '@/stores/theme.store'
import { useToast } from '@/composables/useToast'
import { useConfirm } from '@/composables/useConfirm'
import { useI18n } from 'vue-i18n'
import type { Locale } from '@/stores/app.store'

const route = useRoute()
const router = useRouter()
const authStore = useAuthStore()
const appStore = useAppStore()
const themeStore = useThemeStore()
const toast = useToast()
const confirm = useConfirm()
const { t, locale } = useI18n()

const pageTitle = computed(() => (route.meta?.title as string) ?? '')

const isDark = computed(() => themeStore.mode === 'dark')

const currentLocaleLabel = computed(() => {
  return locale.value === 'zh-CN' ? 'EN' : '中'
})

function toggleTheme() {
  themeStore.toggleTheme()
  toast.info(
    themeStore.mode === 'dark' ? '已切换至暗色主题' : '已切换至亮色主题',
    '',
    1500,
  )
}

function toggleLocale() {
  const next: Locale = locale.value === 'zh-CN' ? 'en-US' : 'zh-CN'
  locale.value = next
  appStore.setLocale(next)
}

async function handleLogout() {
  const confirmed = await confirm.danger(
    t('auth.logoutConfirm'),
    t('auth.logoutConfirmTitle'),
  )
  if (confirmed) {
    authStore.logout()
  }
}
</script>

<template>
  <header
    class="h-14 bg-[var(--bg-elevated)] border-b border-[var(--border-light)] flex items-center justify-between px-4 lg:px-6 shrink-0"
  >
    <!-- 左侧：汉堡菜单 + 页面标题 -->
    <div class="flex items-center gap-3">
      <!-- 移动端汉堡菜单按钮 -->
      <Button
        icon="pi pi-bars"
        severity="secondary"
        text
        rounded
        class="lg:hidden"
        @click="appStore.toggleMobileMenu()"
      />
      <!-- 桌面端折叠按钮 -->
      <Button
        icon="pi pi-bars"
        severity="secondary"
        text
        rounded
        class="hidden lg:inline-flex"
        @click="appStore.toggleSidebar()"
      />
      <h1 class="text-lg font-semibold text-[var(--text-primary)] truncate">
        {{ pageTitle }}
      </h1>
    </div>

    <!-- 右侧：操作区 -->
    <div class="flex items-center gap-1">
      <!-- 主题切换 -->
      <Button
        :icon="isDark ? 'pi pi-sun' : 'pi pi-moon'"
        severity="secondary"
        text
        rounded
        :title="isDark ? '切换亮色主题' : '切换暗色主题'"
        @click="toggleTheme"
      />

      <!-- 语言切换 -->
      <Button
        :label="currentLocaleLabel"
        severity="secondary"
        text
        rounded
        class="text-xs font-medium w-10"
        title="切换语言"
        @click="toggleLocale"
      />

      <!-- 分隔 -->
      <div class="w-px h-5 bg-[var(--divider)] mx-1" />

      <!-- 用户信息 -->
      <div class="flex items-center gap-2">
        <i class="pi pi-user text-sm text-[var(--text-secondary)]" />
        <span class="text-sm text-[var(--text-secondary)] hidden sm:inline">
          {{ authStore.adminInfo?.username ?? '' }}
        </span>
        <span
          v-if="authStore.role"
          class="text-xs px-2 py-0.5 rounded-sm bg-brand-100 dark:bg-brand-900 text-brand-700 dark:text-brand-300 font-medium hidden sm:inline"
        >
          {{ authStore.role }}
        </span>
      </div>

      <!-- 登出 -->
      <Button
        icon="pi pi-sign-out"
        severity="secondary"
        text
        rounded
        title="登出"
        class="text-[var(--text-secondary)] hover:text-[var(--status-error)]"
        @click="handleLogout"
      />
    </div>
  </header>
</template>
