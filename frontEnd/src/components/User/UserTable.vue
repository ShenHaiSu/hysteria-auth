<!-- 用户数据列表：展示用户信息并处理分页 -->
<template>
  <div class="card">
    <DataTable
      :value="users"
      :lazy="true"
      :paginator="true"
      :rows="rows"
      :first="first"
      :totalRecords="totalRecords"
      :loading="loading"
      @page="onPage"
      dataKey="id"
      responsiveLayout="scroll"
      stripedRows
      class="p-datatable-sm"
    >
      <template #empty> 未找到用户信息 </template>
      <template #loading> 正在加载用户数据，请稍候... </template>

      <Column field="id" header="ID" sortable style="width: 5rem"></Column>
      <Column field="name" header="姓名" sortable></Column>
      <Column field="email" header="邮箱" sortable></Column>
      <Column field="status_label" header="状态">
        <template #body="slotProps">
          <Tag :value="slotProps.data.status_label" severity="success" />
        </template>
      </Column>
      <Column field="formatted_created_at" header="创建时间" sortable></Column>
      
      <Column header="操作" style="min-width: 8rem">
        <template #body="slotProps">
          <div class="flex gap-2">
            <Button
              icon="pi pi-pencil"
              outlined
              rounded
              severity="warn"
              @click="$emit('edit', slotProps.data)"
              v-tooltip.top="'编辑'"
            />
            <Button
              icon="pi pi-trash"
              outlined
              rounded
              severity="danger"
              @click="$emit('delete', slotProps.data)"
              v-tooltip.top="'删除'"
            />
          </div>
        </template>
      </Column>
    </DataTable>
  </div>
</template>

<script setup lang="ts">
import DataTable from 'primevue/datatable';
import Column from 'primevue/column';
import Button from 'primevue/button';
import Tag from 'primevue/tag';
import type { UserVO } from '@/composable/user';

// #region 定义属性和事件
defineProps<{
  users: UserVO[];
  totalRecords: number;
  rows: number;
  first: number;
  loading: boolean;
}>();

const emit = defineEmits<{
  (e: 'page', event: any): void;
  (e: 'edit', user: UserVO): void;
  (e: 'delete', user: UserVO): void;
}>();
// #endregion

// #region 事件转发
/**
 * 分页事件转发
 */
const onPage = (event: any) => {
  emit('page', event);
};
// #endregion
</script>
