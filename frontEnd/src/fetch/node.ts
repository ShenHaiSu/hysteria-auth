import client from '@/fetch/client';
import type { NodeServer, NodeQueryParams, NodeSaveRequest } from '@/composable/node';
import type { SuccessResponse } from '@/composable/user';

/**
 * 查询代理节点列表
 * @param params 筛选条件
 * @returns 节点列表
 */
export function getNodes(params?: NodeQueryParams): Promise<NodeServer[]> {
  return client.get('/nodes', { params });
}

/**
 * 管理员新增节点记录
 * @param data 节点数据
 * @returns 创建成功的节点信息
 */
export function createNode(data: NodeSaveRequest): Promise<NodeServer> {
  return client.post('/nodes', data);
}

/**
 * 管理员更新节点记录
 * @param id 节点ID
 * @param data 更新的节点数据
 * @returns 更新后的节点信息
 */
export function updateNode(id: number, data: Partial<NodeSaveRequest>): Promise<NodeServer> {
  return client.put(`/nodes/${id}`, data);
}

/**
 * 管理员删除节点
 * @param id 节点ID
 * @returns 成功标识
 */
export function deleteNode(id: number): Promise<SuccessResponse> {
  return client.delete(`/nodes/${id}`);
}
