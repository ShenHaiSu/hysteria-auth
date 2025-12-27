<template>
  <div class="p-4">
    <!-- 用户信息概览 -->
    <div class="flex justify-between items-center mb-4">
      <h1 class="text-2xl font-bold">概览</h1>
      <Button
        label="退出登录"
        icon="pi pi-sign-out"
        severity="danger"
        @click="handleLogout"
        :loading="isLoggingOut"
      />
    </div>
    <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
      <Card>
        <template #title>欢迎</template>
        <template #content>
          <p>您好，{{ authStore.user?.username || '用户' }}</p>
          <p class="text-sm text-surface-500 mt-2">最后登录: {{ lastLogin }}</p>
        </template>
      </Card>
    </div>

    <!-- debug展示的 -->
    <!-- <div class="mt-8">
      <Button label="测试按钮" icon="pi pi-check" />
      <DatePicker v-model="dateData" show-button-bar class="ml-4" />
    </div> -->
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import Card from 'primevue/card'
import Button from 'primevue/button'
// import DatePicker from 'primevue/datepicker'

const authStore = useAuthStore()
const router = useRouter()
// const dateData = ref<Date>(new Date())
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
