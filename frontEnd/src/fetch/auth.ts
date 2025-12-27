import client from '@/fetch/client';
import type { LoginRequest, LoginResponse, AuthCheckRequest, AuthCheckResponse } from '@/composable/auth';

/**
 * 用户登录并获取会话令牌与基础信息
 * @param data 登录请求数据
 * @returns 登录响应数据
 */
export function login(data: LoginRequest): Promise<LoginResponse> {
  return client.post('/login', data);
}

/**
 * 代理节点与用户代理密码双向校验用于接入
 * @param data 校验请求数据
 * @returns 校验响应数据
 */
export function authCheck(data: AuthCheckRequest): Promise<AuthCheckResponse> {
  return client.post('/auth', data);
}
