import client from '@/fetch/client';
import type { ServerStatus, ClientStatus } from '@/composable/status';

/**
 * 查看服务器运行状态概览
 * @returns 服务器状态
 */
export function getServerStatus(): Promise<ServerStatus> {
  return client.get('/status/server');
}

/**
 * 查看客户端请求上下文信息
 * @returns 客户端状态
 */
export function getClientStatus(): Promise<ClientStatus> {
  return client.get('/status/client');
}
