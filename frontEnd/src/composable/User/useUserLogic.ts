import { ref } from 'vue';
import { useUserStore } from '@/stores/user';
import type { UserVO, UserSaveRequest } from '@/composable/user';
import { useConfirm } from "primevue/useconfirm";
import { useToast } from "primevue/usetoast";

/**
 * 用户管理界面的核心逻辑 Composable
 */
export function useUserLogic() {
  const store = useUserStore();
  const confirm = useConfirm();
  const toast = useToast();

  // #region 状态变量
  const showDialog = ref(false);
  const selectedUser = ref<UserVO | null>(null);
  const saving = ref(false);
  // #endregion

  // #region 逻辑方法
  /**
   * 打开新增用户弹窗
   */
  const openNew = () => {
    selectedUser.value = null;
    showDialog.value = true;
  };

  /**
   * 打开编辑用户弹窗
   * @param user 要编辑的用户 VO
   */
  const openEdit = (user: UserVO) => {
    selectedUser.value = user;
    showDialog.value = true;
  };

  /**
   * 保存用户（新增或更新）
   * @param data 用户表单数据
   */
  const handleSave = async (data: UserSaveRequest) => {
    saving.value = true;
    try {
      if (selectedUser.value) {
        await store.editUser(selectedUser.value.id, data);
        toast.add({ severity: 'success', summary: '成功', detail: '用户信息已更新', life: 1000 });
      } else {
        await store.addUser(data);
        toast.add({ severity: 'success', summary: '成功', detail: '用户已新增', life: 1000 });
      }
      showDialog.value = false;
    } catch (error) {
      toast.add({ severity: 'error', summary: '错误', detail: '操作失败', life: 1000 });
    } finally {
      saving.value = false;
    }
  };

  /**
   * 删除用户确认
   * @param user 要删除的用户 VO
   */
  const confirmDelete = (user: UserVO) => {
    confirm.require({
      message: `确定要删除用户 "${user.username}" 吗？此操作不可撤销。`,
      header: '删除确认',
      icon: 'pi pi-exclamation-triangle',
      rejectLabel: '取消',
      acceptLabel: '确定',
      rejectProps: {
        label: '取消',
        severity: 'secondary',
        outlined: true
      },
      acceptProps: {
        label: '删除',
        severity: 'danger'
      },
      accept: async () => {
        try {
          await store.removeUser(user.id);
          toast.add({ severity: 'success', summary: '成功', detail: '用户已删除', life: 3000 });
        } catch (error) {
          toast.add({ severity: 'error', summary: '错误', detail: '删除失败', life: 3000 });
        }
      }
    });
  };
  // #endregion

  return {
    store,
    showDialog,
    selectedUser,
    saving,
    openNew,
    openEdit,
    handleSave,
    confirmDelete,
  };
}
