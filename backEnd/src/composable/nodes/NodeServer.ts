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
  proxy_port: string;
  server_port: number | null;
  note1: string | null;
  note2: string | null;
  note3: string | null;
  note4: string | null;
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
  proxy_port: string;
}
