import client from '@/fetch/client'
import type {
  LoginRequest,
  LoginResponse,
  AuthCheckRequest,
  AuthCheckResponse,
  AuthUser,
} from '@/composable/auth'

// #region 认证相关接口
/**
 * 用户登录并获取会话令牌与基础信息
 * @param data 登录请求数据
 * @returns 登录响应数据
 */
export function login(data: LoginRequest): Promise<LoginResponse> {
  return client.post('/login', data)
}

/**
 * 获取当前登录用户的基础信息
 * @returns 用户基础信息
 */
export function getMe(): Promise<AuthUser> {
  return client.get('/users/me')
}

/**
 * 用户退出登录
 * @returns 退出结果
 */
export function logout(): Promise<{ ok: boolean }> {
  return client.post('/logout')
}
// #endregion

// #region 代理节点相关接口
/**
 * 代理节点与用户代理密码双向校验用于接入
 * @param data 校验请求数据
 * @returns 校验响应数据
 */
export function authCheck(data: AuthCheckRequest): Promise<AuthCheckResponse> {
  return client.post('/auth', data)
}
// #endregion
