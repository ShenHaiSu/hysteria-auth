import { ref } from 'vue';
import { defineStore } from 'pinia';
import { getServerStatus, getClientStatus } from '@/fetch/status';
import type { ServerStatus, ClientStatus } from '@/composable/status';

/**
 * 通用系统状态相关的 Pinia Store
 * 处理服务器运行状态和客户端请求上下文信息
 */
export const useCommonStore = defineStore('common', () => {
  // --- 状态 (State) ---
  const serverStatus = ref<ServerStatus | null>(null);
  const clientStatus = ref<ClientStatus | null>(null);
  const loading = ref(false);

  // --- 动作 (Actions) ---

  /**
   * 获取服务器运行状态概览
   */
  async function fetchServerStatus() {
    loading.value = true;
    try {
      serverStatus.value = await getServerStatus();
    } finally {
      loading.value = false;
    }
  }

  /**
   * 获取客户端请求上下文信息
   */
  async function fetchClientStatus() {
    loading.value = true;
    try {
      clientStatus.value = await getClientStatus();
    } finally {
      loading.value = false;
    }
  }

  /**
   * 同时刷新所有系统状态
   */
  async function refreshAllStatus() {
    loading.value = true;
    try {
      await Promise.all([
        fetchServerStatus(),
        fetchClientStatus()
      ]);
    } finally {
      loading.value = false;
    }
  }

  return {
    serverStatus,
    clientStatus,
    loading,
    fetchServerStatus,
    fetchClientStatus,
    refreshAllStatus,
  };
});
