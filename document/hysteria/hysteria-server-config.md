# Hysteria 2 完整服务端配置参考

> **来源**: 基于 [Hysteria 2 官方文档](https://v2.hysteria.network/zh/docs/advanced/Full-Server-Config/) 整理  
> **配置格式**: YAML（也支持 JSON，但官方推荐 YAML）  
> **关联文档**: [`architecture-design.md`](architecture-design.md) — 本项目的认证后端系统如何使用此配置

---

## 目录

- [配置格式约定](#配置格式约定)
- [1. 监听地址 (`listen`)](#1-监听地址-listen)
- [2. Realm (`realm`)](#2-realm-realm)
- [3. TLS (`tls` / `acme`)](#3-tls-tls--acme)
- [4. 混淆 (`obfs`)](#4-混淆-obfs)
- [5. QUIC 参数 (`quic`)](#5-quic-参数-quic)
- [6. 带宽 (`bandwidth`)](#6-带宽-bandwidth)
- [7. 速度测试 (`speedTest`)](#7-速度测试-speedtest)
- [8. UDP](#8-udp)
- [9. 验证 (`auth`)](#9-验证-auth)
- [10. DNS 解析 (`resolver`)](#10-dns-解析-resolver)
- [11. 协议嗅探 (`sniff`)](#11-协议嗅探-sniff)
- [12. ACL](#12-acl)
- [13. 出站规则 (`outbounds`)](#13-出站规则-outbounds)
- [14. 流量统计 API (`trafficStats`)](#14-流量统计-api-trafficstats)
- [15. 伪装 (`masquerade`)](#15-伪装-masquerade)
- [与认证后端的集成要点](#与认证后端的集成要点)

---

## 配置格式约定

Hysteria 2 在客户端和服务端配置中有多处使用「类型选择」的配置格式：

```yaml
example:
  type: a
  a:
    something: something
  b:
    something: something
  c:
    something: something
```

`type` 用于确定使用哪种模式以及解析哪些子字段。如果选择了 `a`，则会解析 `a` 子字段，而 `b` 和 `c` 子字段则会被忽略。

---

## 1. 监听地址 (`listen`)

`listen` 是服务器的监听地址。如果省略，服务器将默认监听 `:443` 端口（HTTP/3 的默认端口）。

```yaml
listen: :443
```

> 当只有端口没有 IP 地址时，服务器将监听所有可用的 IPv4 和 IPv6 地址。  
> - 仅监听 IPv4: `0.0.0.0:443`  
> - 仅监听 IPv6: `[::]:443`

### 端口范围（端口跳跃）

`listen` 也支持端口范围，用于[端口跳跃](../advanced/port-hopping/)：

```yaml
listen: :20000-50000
```

服务器将监听范围内的第一个端口，并自动设置防火墙规则（nftables 或 iptables）将其他端口的流量重定向到第一个端口。服务器关闭时会自动清理这些规则。

> **注意**: 端口范围监听仅支持 Linux，需要 `nft`（nftables）或 `iptables`/`ip6tables`，可能需要 root 或 `CAP_NET_ADMIN` 权限。

### Hysteria Realms URI

`listen` 字段还支持 [Hysteria Realms](../advanced/realms/) URI，让服务端在 NAT 后以 P2P 模式运行：

```yaml
listen: realm://user@realm-server.com/your-realm-name
```

---

## 2. Realm (`realm`)

[Hysteria Realms](../advanced/realms/) 模式的可选调优项。所有字段均为可选，默认值通常已经够用。

```yaml
realm:
  stunServers:          # STUN 服务器列表，用于发现服务端公网 UDP 地址
    - stun.nextcloud.com:3478
    - global.stun.twilio.com:3478
  stunTimeout: 5s        # 单个 STUN 服务器的查询超时
  punchTimeout: 5s        # 单次连接尝试中等待 UDP 打洞成功的最大时间
  heartbeatInterval: 30s  # 向牵线服务器发送心跳的间隔
  insecure: false         # 仅开发用途：跳过对自签牵线服务器的 TLS 校验
```

| 字段 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `stunServers` | list | 内置列表 | 用于 NAT 类型探测的 STUN 服务器 |
| `stunTimeout` | duration | — | 单个 STUN 查询超时时间 |
| `punchTimeout` | duration | — | UDP 打洞等待最大时间 |
| `heartbeatInterval` | duration | — | 向牵线服务器发送心跳的间隔 |
| `insecure` | bool | `false` | 是否跳过牵线服务器 TLS 校验 |

---

## 3. TLS (`tls` / `acme`)

**二者选其一**，不能同时包含 `tls` 和 `acme`。

### 方式一：手动证书 (`tls`)

```yaml
tls:
  cert: some.crt
  key: some.key
  sniGuard: strict | disable | dns-san
  clientCA: client.crt      # 使用客户端 CA 进行 mTLS 验证
```

| 字段 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `cert` | string | **必填** | TLS 证书文件路径。每次握手都会重新读取，可原地更新 |
| `key` | string | **必填** | TLS 私钥文件路径 |
| `sniGuard` | enum | `dns-san` | SNI 验证策略 |
| `clientCA` | string | — | 客户端 CA 证书路径（mTLS） |

`sniGuard` 可选值：

| 值 | 说明 |
|-----|------|
| `strict` | 严格验证客户端 SNI，与证书信息匹配才建立连接 |
| `disable` | 禁用 SNI 验证 |
| `dns-san` | **默认值**，仅当证书包含 DNS SAN 扩展时才启用验证 |

### 方式二：ACME 自动签发 (`acme`)

```yaml
acme:
  domains:
    - domain1.com
    - domain2.org
  email: user@example.com
  ca: zerossl               # letsencrypt 或 zerossl
  listenHost: 0.0.0.0      # ACME 验证监听地址（不含端口）
  dir: my_acme_dir          # 存储 ACME 账户密钥和证书的目录
  type: http | tls | dns    # 验证类型（类型选择）
  http:
    altPort: 8888           # HTTP 挑战监听端口（默认 80）
  tls:
    altPort: 44333          # TLS-ALPN 挑战监听端口（默认 443）
  dns:
    name: gomommy           # DNS 提供商名称
    config:
      key1: value1
      key2: value2
```

| 字段 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `domains` | list | **必填** | 要签发证书的域名列表 |
| `email` | string | **必填** | 联系邮箱 |
| `ca` | enum | — | CA 机构：`letsencrypt` 或 `zerossl` |
| `listenHost` | string | `0.0.0.0` | ACME 验证监听地址 |
| `dir` | string | — | ACME 数据存储目录 |
| `type` | enum | **必填** | 验证类型：`http` / `tls` / `dns` |
| `http.altPort` | int | `80` | HTTP 挑战替代端口 |
| `tls.altPort` | int | `443` | TLS-ALPN 挑战替代端口 |
| `dns.name` | string | **必填** | DNS 提供商名称，详见 [ACME DNS 配置](advanced/acme-dns-config/) |
| `dns.config` | map | — | DNS 提供商的配置参数 |

> **注意**: HTTP 挑战改用非 80 端口需配置端口转发或 HTTP 反向代理；TLS-ALPN 挑战改用非 443 端口需配置端口转发或 SNI Proxy，否则证书签署会失败。

---

## 4. 混淆 (`obfs`)

默认 Hysteria 协议伪装为 HTTP/3。如果网络针对性屏蔽了 QUIC 或 HTTP/3 流量，可使用混淆解决。目前唯一实现是 **"Salamander"** 混淆，将数据包混淆成无特征的 UDP 包。

```yaml
obfs:
  type: salamander          # 混淆类型（类型选择）
  salamander:
    password: cry_me_a_r1ver  # 混淆密码，客户端和服务端必须相同
```

| 字段 | 类型 | 说明 |
|------|------|------|
| `type` | enum | 混淆类型，当前仅 `salamander` |
| `salamander.password` | string | 混淆密码（必填） |

> **注意**: 启用混淆将使服务器与标准 QUIC 连接不兼容，失去 HTTP/3 伪装的能力。

---

## 5. QUIC 参数 (`quic`)

```yaml
quic:
  initStreamReceiveWindow: 8388608     # 初始流接收窗口
  maxStreamReceiveWindow: 8388608      # 最大流接收窗口
  initConnReceiveWindow: 20971520      # 初始连接接收窗口
  maxConnReceiveWindow: 20971520       # 最大连接接收窗口
  maxIdleTimeout: 30s                  # 最长空闲超时
  maxIncomingStreams: 1024             # 最大并发流入流数量
  disablePathMTUDiscovery: false       # 禁用 MTU 探测
```

| 字段 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `initStreamReceiveWindow` | int | `8388608` (8MB) | 初始 QUIC 流接收窗口大小 |
| `maxStreamReceiveWindow` | int | `8388608` (8MB) | 最大 QUIC 流接收窗口大小 |
| `initConnReceiveWindow` | int | `20971520` (20MB) | 初始 QUIC 连接接收窗口大小 |
| `maxConnReceiveWindow` | int | `20971520` (20MB) | 最大 QUIC 连接接收窗口大小 |
| `maxIdleTimeout` | duration | `30s` | 无数据后关闭连接的超时时间 |
| `maxIncomingStreams` | int | `1024` | 最大并发传入流的数量 |
| `disablePathMTUDiscovery` | bool | `false` | 是否禁用路径 MTU 探测 |

> **不建议随意修改默认值**。如需调整，建议保持流接收窗口与连接接收窗口的比例为 **2:5**。

---

## 6. 带宽 (`bandwidth`)

```yaml
bandwidth:
  up: 1 gbps
  down: 1 gbps
```

服务器端带宽值作为最高速度限制，限制服务器对每个客户端的最大速率。

> **重要**: 服务器上传速度对应客户端下载速度，反之亦然。

可以不写此字段，或在某一边设为零（零 = 无限制）。

### 支持的单位

| 单位写法 | 含义 |
|----------|------|
| `bps` / `b` | 每秒比特数 |
| `kbps` / `kb` / `k` | 每秒千比特 |
| `mbps` / `mb` / `m` | 每秒兆比特 |
| `gbps` / `gb` / `g` | 每秒吉比特 |
| `tbps` / `tb` / `t` | 每秒太比特 |

### 6.1 忽略客户端带宽设置

```yaml
ignoreClientBandwidth: false
```

启用后，服务器将忽略客户端设置的任何带宽，改为使用已配置的非 Brutal 控制器。此功能主要为不希望让用户自己设置带宽的服务器提供。

### 6.2 拥塞控制

```yaml
congestion:
  type: bbr
  bbrProfile: standard      # 仅在 type 为 bbr 时生效
```

拥塞控制器类型：

| 类型 | 说明 |
|------|------|
| `bbr` | Google BBR v1（**默认**） |
| `reno` | New Reno |

BBR 预设 (`bbrProfile`)：

| 预设 | 说明 |
|------|------|
| `standard` | 标准 BBR 预设（**默认**） |
| `conservative` | 更保守的预设 |
| `aggressive` | 更激进的预设 |

> `congestion` 是各端本地配置，不会通过协议协商。

### 6.3 带宽协商流程

下图展示了如何决定某个方向使用 Brutal 还是非 Brutal 控制器。

```
服务端 ignoreClientBandwidth: true？
    ├── 是 → 使用已配置的非 Brutal 控制器
    └── 否 → 客户端是否配置带宽？
                ├── 否 → 使用已配置的非 Brutal 控制器
                └── 是 → 使用 Brutal → 服务端是否配置带宽？
                            ├── 否 → 以客户端配置的带宽为准
                            └── 是 → 比较服务端和客户端带宽
                                      ├── 服务端更大 → 以客户端为准
                                      └── 客户端更大 → 以服务端为准
```

**简化场景**（自建自用）：直接删除服务端的 `bandwidth` 与 `ignoreClientBandwidth`，仅使用客户端 `bandwidth` 指定带宽。此时不配置则使用非 Brutal，配置则 Brutal 以客户端为准。

### 6.4 拥塞控制细节

Hysteria 有三种拥塞控制模式：

| 模式 | 说明 |
|------|------|
| **BBR** | Google 为 TCP 开发，移植到 QUIC。标准算法，包含慢启动和基于 RTT 的带宽估算，无需手动设置带宽 |
| **Reno** | `quic-go` 默认控制器，比 BBR 更简单、更保守。通过 `congestion.type: reno` 启用 |
| **Brutal** | Hysteria 自有算法。固定速率模型，丢包或 RTT 变化不会降低速度，反而根据丢包率提高发送速率补偿。擅长在拥塞网络中抢占带宽 |

> Brutal 带宽设置**低于**实际最大值可正常运行（相当于限速），但**绝不能高于**实际最大值，否则连接慢、不稳定且浪费流量。

> **当前版本限制**: 服务端的 `bandwidth` 限制仅对 **Brutal** 最大速率生效，使用 BBR 或 Reno 时不会生效。

---

## 7. 速度测试 (`speedTest`)

```yaml
speedTest: false
```

启用后，服务端允许客户端进行下载和上传速度测试。详见[速度测试文档](advanced/speed-test/)。

---

## 8. UDP

```yaml
disableUDP: false           # 禁用 UDP 转发，仅支持 TCP
udpIdleTimeout: 60s         # UDP 会话空闲超时
```

| 字段 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `disableUDP` | bool | `false` | 设为 `true` 则禁用 UDP 转发 |
| `udpIdleTimeout` | duration | `60s` | 无流量时保持本地 UDP 端口的时间（类似 NAT 超时） |

---

## 9. 验证 (`auth`)

```yaml
auth:
  type: password | userpass | http | command    # 验证类型（类型选择）
  password: your_password                        # 方式一：单密码
  userpass:                                      # 方式二：用户名-密码映射
    user1: pass1
    user2: pass2
    user3: pass3
  http:                                          # 方式三：HTTP 后端验证
    url: http://your.backend.com/auth
    insecure: false                              # 禁用后端 TLS 验证（仅 HTTPS）
  command: /etc/some_command                     # 方式四：命令验证
```

| 验证类型 | 说明 | 适用场景 |
|----------|------|----------|
| `password` | 单一密码验证 | 个人使用 / 简单场景 |
| `userpass` | 用户名-密码映射 | 少量用户管理 |
| `http` | HTTP 后端验证 | **本项目使用此方式**，对接主服务器认证 API |
| `command` | 执行命令验证 | 自定义脚本集成 |

### 9.1 HTTP 验证（本项目的核心集成方式）

当使用 HTTP 验证，客户端尝试连接时，服务器向后端发送 `POST` 请求：

**请求体**：

```json
{
    "addr": "123.123.123.123:44556",   // 客户端地址和端口
    "auth": "something_something",      // 客户端提交的密码
    "tx": 123456                        // 发送速率（字节/秒，服务器视角 = 客户端下载速率）
}
```

**后端必须返回**：

```json
{
    "ok": true,          // 是否允许连接
    "id": "john_doe"     // 客户 ID，用于日志和流量统计 API
}
```

> HTTP 状态码必须为 **200** 才认为验证成功，其他状态码均视为失败。

> **本项目对接**: 主服务器的认证 API 需要适配此协议。当前架构设计中 Edge Agent 充当中间代理，接收 Hysteria 的原生请求，转换后转发到主服务器。详见 [`architecture-design.md` 第 10.2 节](architecture-design.md#102-认证流程)。

### 9.2 命令验证

执行指定命令并附带参数：

```bash
/etc/some_command addr auth tx
```

参数定义与 HTTP 验证相同。命令须将客户 ID 输出到 `stdout`，返回退出代码 0 表示允许，非零表示拒绝。

---

## 10. DNS 解析 (`resolver`)

指定用于解析客户端请求中域名的 DNS 服务器。

```yaml
resolver:
  type: udp | tcp | tls | https    # 解析协议类型（类型选择）
  tcp:
    addr: 8.8.8.8:53
    timeout: 4s
  udp:
    addr: 8.8.4.4:53
    timeout: 4s
  tls:
    addr: 1.1.1.1:853
    timeout: 10s
    sni: cloudflare-dns.com
    insecure: false
  https:
    addr: 1.1.1.1:443
    timeout: 10s
    sni: cloudflare-dns.com
    insecure: false
```

| DNS 类型 | 说明 | 额外字段 |
|----------|------|----------|
| `udp` | 标准 UDP DNS | `addr`, `timeout` |
| `tcp` | 标准 TCP DNS | `addr`, `timeout` |
| `tls` | DNS over TLS | `addr`, `timeout`, `sni`, `insecure` |
| `https` | DNS over HTTPS | `addr`, `timeout`, `sni`, `insecure` |

如果省略 `resolver`，Hysteria 将使用系统默认 DNS 服务器。

---

## 11. 协议嗅探 (`sniff`)

当 Hysteria 无法获取域名形式的目标地址（如 TUN 模式下只能拿到 IP）时，通过 DPI 从上层协议中获取目标域名。

**支持的协议**：

- HTTP — Host 字段
- TLS (HTTPS) — SNI
- QUIC (HTTP/3) — SNI

```yaml
sniff:
  enable: true                         # 是否启用
  timeout: 2s                          # 嗅探超时，超时则用原地址
  rewriteDomain: false                 # 是否重写已是域名的请求
  tcpPorts: 80,443,8000-9000           # TCP 嗅探端口列表
  udpPorts: all                        # UDP 嗅探端口列表
```

| 字段 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `enable` | bool | `false` | 是否启用协议嗅探 |
| `timeout` | duration | `2s` | 超时未识别则用原地址发起连接 |
| `rewriteDomain` | bool | `false` | 对已是域名的请求也进行嗅探重写 |
| `tcpPorts` | string | 全部 | 嗅探的 TCP 端口，逗号分隔，支持范围 |
| `udpPorts` | string | 全部 | 嗅探的 UDP 端口，格式同上 |

> 端口列表格式与端口跳跃相同，支持逗号分隔的多个单端口和端口范围。不提供则默认嗅探所有端口。

---

## 12. ACL

ACL 是 Hysteria 服务端中非常强大的功能，用于自定义处理客户端请求的方式，常配合出站规则（outbounds）使用（如屏蔽地址、不同网站使用不同出口）。

**两种配置方式，不能混用**：

### 文件方式

```yaml
acl:
  file: some.txt              # ACL 文件路径
  # geoip: geoip.dat          # 可选。GeoIP 数据库路径（省略则自动下载）
  # geosite: geosite.dat      # 可选。GeoSite 数据库路径（省略则自动下载）
  # geoUpdateInterval: 168h   # 可选。数据库刷新间隔，默认 168h（1周）
```

### 内联方式

```yaml
acl:
  inline:
    - reject(suffix:v2ex.com)
    - reject(all, udp/443)
    - reject(geoip:cn)
    - reject(geosite:netflix)
  # geoip: geoip.dat
  # geosite: geosite.dat
  # geoUpdateInterval: 168h
```

| 字段 | 类型 | 说明 |
|------|------|------|
| `file` | string | ACL 规则文件路径 |
| `inline` | list | 内联 ACL 规则列表 |
| `geoip` | string | GeoIP 数据库路径（省略自动从 [Loyalsoldier/v2ray-rules-dat](https://github.com/Loyalsoldier/v2ray-rules-dat) 下载） |
| `geosite` | string | GeoSite 数据库路径（同上） |
| `geoUpdateInterval` | duration | 数据库刷新间隔（仅自动下载时生效） |

> **注意**: 仅在 ACL 中有至少一条 GeoIP/GeoSite 规则时才会下载对应数据库。当前版本仅在启动时下载，需借助外部工具定期重启才能实现自动更新，后续版本计划完善。

详细语法和使用方法请参考 [ACL 文档](advanced/acl/)。

---

## 13. 出站规则 (`outbounds`)

出站规则定义连接通过哪个「出口」路由。结合 ACL 可实现灵活的分流。

**支持的出站类型**：

| 类型 | 说明 |
|------|------|
| `direct` | 通过本地网络直接连接 |
| `socks5` | SOCKS5 代理 |
| `http` | HTTP/HTTPS 代理（**不支持 UDP**） |

> **不使用 ACL 时**，所有连接始终通过列表中的第一个（"默认"）出站规则路由，其他出站规则被忽略。

```yaml
outbounds:
  - name: my_outbound_1        # 出站名称，在 ACL 中使用
    type: direct
  - name: my_outbound_2
    type: socks5
    socks5:
      addr: shady.proxy.ru:1080
      username: hackerman      # 可选
      password: Elliot Alderson # 可选
  - name: my_outbound_3
    type: http
    http:
      url: http://username:password@192.168.1.1:8081   # 支持 http:// 或 https://
      insecure: false          # 可选，禁用 TLS 验证（仅 HTTPS）
```

| 字段 | 类型 | 说明 |
|------|------|------|
| `name` | string | 出站名称，供 ACL 引用 |
| `type` | enum | `direct` / `socks5` / `http` |
| `socks5.addr` | string | SOCKS5 代理地址 |
| `socks5.username` | string | SOCKS5 用户名（可选） |
| `socks5.password` | string | SOCKS5 密码（可选） |
| `http.url` | string | HTTP/HTTPS 代理 URL |
| `http.insecure` | bool | 是否禁用 HTTPS 代理的 TLS 验证 |

### 13.1 `direct` 出站额外选项

```yaml
outbounds:
  - name: hoho
    type: direct
    direct:
      mode: auto               # 地址族选择模式
      bindIPv4: 2.4.6.8        # 绑定的本地 IPv4
      bindIPv6: "0:0:0:0:0:ffff:0204:0608"  # 绑定的本地 IPv6
      bindDevice: eth233       # 绑定的本地网卡
      fastOpen: false          # TCP 快速打开
```

> **互斥约束**: `bindIPv4`、`bindIPv6` 和 `bindDevice` 三者互斥。可只指定 `bindIPv4` 和/或 `bindIPv6`，或只使用 `bindDevice`。

`mode` 可选值：

| 值 | 说明 |
|-----|------|
| `auto` | **默认值**，双栈模式，同时尝试 IPv4/IPv6，选择先成功的 |
| `64` | 优先 IPv6，无可用 IPv6 时用 IPv4 |
| `46` | 优先 IPv4，无可用 IPv4 时用 IPv6 |
| `6` | 仅 IPv6，无可用地址则失败 |
| `4` | 仅 IPv4，无可用地址则失败 |

---

## 14. 流量统计 API (`trafficStats`)

通过 HTTP API 查询服务器流量统计信息和踢用户下线。

```yaml
trafficStats:
  listen: :9999               # API 监听地址
  secret: some_secret         # 认证密钥（强烈建议设置）
```

| 字段 | 类型 | 说明 |
|------|------|------|
| `listen` | string | HTTP API 监听地址 |
| `secret` | string | 密钥，需在请求的 `Authorization` 头中提供 |

> **安全警告**: 不设置密钥则任何能访问该地址的人都可以查询流量信息和踢用户下线。强烈建议设置密钥，或用 ACL/防火墙限制对 API 端口的访问。

四个 API 接口的完整说明、请求/响应格式、字段详解以及与本项目的集成方案，详见 **[`hysteria-traffic-stats-api.md`](hysteria-traffic-stats-api.md)**。

---

## 15. 伪装 (`masquerade`)

Hysteria 抵抗审查的关键：伪装成标准 HTTP/3 流量，服务器会像普通网站一样响应 HTTP 请求。

> 如果审查不是问题，可完全删除此节，Hysteria 将对所有 HTTP 请求返回 404。

**伪装模式**：

| 模式 | 说明 |
|------|------|
| `file` | 静态文件服务器 |
| `proxy` | 反向代理 |
| `string` | 返回固定字符串 |

```yaml
masquerade:
  type: file | proxy | string    # 伪装类型（类型选择）
  file:
    dir: /www/masq               # 文件目录
  proxy:
    url: https://some.site.net   # 代理目标 URL
    rewriteHost: true            # 是否重写 Host 头
    insecure: false              # 是否禁用目标 TLS 验证
    xForwarded: false            # 是否设置 X-Forwarded-* 头
  string:
    content: hello stupid world  # 返回内容
    headers:                     # 可选，自定义 HTTP 头
      content-type: text/plain
      custom-stuff: ice cream so good
    statusCode: 200              # 可选，HTTP 状态码（默认 200）
  listenHTTP: :80                # HTTP (TCP) 伪装监听
  listenHTTPS: :443              # HTTPS (TCP) 伪装监听
  forceHTTPS: true               # HTTP 强制跳转 HTTPS
```

| 字段 | 类型 | 说明 |
|------|------|------|
| `type` | enum | 伪装模式：`file` / `proxy` / `string` |
| `file.dir` | string | 静态文件目录 |
| `proxy.url` | string | 反向代理目标 URL |
| `proxy.rewriteHost` | bool | 是否重写 `Host` 头匹配目标 |
| `proxy.insecure` | bool | 是否跳过目标 TLS 验证 |
| `proxy.xForwarded` | bool | 是否设置 `X-Forwarded-For`/`Host`/`Proto` 头 |
| `string.content` | string | 返回的固定字符串 |
| `string.headers` | map | 自定义响应头 |
| `string.statusCode` | int | 响应状态码，默认 200 |
| `listenHTTP` | string | HTTP TCP 监听地址 |
| `listenHTTPS` | string | HTTPS TCP 监听地址 |
| `forceHTTPS` | bool | HTTP 请求是否强制跳转 HTTPS |

### 15.1 测试伪装配置

用以下参数启动 Chrome 强制使用 QUIC 测试：

```bash
chrome --origin-to-force-quic-on=your.site.com:443
```

然后访问 `https://your.site.com` 验证。启动前须确保 Chrome 已完全退出。

> `listenHTTP`/`listenHTTPS` 提供了更完整的伪装——同时监听 TCP 的 HTTP/HTTPS，模仿标准网站行为。此功能仅为追求「做戏做全套」的用户提供。

---

## 与认证后端的集成要点

> 关联文档: [`architecture-design.md`](architecture-design.md)

### 认证配置映射

在项目架构中，Hysteria 服务端运行于每个**边缘节点**，通过 HTTP 认证与 Edge Agent 通信：

```yaml
# 边缘节点 Hysteria 配置（与 architecture-design.md §10.1 对应）
auth:
  type: http
  http:
    url: http://127.0.0.1:8080/auth      # Edge Agent 本地认证代理地址
    insecure: false                        # 本地回环无需 TLS
```

### Hysteria 原生认证协议 vs 项目 API

Hysteria 原生 HTTP 认证请求体与项目内部 API 有所不同，Edge Agent 负责协议转换：

| 对比维度 | Hysteria 原生协议 | 项目内部 API (`/api/v1/auth/hysteria`) |
|----------|-------------------|----------------------------------------|
| 认证字段 | `auth` (密码) | `password` |
| 用户标识 | 无用户名概念（取决于 `auth.type`） | `username` + `password` |
| 地址字段 | `addr` | `clientIp` |
| 节点标识 | 无 | `nodeId` |
| 速率信息 | `tx` (字节/秒) | 含在请求体中 |
| 成功响应 | `{"ok": true, "id": "..."}` | `{"success": true, "userId": ..., "remainingTraffic": ...}` |

### 推荐配置模板

以下是一个结合本项目的完整服务端配置参考：

```yaml
# Hysteria 2 服务端配置 — 边缘节点模板
listen: :443

tls:
  cert: /etc/hysteria/server.crt
  key: /etc/hysteria/server.key

auth:
  type: http
  http:
    url: http://127.0.0.1:8080/auth    # Edge Agent 认证代理
    insecure: false

bandwidth:
  up: 100 mbps
  down: 100 mbps

ignoreClientBandwidth: false

sniff:
  enable: true

udpIdleTimeout: 60s

speedTest: false

# 可选：流量统计 API（由项目主服务器管理时可不配置）
# trafficStats:
#   listen: :9999

# 可选：伪装
masquerade:
  type: proxy
  proxy:
    url: https://www.bing.com
    rewriteHost: true
```

---

## 参考链接

- [Hysteria 2 官方文档](https://v2.hysteria.network/)
- [完整客户端配置](advanced/full-client-config/)
- [ACL 配置](advanced/acl/)
- [流量统计 API 详细文档](hysteria-traffic-stats-api.md)（本地文档）
- [端口跳跃](advanced/port-hopping/)
- [Hysteria Realms](advanced/realms/)
- [速度测试](advanced/speed-test/)
- [ACME DNS 配置](advanced/acme-dns-config/)

---

> **文档版本**: 基于 Hysteria 2 官方文档 (v2.hysteria.network) 整理  
> **最后更新**: 2026-05-21
