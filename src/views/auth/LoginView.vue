<script setup lang="ts">
import { ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useAuth } from '@/composables/useAuth'
import { useTheme } from '@/composables/useTheme'

const { t } = useI18n()
const { isLoggingIn, loginError, login } = useAuth()
const { isDark } = useTheme()

const username = ref('')
const password = ref('')

async function handleSubmit() {
  if (!username.value.trim() || !password.value.trim()) return
  await login({ username: username.value.trim(), password: password.value })
}
</script>

<template>
  <div
    class="min-h-screen flex items-center justify-center bg-[var(--bg-primary)] p-4 transition-colors duration-200"
  >
    <div
      class="w-full max-w-md bg-[var(--bg-elevated)] rounded-lg border border-[var(--border-light)] shadow-md p-8"
    >
      <!-- Logo / 品牌 -->
      <div class="text-center mb-8">
        <div class="flex items-center justify-center gap-2 mb-2">
          <i class="pi pi-bolt text-3xl text-brand-500" />
          <h1 class="text-3xl font-bold text-[var(--text-primary)]">
            {{ $t('auth.brandName') }}
          </h1>
        </div>
        <p class="text-sm text-[var(--text-muted)]">
          {{ $t('auth.brandSubtitle') }}
        </p>
      </div>

      <!-- 登录表单 -->
      <form class="space-y-5" @submit.prevent="handleSubmit">
        <!-- 用户名 -->
        <div>
          <label
            for="login-username"
            class="block text-sm font-medium text-[var(--text-primary)] mb-1.5"
          >
            {{ $t('auth.username') }}
          </label>
          <InputText
            id="login-username"
            v-model="username"
            :placeholder="$t('auth.username')"
            :disabled="isLoggingIn"
            class="w-full"
            autocomplete="username"
            autofocus
          />
        </div>

        <!-- 密码 -->
        <div>
          <label
            for="login-password"
            class="block text-sm font-medium text-[var(--text-primary)] mb-1.5"
          >
            {{ $t('auth.password') }}
          </label>
          <InputText
            id="login-password"
            type="password"
            v-model="password"
            :placeholder="$t('auth.password')"
            :disabled="isLoggingIn"
            :feedback="false"
            class="w-full"
            autocomplete="current-password"
            @keyup.enter="handleSubmit"
          />
        </div>

        <!-- 错误提示 -->
        <div
          v-if="loginError"
          class="flex items-center gap-2 p-3 rounded-md bg-[var(--status-error-bg)] text-[var(--status-error)] text-sm"
          role="alert"
        >
          <i class="pi pi-exclamation-circle" />
          <span>{{ loginError }}</span>
        </div>

        <!-- 登录按钮 -->
        <Button
          type="submit"
          :label="isLoggingIn ? $t('auth.loggingIn') : $t('auth.loginBtn')"
          :loading="isLoggingIn"
          :disabled="!username.trim() || !password.trim()"
          class="w-full bg-brand-500 hover:bg-brand-600 text-white border-none rounded-md"
          size="large"
        />
      </form>

      <!-- 底部 -->
      <div class="mt-6 text-center">
        <p class="text-xs text-[var(--text-muted)]">Hysteria Auth &copy; 2026</p>
      </div>
    </div>
  </div>
</template>
