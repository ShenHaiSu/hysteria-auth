import { ref, computed } from 'vue'
import { defineStore } from 'pinia'
import { getUsers, getUserById, createUser, updateUser, deleteUser } from '@/fetch/user'
import type { UserListItem, UserSaveRequest, UserVO, UserQuery } from '@/composable/user'

/**
 * 用户管理相关的 Pinia Store (管理员专用)
 * 处理用户列表的获取、新增、更新和删除
 */
export const useUserStore = defineStore('user', () => {
  // #region 状态 (State)
  const users = ref<UserListItem[]>([])
  const totalRecords = ref(0)
  const currentUser = ref<UserListItem | null>(null)
  const loading = ref(false)

  // 分页与过滤状态
  const first = ref(0) // PrimeVue DataTable 起始行索引
  const rows = ref(10) // 每页行数
  const page = computed(() => Math.floor(first.value / rows.value) + 1)
  const search = ref('')
  // #endregion

  // #region 计算属性 (Getters)
  /**
   * 将 DTO 转换为 VO
   */
  const userVOs = computed<UserVO[]>(() => {
    return users.value.map((user) => ({
      ...user,
      formatted_created_at: new Date(user.created_at).toLocaleString(),
      status_label: '正常', // 这里可以根据业务逻辑扩展
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
      const query: UserQuery = {
        page: page.value,
        limit: rows.value,
        search: search.value || undefined,
      }
      const response = await getUsers(query)
      console.log(response)

      users.value = response.items
      totalRecords.value = response.total
    } finally {
      loading.value = false
    }
  }

  /**
   * 处理分页点击
   */
  function onPage(event: { first: number; rows: number }) {
    first.value = event.first
    rows.value = event.rows
    fetchUsers()
  }

  /**
   * 处理搜索
   */
  function onSearch(query: string) {
    search.value = query
    first.value = 0 // 重置到第一页
    fetchUsers()
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
    totalRecords,
    currentUser,
    loading,
    first,
    rows,
    search,
    fetchUsers,
    fetchUserById,
    addUser,
    editUser,
    removeUser,
    onPage,
    onSearch,
  }
})
