import http from '@/api'
import type { PageResponse } from '@/api/types'
import type {
  NodeDto,
  NodeDetail,
  NodeStatusRecord,
  PreRegisterNodeRequest,
  PreRegisterNodeResponse,
} from '@/types/node.types'

export const nodeApi = {
  getList(params: { page: number; pageSize: number; search?: string; isActive?: boolean }) {
    return http.get<PageResponse<NodeDto>>('/nodes', { params })
  },

  getById(id: string) {
    return http.get<NodeDetail>(`/nodes/${id}`)
  },

  getStatusHistory(
    id: string,
    params: { page: number; pageSize: number; startDate?: string; endDate?: string },
  ) {
    return http.get<PageResponse<NodeStatusRecord>>(`/nodes/${id}/status-history`, { params })
  },

  preRegister(data: PreRegisterNodeRequest) {
    return http.post<PreRegisterNodeResponse>('/admin/nodes/pre-register', data)
  },

  rotateSecret(id: string) {
    return http.post<{ message: string }>(`/admin/nodes/${id}/rotate-secret`)
  },

  kickUser(nodeId: string, userId: number) {
    return http.post<{ message: string }>(`/admin/nodes/${nodeId}/kick-user`, { userId })
  },
}
