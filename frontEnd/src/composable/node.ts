/**
 * 代理节点信息
 */
export interface NodeServer {
  id: number;
  ip_address: string;
  domain: string | null;
  server_group: string;
  salamander: string;
  idc_name: string | null;
  rent_ts: number;
  expire_ts: number;
  fee: number;
  proxy_port: string;
  server_port: number | null;
  note1: string | null;
  note2: string | null;
  note3: string | null;
  note4: string | null;
  is_active: 0 | 1;
  created_at: string;
  updated_at: string | null;
}

/**
 * 节点查询参数
 */
export interface NodeQueryParams {
  server_group?: string;
  ip_address?: string;
  domain?: string;
  is_active?: 0 | 1;
  expire_from?: number;
  expire_to?: number;
}

/**
 * 新增/更新节点请求体
 */
export interface NodeSaveRequest {
  ip_address: string;
  domain: string | null;
  server_group: string;
  salamander: string;
  idc_name: string | null;
  rent_ts: number;
  expire_ts: number;
  fee: number;
  proxy_port: string;
  server_port: number | null;
  note1: string | null;
  note2: string | null;
  note3: string | null;
  note4: string | null;
  is_active: 0 | 1;
}
