import { ref } from 'vue';
import { defineStore } from 'pinia';
import { getNodes, createNode, updateNode, deleteNode } from '@/fetch/node';
import { getProxyConfig } from '@/fetch/proxy';
import type { NodeServer, NodeQueryParams, NodeSaveRequest } from '@/composable/node';
import type { ProxyConfigItem } from '@/composable/proxy';

/**
 * 节点管理相关的 Pinia Store
 * 处理节点列表、节点增删改查以及代理配置下发
 */
export const useNodeStore = defineStore('node', () => {
  // --- 状态 (State) ---
  const nodes = ref<NodeServer[]>([]);
  const proxyConfigs = ref<ProxyConfigItem[]>([]);
  const loading = ref(false);

  // --- 动作 (Actions) ---

  /**
   * 获取节点列表
   * @param params 可选的查询参数（如 server_group, is_active 等）
   */
  async function fetchNodes(params?: NodeQueryParams) {
    loading.value = true;
    try {
      nodes.value = await getNodes(params);
    } finally {
      loading.value = false;
    }
  }

  /**
   * 获取当前用户可用的代理配置
   */
  async function fetchProxyConfigs() {
    loading.value = true;
    try {
      const response = await getProxyConfig();
      proxyConfigs.value = response.items;
    } finally {
      loading.value = false;
    }
  }

  /**
   * 新增节点
   * @param data 节点信息
   */
  async function addNode(data: NodeSaveRequest) {
    loading.value = true;
    try {
      const newNode = await createNode(data);
      nodes.value.push(newNode);
      return newNode;
    } finally {
      loading.value = false;
    }
  }

  /**
   * 更新节点信息
   * @param id 节点 ID
   * @param data 需要更新的字段
   */
  async function refreshNode(id: number, data: Partial<NodeSaveRequest>) {
    loading.value = true;
    try {
      const updated = await updateNode(id, data);
      const index = nodes.value.findIndex(n => n.id === id);
      if (index !== -1) {
        nodes.value[index] = updated;
      }
      return updated;
    } finally {
      loading.value = false;
    }
  }

  /**
   * 删除节点
   * @param id 节点 ID
   */
  async function removeNode(id: number) {
    loading.value = true;
    try {
      const result = await deleteNode(id);
      if (result.success) {
        nodes.value = nodes.value.filter(n => n.id !== id);
      }
      return result;
    } finally {
      loading.value = false;
    }
  }

  return {
    nodes,
    proxyConfigs,
    loading,
    fetchNodes,
    fetchProxyConfigs,
    addNode,
    refreshNode,
    removeNode,
  };
});
