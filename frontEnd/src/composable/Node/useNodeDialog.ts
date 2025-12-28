import { ref } from 'vue';
import type { NodeServer, NodeSaveRequest } from '@/composable/node';

/**
 * 节点编辑对话框逻辑处理
 * @returns 对话框状态和操作函数
 */
export function useNodeDialog() {
  const visible = ref(false);
  const loading = ref(false);
  const mode = ref<'add' | 'edit'>('add');
  const currentNode = ref<NodeServer | null>(null);
  
  // 表单初始值
  const initialForm: NodeSaveRequest = {
    ip_address: '',
    domain: null,
    server_group: 'default',
    salamander: '',
    idc_name: null,
    rent_ts: Math.floor(Date.now() / 1000),
    expire_ts: 0,
    fee: 0,
    proxy_port: '20000-30000',
    server_port: null,
    note1: null,
    note2: null,
    note3: null,
    note4: null,
    is_active: 1
  };

  const form = ref<NodeSaveRequest>({ ...initialForm });

  /**
   * 打开新增对话框
   */
  function openAdd() {
    mode.value = 'add';
    currentNode.value = null;
    form.value = { ...initialForm };
    visible.value = true;
  }

  /**
   * 打开编辑对话框
   * @param node 选中的节点数据
   */
  function openEdit(node: NodeServer) {
    mode.value = 'edit';
    currentNode.value = node;
    form.value = {
      ip_address: node.ip_address,
      domain: node.domain,
      server_group: node.server_group,
      salamander: node.salamander,
      idc_name: node.idc_name,
      rent_ts: node.rent_ts,
      expire_ts: node.expire_ts,
      fee: node.fee,
      proxy_port: node.proxy_port,
      server_port: node.server_port,
      note1: node.note1,
      note2: node.note2,
      note3: node.note3,
      note4: node.note4,
      is_active: node.is_active
    };
    visible.value = true;
  }

  /**
   * 打开复用对话框 (基于现有节点新增)
   * @param node 被复用的节点数据
   */
  function openCopy(node: NodeServer) {
    mode.value = 'add';
    currentNode.value = null; // 复用是新增，不关联旧 ID
    form.value = {
      ...initialForm,
      server_group: node.server_group,
      salamander: node.salamander,
      idc_name: node.idc_name,
      fee: node.fee,
      proxy_port: node.proxy_port,
      server_port: node.server_port,
      rent_ts: node.rent_ts,
      expire_ts: node.expire_ts,
      is_active: 1 // 默认开启
    };
    visible.value = true;
  }

  /**
   * 关闭对话框
   */
  function close() {
    visible.value = false;
  }

  return {
    visible,
    loading,
    mode,
    form,
    currentNode,
    openAdd,
    openEdit,
    openCopy,
    close
  };
}
