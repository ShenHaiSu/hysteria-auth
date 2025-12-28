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
      let proxyPassword = ''
      const userId = targetUserId.value || authStore.user?.id

      if (!userId) {
        // 如果没有 targetUserId 且 authStore 也没用户信息，尝试获取一次
        await authStore.fetchMe()
      }

      const finalUserId = targetUserId.value || authStore.user?.id
      if (finalUserId) {
        // 无论是不是管理员，都尝试通过 getUserById 获取完整信息（包含 proxy_password）
        const user = await getUserById(finalUserId)
        proxyPassword = user.proxy_password
      }

      if (!proxyPassword) throw new Error('无法获取代理密码，请检查用户配置')

      // 3. 逐一生成配置
      const configs = nodes.map((node, index) => {
        // 根据索引生成展示别名
        const displayAlias = `${index.toFixed(0).padStart(2, '0')}_${node.server_group || 'Hysteria2 Node'}`

        // 解析node中的proxy_port
        // 如果是 数字-数字 就使用range 如果是纯数字 那就不是用range
        

        return generateHy2Config(
          proxyPassword,
          node.domain || node.ip_address,
          node.server_port || 443,
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
  }
})
