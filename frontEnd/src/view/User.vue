<!-- 用户管理界面 管理员专用 -->
<template>
  <div class="user-management p-4">
    <!-- 1. 搜索过滤以及操作栏 -->
    <UserToolbar
      :initial-search="store.search"
      @search="store.onSearch"
      @add="openNew"
    />

    <!-- 2. 数据列表展示的 datatable 栏 -->
    <UserTable
      :users="store.userVOs"
      :total-records="store.totalRecords"
      :rows="store.rows"
      :first="store.first"
      :loading="store.loading"
      @page="store.onPage"
      @edit="openEdit"
      @delete="confirmDelete"
    />

    <!-- 3. 隐藏着的 Dialog 元素 -->
    <UserDialog
      v-model:show="showDialog"
      :user="selectedUser"
      :saving="saving"
      @save="handleSave"
    />

    <!-- 辅助组件：全局确认对话框和通知 -->
    <ConfirmDialog />
    <Toast />
  </div>
</template>

<script setup lang="ts">
import { onMounted } from 'vue';
import UserToolbar from '@/components/User/UserToolbar.vue';
import UserTable from '@/components/User/UserTable.vue';
import UserDialog from '@/components/User/UserDialog.vue';
import { useUserLogic } from '@/composable/User/useUserLogic';
import ConfirmDialog from 'primevue/confirmdialog';
import Toast from 'primevue/toast';

// #region 逻辑集成
const {
  store,
  showDialog,
  selectedUser,
  saving,
  openNew,
  openEdit,
  handleSave,
  confirmDelete,
} = useUserLogic();

/**
 * 页面加载时获取初始数据
 */
onMounted(() => {
  store.fetchUsers();
});
// #endregion
</script>

<style scoped>
.user-management {
  max-width: 1200px;
  margin: 0 auto;
}
</style>
