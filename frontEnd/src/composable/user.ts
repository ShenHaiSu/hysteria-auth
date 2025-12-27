/**
 * 用户基础信息
 */
export interface UserInfo {
  id: number
  username: string | null
  permission: 'admin' | 'user'
  is_active: number
  proxy_expire_ts: number
  last_login_ts: number
}

/**
 * 管理员获取的用户列表项 (DTO)
 */
export interface UserListItem {
  id: number
  name: string
  email: string
  created_at: string
}

/**
 * 用户视图对象 (VO)
 */
export interface UserVO extends UserListItem {
  formatted_created_at: string
  status_label: string
}

/**
 * 分页和过滤查询参数
 */
export interface UserQuery {
  page: number
  limit: number
  search?: string
  [key: string]: any
}

/**
 * 分页返回结构
 */
export interface UserListResponse {
  items: UserListItem[]
  total: number
}

/**
 * 新增/更新用户请求体
 */
export interface UserSaveRequest {
  name: string
  email: string
}

/**
 * 接口返回的通用成功标识
 */
export interface SuccessResponse {
  success: boolean
}
