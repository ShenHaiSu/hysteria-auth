import axios from 'axios'
import type { AxiosInstance, InternalAxiosRequestConfig, AxiosResponse, AxiosError } from 'axios'
import type { ApiError } from '@/api/types'

const http: AxiosInstance = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL,
  timeout: 30000,
  headers: { 'Content-Type': 'application/json' },
})

// 请求拦截器：自动注入 JWT Token
http.interceptors.request.use(async (config: InternalAxiosRequestConfig) => {
  // 延迟调用 useAuthStore() 避免 Pinia 循环依赖
  // 动态 import 在运行时才执行，此时 Pinia 已初始化
  const { useAuthStore } = await import('@/stores/auth.store')
  const authStore = useAuthStore()
  if (authStore.token) {
    config.headers.Authorization = `Bearer ${authStore.token}`
  }
  return config
})

// 响应拦截器：统一错误处理
http.interceptors.response.use(
  (response: AxiosResponse) => response,
  async (error: AxiosError<ApiError>) => {
    const errorData = error.response?.data?.error

    switch (errorData?.code) {
      case 'token_expired':
      case 'token_invalid':
      case 'unauthorized': {
        const { useAuthStore } = await import('@/stores/auth.store')
        const authStore = useAuthStore()
        authStore.logout()
        break
      }
      case 'forbidden':
      case 'rate_limited':
      case 'internal_error':
        // 由调用方处理
        break
    }

    return Promise.reject(error)
  },
)

export default http
