import client from '@/fetch/client';
import type { ProxyConfigResponse } from '@/composable/proxy';

/**
 * 下发当前用户可用的代理节点配置
 * @returns 代理配置响应
 */
export function getProxyConfig(): Promise<ProxyConfigResponse> {
  return client.get('/proxy/config');
}
