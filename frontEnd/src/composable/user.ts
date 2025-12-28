/**
 * 用户基础信息
 */
export interface UserInfo {
  id: number
  username: string | null
  permission: 'admin' | 'user'
  is_active: number
  proxy_expire_ts: number | null
  last_login_ts: number | null
}

/**
 * 管理员获取的用户列表项 (DTO)
 */
export interface UserListItem {
  id: number
  username: string
  email: string
  permission: 'admin' | 'user'
  is_active: number
  login_password_md5: string
  proxy_password: string
  proxy_expire_ts: number
  last_login_ip: string | null
  last_login_ts: number
  register_ip: string | null
  register_ts: number
  created_at: string
  updated_at: string
}

/**
 * 用户视图对象 (VO)
 */
export interface UserVO extends UserListItem {
  formatted_created_at: string
  formatted_updated_at: string
  formatted_proxy_expire_at: string
  formatted_last_login_at: string
  formatted_register_at: string
  status_label: string
}

/**
 * 新增/更新用户请求体
 */
export interface UserSaveRequest {
  username: string
  email: string
  login_password_md5?: string
  proxy_password?: string
  permission?: 'admin' | 'user'
  is_active?: number
  proxy_expire_ts?: number | null
}

/**
 * 接口返回的通用成功标识
 */
export interface SuccessResponse {
  success: boolean
}
