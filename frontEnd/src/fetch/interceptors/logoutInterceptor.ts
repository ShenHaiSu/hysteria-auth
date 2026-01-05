import type { AxiosInstance } from 'axios'

/**
 * 退出登录拦截器
 * 拦截 POST 请求且路径以 /logout 结尾的成功响应，清除本地认证凭证
 * @param instance Axios 实例
 */
export function setupLogoutInterceptor(instance: AxiosInstance): void {
  instance.interceptors.response.use(
    (response) => {
      const { config } = response

      // 检查请求方法是否为 POST
      const isPost = config.method?.toUpperCase() === 'POST'
      // 检查请求路径是否以 /logout 结尾 (考虑到 baseURL 可能的影响)
      const isLogoutPath = config.url?.endsWith('/logout') || config.url?.endsWith('/api/logout')

      if (isPost && isLogoutPath) {
        // 清除本地存储的认证凭证
        localStorage.removeItem('token')
        // 如果未来有更多存储在 localStorage 的用户信息，也可以在此清除
        // localStorage.removeItem('user');

        console.log('[Auth Interceptor] Logout successful, credentials cleared.')
      }

      return response
    },
    (error) => {
      return Promise.reject(error)
    },
  )
}
