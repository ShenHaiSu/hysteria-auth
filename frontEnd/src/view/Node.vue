<!-- 子代理节点管理界面 管理员专用 -->
<template>
  <div class="p-4 flex flex-col gap-4">
    <!-- 顶部标题 -->
    <div class="flex justify-between items-center mb-2">
      <h1 class="text-2xl font-bold">节点管理</h1>
    </div>

    <!-- 筛选区域 -->
    <NodeFilter :filters="filters" @search="loadNodes" @reset="resetFilters" />

    <!-- 列表展示 -->
    <NodeList
      :nodes="nodes"
      :loading="loading"
      @add="openAdd"
      @edit="openEdit"
      @delete="confirmDelete"
      @stop="handleStop"
      @copy="openCopy"
    />

    <!-- 编辑/新增对话框 -->
    <NodeDialog
      v-model:visible="visible"
      :mode="mode"
      :form="form"
      :loading="dialogLoading"
      @save="handleSave"
      @cancel="close"
    />

    <!-- 删除确认对话框 -->
    <ConfirmDialog></ConfirmDialog>

    <!-- 消息提示 -->
    <Toast />
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { useNodeTable } from '@/composable/Node/useNodeTable'
import { useNodeDialog } from '@/composable/Node/useNodeDialog'
import { useNodeStore } from '@/stores/node'
import NodeFilter from '@/components/Node/NodeFilter.vue'
import NodeList from '@/components/Node/NodeList.vue'
import NodeDialog from '@/components/Node/NodeDialog.vue'
import ConfirmDialog from 'primevue/confirmdialog'
import Toast from 'primevue/toast'
import { useConfirm } from 'primevue/useconfirm'
import { useToast } from 'primevue/usetoast'
import type { NodeServer, NodeSaveRequest } from '@/composable/node'

// 1. 初始化列表逻辑
const { nodes, loading, filters, loadNodes, resetFilters } = useNodeTable()

// 2. 初始化对话框逻辑
const {
  visible,
  loading: dialogLoading,
  mode,
  form,
  currentNode,
  openAdd,
  openEdit,
  openCopy,
  close,
} = useNodeDialog()

const nodeStore = useNodeStore()
const confirm = useConfirm()
const toast = useToast()

/**
 * 处理停用操作
 * @param node 选中的节点
 */
async function handleStop(node: NodeServer) {
  try {
    await nodeStore.refreshNode(node.id, { is_active: 0 })
    toast.add({
      severity: 'success',
      summary: '成功',
      detail: `节点 ${node.ip_address} 已停用`,
      life: 3000,
    })
  } catch (error: any) {
    toast.add({
      severity: 'error',
      summary: '错误',
      detail: error.message || '停用失败',
      life: 5000,
    })
  }
}

/**
 * 处理保存操作
 * @param data 准备保存的节点数据
 */
async function handleSave(data: NodeSaveRequest) {
  dialogLoading.value = true
  try {
    if (mode.value === 'add') {
      await nodeStore.addNode(data as any)
      toast.add({ severity: 'success', summary: '成功', detail: '节点已新增', life: 3000 })
    } else if (currentNode.value) {
      await nodeStore.refreshNode(currentNode.value.id, data)
      toast.add({ severity: 'success', summary: '成功', detail: '节点已更新', life: 3000 })
    }
    close()
    loadNodes() // 重新加载列表以同步可能的组内属性变化
  } catch (error: any) {
    toast.add({
      severity: 'error',
      summary: '错误',
      detail: error.message || '操作失败',
      life: 5000,
    })
  } finally {
    dialogLoading.value = false
  }
}

/**
 * 确认删除节点
 * @param node 选中的节点
 */
function confirmDelete(node: NodeServer) {
  confirm.require({
    message: `确定要删除节点 "${node.ip_address}" 吗？此操作不可撤销。`,
    header: '删除确认',
    icon: 'pi pi-exclamation-triangle',
    rejectLabel: '取消',
    acceptLabel: '确定',
    rejectProps: {
      label: '取消',
      severity: 'secondary',
      outlined: true,
    },
    acceptProps: {
      label: '确定',
      severity: 'danger',
    },
    accept: async () => {
      try {
        await nodeStore.removeNode(node.id)
        toast.add({ severity: 'success', summary: '成功', detail: '节点已删除', life: 3000 })
        loadNodes()
      } catch (error: any) {
        toast.add({
          severity: 'error',
          summary: '错误',
          detail: error.message || '删除失败',
          life: 5000,
        })
      }
    },
  })
}
</script>

<style scoped>
/* 可以在此处添加视图特有的样式 */
</style>
