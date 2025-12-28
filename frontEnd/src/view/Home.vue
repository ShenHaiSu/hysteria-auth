<template>
  <div class="p-4 mx-auto">
    <!-- 用户信息概览 -->
    <div class="flex justify-between items-center mb-6">
      <div>
        <h1 class="text-3xl font-bold text-surface-900">
          欢迎回来, {{ authStore.user?.username || '用户' }}
        </h1>
        <p class="text-surface-500 mt-1">最后登录: {{ lastLogin }}</p>
      </div>
      <Button
        label="退出登录"
        icon="pi pi-sign-out"
        severity="danger"
        text
        @click="handleLogout"
        :loading="isLoggingOut"
      ></Button>
    </div>

    <!-- 仪表盘组件网格 -->
    <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
      <!-- 服务器状态 (所有用户可见) -->
      <Suspense>
        <template #default>
          <ServerStatus />
        </template>
        <template #fallback>
          <Skeleton height="200px" />
        </template>
      </Suspense>

      <!-- 访问信息 (所有用户可见) -->
      <Suspense>
        <template #default>
          <ClientInfo />
        </template>
        <template #fallback>
          <Skeleton height="200px" />
        </template>
      </Suspense>

      <!-- 节点概览 -->
      <Suspense>
        <template #default>
          <NodeStats />
        </template>
        <template #fallback>
          <Skeleton height="200px" />
        </template>
      </Suspense>

      <!-- 快速配置 -->
      <Suspense>
        <template #default>
          <QuickConfig />
        </template>
        <template #fallback>
          <Skeleton height="200px" />
        </template>
      </Suspense>

      <!-- 用户统计 (仅管理员可见) -->
      <Suspense v-if="authStore.isAdmin">
        <template #default>
          <UserStats />
        </template>
        <template #fallback>
          <Skeleton height="200px" />
        </template>
      </Suspense>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, defineAsyncComponent } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import Button from 'primevue/button'
import Skeleton from 'primevue/skeleton'

// #region 异步组件加载 (Lazy Loading Components)
const ServerStatus = defineAsyncComponent(() => import('@/components/Home/ServerStatus.vue'))
const ClientInfo = defineAsyncComponent(() => import('@/components/Home/ClientInfo.vue'))
const NodeStats = defineAsyncComponent(() => import('@/components/Home/NodeStats.vue'))
const QuickConfig = defineAsyncComponent(() => import('@/components/Home/QuickConfig.vue'))
const UserStats = defineAsyncComponent(() => import('@/components/Home/UserStats.vue'))
// #endregion

const authStore = useAuthStore()
const router = useRouter()
const isLoggingOut = ref(false)

// #region 逻辑处理 (Logic)
/**
 * 处理登出逻辑
 * 调用 store 的登出方法并跳转至登录页
 */
async function handleLogout() {
  isLoggingOut.value = true
  try {
    await authStore.logout()
    router.push('/login')
  } finally {
    isLoggingOut.value = false
  }
}
// #endregion

const lastLogin = computed(() => {
  if (!authStore.user?.last_login_ts) return '未知'
  return new Date(authStore.user.last_login_ts * 1000).toLocaleString()
})
</script>
