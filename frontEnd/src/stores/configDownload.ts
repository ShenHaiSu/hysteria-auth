import { ref } from 'vue'
import { defineStore } from 'pinia'
import type { NodeQueryParams } from '@/composable/node'
import { getNodes } from '@/fetch/node'
import { getUserById } from '@/fetch/user'
import { useAuthStore } from '@/stores/auth'
import { generateConfig as generateHy2Config } from '@/asset/configGenerate'

/**
 * 代理配置下载相关的 Pinia Store
 */
export const useConfigDownloadStore = defineStore('configDownload', () => {
  const authStore = useAuthStore()

  // --- 状态 (State) ---
  // 节点过滤条件 (参考 NodeFilter.vue)
  const filters = ref<NodeQueryParams>({
    server_group: '',
    ip_address: '',
    domain: '',
    is_active: 1, // 默认只下载启用的节点配置
  })

  // 管理员为指定用户生成配置时的用户 ID
  const targetUserId = ref<number | null>(null)

  const nodeSearch = ref('')
  const selectedNode = ref(null)
  const configType = ref('clash')
  const generatedConfig = ref('')
  const loading = ref(false)

  // --- 动作 (Actions) ---

  /**
   * 重置过滤条件
   */
  function resetFilters() {
    filters.value = {
      server_group: '',
      ip_address: '',
      domain: '',
      is_active: 1,
    }
    targetUserId.value = null
    nodeSearch.value = ''
    generatedConfig.value = ''
  }

  /**
   * 发起网络请求并生成配置
   * 1. 根据筛选条件获取节点列表
   * 2. 获取目标用户(或当前用户)的代理密码
   * 3. 逐一生成 hy2 链接并合并
   */
  async function generateConfig() {
    loading.value = true
    try {
      // 1. 获取节点列表 (后端搜索)
      let nodes = await getNodes(filters.value)

      // 前端过滤 (如果有 nodeSearch)
      if (nodeSearch.value) {
        const search = nodeSearch.value.toLowerCase()
        nodes = nodes.filter(
          (n) =>
            n.server_group.toLowerCase().includes(search) ||
            (n.domain && n.domain.toLowerCase().includes(search)) ||
            n.ip_address.includes(search),
        )
      }

      // 2. 获取用户信息 (Proxy_password)
      const userId = targetUserId.value || authStore.user?.id
      const proxyPassword = await getOrFetchProxyPassword(userId)

      if (!proxyPassword) throw new Error('无法获取代理密码，请检查用户配置')

      // 3. 逐一生成配置
      const configs = nodes.map((node, index) => {
        // 根据索引生成展示别名
        const displayAlias = `${index.toFixed(0).padStart(2, '0')}_${node.server_group || 'Hysteria2 Node'}`

        return generateHy2Config(
          proxyPassword,
          node.domain || node.ip_address,
          443,
          node.proxy_port,
          node.salamander,
          displayAlias,
        )
      })

      // 4. 将配置回写内存
      generatedConfig.value = configs.join('\n')
    } catch (error) {
      console.error('生成配置失败:', error)
      generatedConfig.value = `生成失败: ${error instanceof Error ? error.message : String(error)}`
    } finally {
      loading.value = false
    }
  }

  // #region 辅助函数 (Helper Functions)
  /**
   * 获取或拉取用户的代理密码
   * @param userId 用户 ID
   */
  async function getOrFetchProxyPassword(userId?: number | null) {
    if (!userId) {
      await authStore.fetchMe()
      userId = authStore.user?.id
    }

    if (userId) {
      const user = await getUserById(userId)
      return user.proxy_password
    }
    return ''
  }

  /**
   * 为单个节点生成配置字符串
   * @param node 节点对象 (兼容 NodeServer 和 ProxyConfigItem)
   * @param alias 别名，如果不提供则使用 node.server_group
   */
  async function generateSingleConfig(node: any, alias?: string) {
    try {
      const userId = targetUserId.value || authStore.user?.id
      const proxyPassword = await getOrFetchProxyPassword(userId)

      if (!proxyPassword) {
        throw new Error('无法获取代理密码')
      }

      return generateHy2Config(
        proxyPassword,
        node.domain || node.ip_address,
        node.server_port || 443,
        node.proxy_port,
        node.salamander,
        alias || node.server_group || 'Hysteria2 Node',
      )
    } catch (error) {
      console.error('生成单节点配置失败:', error)
      return ''
    }
  }
  // #endregion

  return {
    filters,
    targetUserId,
    nodeSearch,
    selectedNode,
    configType,
    generatedConfig,
    loading,
    resetFilters,
    generateConfig,
    generateSingleConfig,
  }
})
