export interface AuthRequest {
  addr: string;
  auth: string;
  tx: number;
}

export interface AuthResponse {
  ok: boolean;
  id: string;
}

export interface ActiveUser {
  id: number;
  username: string | null;
}

export interface LoginRequest {
  username: string;
  login_password_md5: string;
}

export interface AuthUserInfo {
  id: number;
  username: string | null;
  permission: "admin" | "user";
  is_active: number;
  proxy_expire_ts: number | null;
  last_login_ts: number | null;
}

export interface LoginResponse {
  token: string;
  user: AuthUserInfo;
}
