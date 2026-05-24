import type { ProvisionStatus } from '@/types/common.types'

export type ObfsType = 'salamander'
export type CongestionControl = 'bbr' | 'cubic' | 'brutal'
export type MasqueradeType = 'file' | 'proxy' | 'string' | 'reply'
export type ResolverType = 'system' | 'udp' | 'tcp' | 'tls'

export interface NodeDto {
  id: string
  name: string
  ipAddress: string | null
  port: number
  isActive: boolean
  createdAt: string
  lastHeartbeat: string | null
  location: string | null
  trafficStatsPort?: number
  provisionStatus: ProvisionStatus
  // ═══ Phase 7 新增 ═══
  // 监听与端口跳跃
  listenAddress?: string | null
  listenPort?: number | null
  enablePortHopping?: boolean
  portHopRangeStart?: number | null
  portHopRangeEnd?: number | null
  // 混淆与拥塞控制
  obfsType?: ObfsType | null
  congestionControl?: CongestionControl | null
  brutalTxBandwidth?: number | null
  // 带宽与速度测试
  bandwidthUp?: string | null
  bandwidthDown?: string | null
  ignoreClientBandwidth?: boolean | null
  enableSpeedTest?: boolean | null
  speedTestPingInterval?: number | null
  // UDP 与协议嗅探
  udpIdleTimeout?: number | null
  sniffEnabled?: boolean | null
  sniffTimeout?: number | null
  sniffRespectHttps?: boolean | null
  // 伪装
  masqueradeType?: MasqueradeType | null
  masqueradeFile?: string | null
  masqueradeProxyUrl?: string | null
  masqueradeStringContent?: string | null
  masqueradeStringHeaders?: string | null
  masqueradeStringStatusCode?: number | null
  // DNS
  resolverType?: ResolverType | null
  resolverTcpAddr?: string | null
  resolverUdpAddr?: string | null
  resolverTlsAddr?: string | null
  // 配置版本与运营管理
  configVersion?: number
  configUpdatedAt?: string | null
  serverCost?: number | null
  billingCycle?: 'monthly' | 'quarterly' | 'yearly' | null
  expirationDate?: string | null
  domainName?: string | null
  remark?: string | null
}

export interface NodeDetail extends NodeDto {
  secretVersion: number
  trafficStatsSecret?: string
}

export interface NodeStatusRecord {
  id: number
  nodeId: string
  cpuUsagePercent: number
  memoryUsagePercent: number
  memoryUsedMb: number
  memoryTotalMb: number
  networkInBytes: number
  networkOutBytes: number
  networkInMbps: number
  networkOutMbps: number
  activeConnections: number
  reportedAt: string
}

export interface PreRegisterNodeRequest {
  name: string
  location?: string
  port?: number
  trafficStatsPort?: number
  // Phase 7 新增
  listenPort?: number
  domainName?: string
  remark?: string
}

export interface PreRegisterNodeResponse {
  provisionToken: string
  masterServerUrl: string
  expiresAt: string
  startupCommand: string
}

/**
 * PUT /api/v1/admin/nodes/{nodeId}/config 请求体
 * 所有字段可选，null 表示不修改
 */
export interface UpdateNodeConfigRequest {
  // 监听
  listenAddress?: string | null
  listenPort?: number | null
  enablePortHopping?: boolean | null
  portHopRangeStart?: number | null
  portHopRangeEnd?: number | null
  // 混淆
  obfsType?: ObfsType | null
  obfsPassword?: string | null
  // 拥塞控制
  congestionControl?: CongestionControl | null
  brutalTxBandwidth?: number | null
  // QUIC
  quicMaxIdleTimeout?: number | null
  quicMaxUdpPayloadSize?: number | null
  // 带宽
  bandwidthUp?: string | null
  bandwidthDown?: string | null
  ignoreClientBandwidth?: boolean | null
  // 速度测试
  enableSpeedTest?: boolean | null
  speedTestPingInterval?: number | null
  // UDP
  udpIdleTimeout?: number | null
  // 协议嗅探
  sniffEnabled?: boolean | null
  sniffTimeout?: number | null
  sniffRespectHttps?: boolean | null
  // 伪装
  masqueradeType?: MasqueradeType | null
  masqueradeFile?: string | null
  masqueradeProxyUrl?: string | null
  masqueradeStringContent?: string | null
  masqueradeStringHeaders?: string | null
  masqueradeStringStatusCode?: number | null
  // DNS
  resolverType?: ResolverType | null
  resolverTcpAddr?: string | null
  resolverUdpAddr?: string | null
  resolverTlsAddr?: string | null
  // 运营管理
  serverCost?: number | null
  billingCycle?: string | null
  expirationDate?: string | null
  domainName?: string | null
  remark?: string | null
}
