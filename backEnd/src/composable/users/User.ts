export interface User {
  id: number;
  username: string;
  email: string;
  login_password_md5: string;
  proxy_password: string;
  permission: "admin" | "user";
  is_active: number;
  proxy_expire_ts: number | null;
  register_ts: number;
  last_login_ts: number | null;
  register_ip: string | null;
  last_login_ip: string | null;
  created_at: string;
  updated_at: string | null;
}
