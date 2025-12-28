// 配置生成函数

// 参考配置: hy2://admin:4kysQG7wWxYfcLwD8sEKDd18F@ser5.daoluolts.de:443/?mport=50000-59999&obfs=salamander&obfs-password=LJzKBgrV8mTgoRTPfxh0V5Pht#Ser5%20hy2
/**
 * 参考结构:
 * hy2://{password}@{domain/ip_address}:{base_port}/?mport={port_range}&obfs=salamander&obfs-password={obfs_password}#{proxy_display_alias}
 *
 * 说明:
 * - {password} 为用户配置的代理密码
 * - {domain/ip_address} 为服务器域名或 IP 地址
 * - {base_port} 为服务器监听端口
 * - {port_range} 为端口范围，格式为 "start-end"，如 "50000-59999"
 *    如果节点配置中的端口不是范围 那 mport 这个参数就不要写了, 直接将单端口写到base_port就可以了
 * - {obfs_password} 为混淆密码，用于增加安全性
 * - {proxy_display_alias} 为代理显示别名，用于识别不同的配置
 */

/**
 * 生成 Hysteria 2 配置文件链接
 * 
 * @param {string} password - 用户配置的代理密码
 * @param {string} domainOrIp - 服务器域名或 IP 地址
 * @param {number} basePort - 服务器监听端口
 * @param {string} portRange - 端口范围，格式为 "start-end"，如 "50000-59999"
 * @param {string} obfsPassword - 混淆密码
 * @param {string} proxyDisplayAlias - 代理显示别名
 * @returns {string} 生成的 hy2 链接字符串
 */
export const generateConfig = (
  password: string,
  domainOrIp: string,
  basePort: number,
  portRange: string,
  obfsPassword: string,
  proxyDisplayAlias: string,
): string => {
  const params: string[] = []

  // 处理端口范围: 如果不是范围格式则不包含 mport 参数
  if (portRange && portRange.includes('-')) {
    params.push(`mport=${portRange}`)
  }

  // 处理混淆配置: 如果没有混淆密码则不生成 obfs 相关配置
  if (obfsPassword) {
    params.push('obfs=salamander')
    params.push(`obfs-password=${obfsPassword}`)
  }

  const queryString = params.length > 0 ? `?${params.join('&')}` : ''
  const alias = encodeURIComponent(proxyDisplayAlias)

  return `hy2://${password}@${domainOrIp}:${basePort}/${queryString}#${alias}`
}
