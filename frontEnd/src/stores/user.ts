import { ref } from 'vue';
import { defineStore } from 'pinia';
import { getUsers, getUserById, createUser, updateUser, deleteUser } from '@/fetch/user';
import type { UserListItem, UserSaveRequest } from '@/composable/user';

/**
 * 用户管理相关的 Pinia Store (管理员专用)
 * 处理用户列表的获取、新增、更新和删除
 */
export const useUserStore = defineStore('user', () => {
  // --- 状态 (State) ---
  const users = ref<UserListItem[]>([]);
  const currentUser = ref<UserListItem | null>(null);
  const loading = ref(false);

  // --- 动作 (Actions) ---

  /**
   * 管理员获取用户列表
   */
  async function fetchUsers() {
    loading.value = true;
    try {
      users.value = await getUsers();
    } finally {
      loading.value = false;
    }
  }

  /**
   * 管理员根据 ID 获取用户详情
   * @param id 用户 ID
   */
  async function fetchUserById(id: number) {
    loading.value = true;
    try {
      currentUser.value = await getUserById(id);
      return currentUser.value;
    } finally {
      loading.value = false;
    }
  }

  /**
   * 管理员新增用户
   * @param data 用户数据 (name, email)
   */
  async function addUser(data: UserSaveRequest) {
    loading.value = true;
    try {
      const newUser = await createUser(data);
      users.value.push(newUser);
      return newUser;
    } finally {
      loading.value = false;
    }
  }

  /**
   * 管理员更新用户信息
   * @param id 用户 ID
   * @param data 更新的数据
   */
  async function editUser(id: number, data: UserSaveRequest) {
    loading.value = true;
    try {
      const updated = await updateUser(id, data);
      const index = users.value.findIndex(u => u.id === id);
      if (index !== -1) {
        users.value[index] = updated;
      }
      if (currentUser.value?.id === id) {
        currentUser.value = updated;
      }
      return updated;
    } finally {
      loading.value = false;
    }
  }

  /**
   * 管理员删除用户
   * @param id 用户 ID
   */
  async function removeUser(id: number) {
    loading.value = true;
    try {
      const result = await deleteUser(id);
      if (result.success) {
        users.value = users.value.filter(u => u.id !== id);
        if (currentUser.value?.id === id) {
          currentUser.value = null;
        }
      }
      return result;
    } finally {
      loading.value = false;
    }
  }

  return {
    users,
    currentUser,
    loading,
    fetchUsers,
    fetchUserById,
    addUser,
    editUser,
    removeUser,
  };
});
