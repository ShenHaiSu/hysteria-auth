/**
 * 代理配置项
 */
export interface ProxyConfigItem {
  server_group: string;
  ip_address: string;
  domain: string | null;
  salamander: string;
}

/**
 * 代理配置响应
 */
export interface ProxyConfigResponse {
  items: ProxyConfigItem[];
}
