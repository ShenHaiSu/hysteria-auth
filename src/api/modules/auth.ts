import http from '@/api'
import type { LoginRequest, LoginResponse } from '@/types/auth.types'

export const authApi = {
  login(data: LoginRequest) {
    return http.post<LoginResponse>('/admin/login', data)
  },
}
