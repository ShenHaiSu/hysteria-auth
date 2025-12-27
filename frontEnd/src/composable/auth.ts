/**
 * 登录请求体
 */
export interface LoginRequest {
  username: string;
  login_password_md5: string;
}

/**
 * 登录成功响应的用户信息
 */
export interface AuthUser {
  id: number;
  username: string | null;
  permission: 'admin' | 'user';
  is_active: number;
  proxy_expire_ts: number;
  last_login_ts: number;
}

/**
 * 登录响应
 */
export interface LoginResponse {
  token: string;
  user: AuthUser;
}

/**
 * 双向校验请求体
 */
export interface AuthCheckRequest {
  addr: string;
  auth: string;
  tx: number;
}

/**
 * 双向校验响应
 */
export interface AuthCheckResponse {
  ok: boolean;
  id: string;
}
