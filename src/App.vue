<template>
  <div class="app-shell">
    <!-- 顶部路由加载浮窗：仅在路由跳转期间显示 -->
    <Transition name="loading-bar-slide">
      <div
        v-if="appStore.routeLoading"
        class="fixed top-0 left-0 right-0 z-[9999] h-1 bg-[var(--brand-500)] shadow-[0_0_6px_var(--brand-500)] before:absolute before:inset-0 before:animate-loading-bar before:bg-[linear-gradient(90deg,transparent,var(--brand-200),transparent)]"
        role="alert"
        aria-label="页面加载中"
      />
    </Transition>

    <RouterView />
    <Toast position="top-right" />
    <ConfirmDialog />
  </div>
</template>

<script setup lang="ts">
import { useAppStore } from '@/stores/app.store'
import { useI18n } from 'vue-i18n'

const appStore = useAppStore()
const { locale } = useI18n()

// 初始化语言
const savedLocale = localStorage.getItem('locale')
if (savedLocale) {
  locale.value = savedLocale
}
</script>
