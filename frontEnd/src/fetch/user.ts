import client from '@/fetch/client';
import type { UserInfo, UserListItem, UserSaveRequest, SuccessResponse } from '@/composable/user';

/**
 * 获取当前登录用户的基础信息
 * @returns 用户基础信息
 */
export function getMe(): Promise<UserInfo> {
  return client.get('/users/me');
}

/**
 * 管理员查询用户列表
 * @returns 用户列表
 */
export function getUsers(): Promise<UserListItem[]> {
  return client.get('/users');
}

/**
 * 管理员获取指定用户详情
 * @param id 用户ID
 * @returns 用户详情
 */
export function getUserById(id: number): Promise<UserListItem> {
  return client.get(`/users/${id}`);
}

/**
 * 管理员新增用户
 * @param data 用户数据
 * @returns 创建成功的用户信息
 */
export function createUser(data: UserSaveRequest): Promise<UserListItem> {
  return client.post('/users', data);
}

/**
 * 管理员更新用户
 * @param id 用户ID
 * @param data 更新的用户数据
 * @returns 更新后的用户信息
 */
export function updateUser(id: number, data: UserSaveRequest): Promise<UserListItem> {
  return client.put(`/users/${id}`, data);
}

/**
 * 管理员删除用户
 * @param id 用户ID
 * @returns 成功标识
 */
export function deleteUser(id: number): Promise<SuccessResponse> {
  return client.delete(`/users/${id}`);
}
