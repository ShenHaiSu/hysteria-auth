export interface NodeServer {
  id: number;
  ip_address: string;
  domain: string | null;
  server_group: string;
  salamander: string;
  idc_name: string | null;
  rent_ts: number | null;
  expire_ts: number | null;
  fee: number;
  is_active: number;
  created_at: string;
  updated_at: string | null;
}

export interface NodeFilters {
  server_group?: string;
  ip_address?: string;
  domain?: string;
  is_active?: number;
  expire_from?: number;
  expire_to?: number;
}

export interface ProxyConfigItem {
  server_group: string;
  ip_address: string;
  domain: string | null;
  salamander: string;
}
