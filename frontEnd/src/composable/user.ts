/**
 * 用户基础信息
 */
export interface UserInfo {
  id: number;
  username: string | null;
  permission: 'admin' | 'user';
  is_active: number;
  proxy_expire_ts: number;
  last_login_ts: number;
}

/**
 * 管理员获取的用户列表项
 */
export interface UserListItem {
  id: number;
  name: string;
  email: string;
  created_at: string;
}

/**
 * 新增/更新用户请求体
 */
export interface UserSaveRequest {
  name: string;
  email: string;
}

/**
 * 接口返回的通用成功标识
 */
export interface SuccessResponse {
  success: boolean;
}
