<template>
  <div
    class="login-container flex items-center justify-center min-h-screen bg-surface-50 dark:bg-surface-950 p-4"
  >
    <!-- #region 登录卡片 -->
    <div class="w-full max-w-md md:max-w-lg lg:max-w-xl transition-all duration-300">
      <Card class="shadow-xl border-none overflow-hidden">
        <template #header>
          <div class="h-2 bg-primary-500"></div>
        </template>

        <template #title>
          <div class="flex flex-col items-center gap-2 pt-6">
            <div
              class="w-16 h-16 rounded-full bg-primary-100 dark:bg-primary-900/30 flex items-center justify-center mb-2"
            >
              <i class="pi pi-shield text-3xl text-primary-600 dark:text-primary-400"></i>
            </div>
            <h1 class="text-2xl font-bold text-surface-900 dark:text-surface-0">欢迎回来</h1>
            <p class="text-surface-500 dark:text-surface-400 text-sm">
              请输入您的凭据以访问控制面板
            </p>
          </div>
        </template>

        <template #content>
          <form @submit.prevent="handleLogin" class="flex flex-col gap-6 px-2 md:px-6 py-4">
            <!-- 用户名输入 -->
            <div class="flex flex-col gap-2">
              <label for="username" class="font-medium text-surface-700 dark:text-surface-200"
                >用户名</label
              >
              <div class="relative">
                <i class="pi pi-user absolute left-3 top-1/2 -translate-y-1/2 text-surface-400"></i>
                <InputText
                  id="username"
                  v-model="loginForm.username"
                  placeholder="请输入用户名"
                  class="w-full pl-10"
                  :disabled="authStore.loading"
                  required
                />
              </div>
            </div>

            <!-- 密码输入 -->
            <div class="flex flex-col gap-2">
              <label for="password" class="font-medium text-surface-700 dark:text-surface-200"
                >密码</label
              >
              <div class="relative">
                <Password
                  id="password"
                  v-model="loginForm.password"
                  placeholder="请输入密码"
                  class="w-full"
                  input-class="w-full pl-10"
                  :feedback="false"
                  toggle-mask
                  :disabled="authStore.loading"
                  required
                >
                  <template #header>
                    <i
                      class="pi pi-lock absolute left-3 top-1/2 -translate-y-1/2 text-surface-400 z-10"
                    ></i>
                  </template>
                </Password>
                <i
                  class="pi pi-lock absolute left-3 top-1/2 -translate-y-1/2 text-surface-400 z-10 pointer-events-none"
                ></i>
              </div>
            </div>

            <!-- 错误信息提示 -->
            <transition name="p-message">
              <Message v-if="errorMsg" severity="error" variant="simple" size="small" class="mt-2">
                {{ errorMsg }}
              </Message>
            </transition>

            <!-- 登录按钮 -->
            <Button
              type="submit"
              label="登录"
              icon="pi pi-sign-in"
              class="w-full mt-2"
              :loading="authStore.loading"
            />
          </form>
        </template>
      </Card>
    </div>
    <!-- #endregion -->
  </div>
</template>

<script setup lang="ts">
// #region 导入
import { ref, reactive } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import md5 from 'md5'
import Card from 'primevue/card'
import InputText from 'primevue/inputtext'
import Password from 'primevue/password'
import Button from 'primevue/button'
import Message from 'primevue/message'
// #endregion

// #region 状态与变量
const router = useRouter()
const authStore = useAuthStore()

/** 登录表单数据 */
const loginForm = reactive({
  username: '',
  password: '',
})

/** 错误提示信息 */
const errorMsg = ref('')
// #endregion

// #region 业务逻辑
/**
 * 处理用户登录逻辑
 * 1. 验证输入
 * 2. 密码进行 MD5 加密
 * 3. 调用 store 的登录方法
 * 4. 成功后跳转到首页
 */
async function handleLogin() {
  if (!loginForm.username || !loginForm.password) {
    errorMsg.value = '请输入用户名和密码'
    return
  }

  errorMsg.value = ''

  try {
    // 构造登录请求，密码使用 MD5 加密
    await authStore.login({
      username: loginForm.username,
      login_password_md5: md5(loginForm.password),
    })

    // 登录成功，跳转到首页
    router.push('/')
  } catch (error: any) {
    console.error('Login failed:', error)
    // 根据后端返回的错误结构显示错误信息
    errorMsg.value =
      error.response?.data?.message || error.message || '登录失败，请检查用户名或密码'
  }
}
// #endregion
</script>

<style scoped>
/* 适配移动端的微调 */
@media (max-width: 640px) {
  .login-container {
    padding: 1rem;
    align-items: flex-start;
    padding-top: 10vh;
  }
}

/* PrimeVue Message 动画 */
.p-message-enter-active,
.p-message-leave-active {
  transition: all 0.3s ease;
}
.p-message-enter-from,
.p-message-leave-to {
  opacity: 0;
  transform: translateY(-10px);
}
</style>
