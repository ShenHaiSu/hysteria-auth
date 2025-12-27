/**
 * 服务器状态
 */
export interface ServerStatus {
  runtime: {
    bun: { version: string };
    node_compat: { platform: string };
  };
  os: {
    hostname: string;
    platform: string;
    uptime_sec: number;
  };
  cpu: {
    count: number;
    model: string;
    speed_mhz: number;
  };
  memory: {
    total_bytes: number;
    free_bytes: number;
  };
  network: {
    interfaces: Array<{
      iface: string;
      address: string;
      family: string;
    }>;
  };
  env: {
    keys: string[];
    safe: Record<string, string>;
  };
  time: {
    now_iso: string;
    tz: string;
  };
}

/**
 * 客户端请求上下文信息
 */
export interface ClientStatus {
  method: string;
  url: string;
  path: string;
  query: Record<string, any>;
  ip: string;
  headers: Record<string, string>;
  cookies: Record<string, string>;
  ua: string;
  origin: string;
  referer: string;
  forwarded: {
    x_forwarded_for: string;
    x_real_ip: string;
  };
  time: {
    now_iso: string;
  };
}
