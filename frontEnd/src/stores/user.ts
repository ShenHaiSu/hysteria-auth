import { ref, computed } from 'vue'
import { defineStore } from 'pinia'
import { getUsers, getUserById, createUser, updateUser, deleteUser } from '@/fetch/user'
import type { UserListItem, UserSaveRequest, UserVO } from '@/composable/user'

/**
 * 用户管理相关的 Pinia Store (管理员专用)
 * 处理用户列表的获取、新增、更新和删除
 */
export const useUserStore = defineStore('user', () => {
  // #region 状态 (State)
  const users = ref<UserListItem[]>([])
  const currentUser = ref<UserListItem | null>(null)
  const loading = ref(false)

  // 过滤状态
  const search = ref('')
  // #endregion

  // #region 计算属性 (Getters)
  /**
   * 将 DTO 转换为 VO，并根据搜索词进行前端过滤
   */
  const userVOs = computed<UserVO[]>(() => {
    let filteredUsers = users.value

    if (search.value) {
      const s = search.value.toLowerCase()
      filteredUsers = users.value.filter(
        (user) => user.username.toLowerCase().includes(s) || user.email.toLowerCase().includes(s),
      )
    }

    return filteredUsers.map((user) => ({
      ...user,
      formatted_created_at: user.created_at ? new Date(user.created_at).toLocaleString() : '-',
      formatted_updated_at: user.updated_at ? new Date(user.updated_at).toLocaleString() : '-',
      formatted_proxy_expire_at:
        user.proxy_expire_ts && user.proxy_expire_ts > 0
          ? new Date(user.proxy_expire_ts * 1000).toLocaleString()
          : '永久',
      formatted_last_login_at:
        user.last_login_ts && user.last_login_ts > 0
          ? new Date(user.last_login_ts * 1000).toLocaleString()
          : '从未登录',
      formatted_register_at:
        user.register_ts && user.register_ts > 0
          ? new Date(user.register_ts * 1000).toLocaleString()
          : '-',
      status_label: user.is_active ? '正常' : '禁用',
    }))
  })
  // #endregion

  // #region 动作 (Actions)
  /**
   * 管理员获取用户列表
   */
  async function fetchUsers() {
    loading.value = true
    try {
      const response = await getUsers()
      users.value = response
    } finally {
      loading.value = false
    }
  }

  /**
   * 处理搜索
   */
  function onSearch(query: string) {
    search.value = query
  }

  /**
   * 管理员根据 ID 获取用户详情
   * @param id 用户 ID
   */
  async function fetchUserById(id: number) {
    loading.value = true
    try {
      currentUser.value = await getUserById(id)
      return currentUser.value
    } finally {
      loading.value = false
    }
  }

  /**
   * 管理员新增用户
   * @param data 用户数据 (name, email)
   */
  async function addUser(data: UserSaveRequest) {
    loading.value = true
    try {
      const newUser = await createUser(data)
      users.value.push(newUser)
      return newUser
    } finally {
      loading.value = false
    }
  }

  /**
   * 管理员更新用户信息
   * @param id 用户 ID
   * @param data 更新的数据
   */
  async function editUser(id: number, data: UserSaveRequest) {
    loading.value = true
    try {
      const updated = await updateUser(id, data)
      const index = users.value.findIndex((u) => u.id === id)
      if (index !== -1) {
        users.value[index] = updated
      }
      if (currentUser.value?.id === id) {
        currentUser.value = updated
      }
      return updated
    } finally {
      loading.value = false
    }
  }

  /**
   * 管理员删除用户
   * @param id 用户 ID
   */
  async function removeUser(id: number) {
    loading.value = true
    try {
      const result = await deleteUser(id)
      if (result.success) {
        users.value = users.value.filter((u) => u.id !== id)
        if (currentUser.value?.id === id) {
          currentUser.value = null
        }
      }
      return result
    } finally {
      loading.value = false
    }
  }

  return {
    users,
    userVOs,
    currentUser,
    loading,
    search,
    fetchUsers,
    fetchUserById,
    addUser,
    editUser,
    removeUser,
    onSearch,
  }
})
