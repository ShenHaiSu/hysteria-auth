import http from '@/api'
import type { PageResponse } from '@/api/types'
import type {
  UserDto,
  CreateUserRequest,
  UpdateUserRequest,
  UserFilters,
  UserTrafficStats,
} from '@/types/user.types'

export const userApi = {
  getList(params: UserFilters & { page: number; pageSize: number }) {
    return http.get<PageResponse<UserDto>>('/users', { params })
  },

  getById(id: number) {
    return http.get<UserDto>(`/users/${id}`)
  },

  create(data: CreateUserRequest) {
    return http.post<UserDto>('/users', data)
  },

  update(id: number, data: UpdateUserRequest) {
    return http.put<UserDto>(`/users/${id}`, data)
  },

  delete(id: number) {
    return http.delete(`/users/${id}`)
  },

  resetTraffic(id: number) {
    return http.post<{ message: string }>(`/users/${id}/reset-traffic`)
  },

  getTrafficStats(id: number, period: 'day' | 'week' | 'month' | 'all' = 'month') {
    return http.get<UserTrafficStats>(`/users/${id}/traffic-stats`, { params: { period } })
  },
}
