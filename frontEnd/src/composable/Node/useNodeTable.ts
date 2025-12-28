import { ref, onMounted } from 'vue';
import { storeToRefs } from 'pinia';
import { useNodeStore } from '@/stores/node';
import type { NodeQueryParams } from '@/composable/node';

/**
 * 节点列表表格逻辑处理
 * @returns 列表数据、加载状态和查询操作
 */
export function useNodeTable() {
  const nodeStore = useNodeStore();
  const { nodes, loading } = storeToRefs(nodeStore);
  
  const filters = ref<NodeQueryParams>({
    server_group: '',
    ip_address: '',
    domain: '',
    is_active: undefined,
    expire_from: undefined,
    expire_to: undefined
  });

  /**
   * 加载节点数据
   */
  async function loadNodes() {
    const params: NodeQueryParams = {};
    if (filters.value.server_group) params.server_group = filters.value.server_group;
    if (filters.value.ip_address) params.ip_address = filters.value.ip_address;
    if (filters.value.domain) params.domain = filters.value.domain;
    if (filters.value.is_active !== undefined) params.is_active = filters.value.is_active;
    if (filters.value.expire_from) params.expire_from = filters.value.expire_from;
    if (filters.value.expire_to) params.expire_to = filters.value.expire_to;
    
    await nodeStore.fetchNodes(params);
  }

  /**
   * 重置查询条件
   */
  function resetFilters() {
    filters.value = {
      server_group: '',
      ip_address: '',
      domain: '',
      is_active: undefined,
      expire_from: undefined,
      expire_to: undefined
    };
    loadNodes();
  }

  onMounted(() => {
    loadNodes();
  });

  return {
    nodes,
    loading,
    filters,
    loadNodes,
    resetFilters
  };
}
