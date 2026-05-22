import http from '@/api'
import type { PageResponse } from '@/api/types'
import type {
  AdminDto,
  CreateAdminRequest,
  UpdateAdminRequest,
  AdminFilters,
} from '@/types/admin.types'

export const adminApi = {
  getList(params: AdminFilters & { page: number; pageSize: number }) {
    return http.get<PageResponse<AdminDto>>('/admin/admins', { params })
  },

  create(data: CreateAdminRequest) {
    return http.post<AdminDto>('/admin/admins', data)
  },

  update(id: number, data: UpdateAdminRequest) {
    return http.put<AdminDto>(`/admin/admins/${id}`, data)
  },
}
