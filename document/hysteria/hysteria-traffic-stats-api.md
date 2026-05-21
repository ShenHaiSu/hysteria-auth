# Hysteria 2 流量统计 API 详细文档

> **来源**: 基于 [Hysteria 2 官方文档 - 流量统计 API](https://v2.hysteria.network/zh/docs/advanced/Traffic-Stats-API/) 整理  
> **关联文档**: [`hysteria-server-config.md` 第 14 节](hysteria-server-config.md#14-流量统计-api-trafficstats) — 服务端配置中的 `trafficStats` 配置项  
> **适用范围**: Hysteria 2 服务端内建 HTTP API，用于查询流量统计、在线用户、TCP 流详情以及踢用户下线

---

## 目录

- [概述](#概述)
- [认证机制](#认证机制)
- [GET /traffic — 查询用户流量](#get-traffic--查询用户流量)
- [POST /kick — 踢用户下线](#post-kick--踢用户下线)
- [GET /online — 查询在线用户](#get-online--查询在线用户)
- [GET /dump/streams — 查询 TCP 流详情](#get-dumpstreams--查询-tcp-流详情)
- [与本项目的集成方案](#与本项目的集成方案)

---

## 概述

Hysteria 2 服务端内建了一个 HTTP API，用于：

| 功能 | 接口 | 方法 |
|------|------|------|
| 查询每个用户的累计流量 | `/traffic` | `GET` |
| 踢指定用户下线 | `/kick` | `POST` |
| 查询当前在线用户及连接数 | `/online` | `GET` |
| 导出所有 TCP 代理流的详细信息 | `/dump/streams` | `GET` |

API 的监听地址和认证密钥通过服务端配置文件中的 [`trafficStats`](hysteria-server-config.md#14-流量统计-api-trafficstats) 配置项指定：

```yaml
trafficStats:
  listen: :9999               # API 监听地址
  secret: some_secret         # 认证密钥（强烈建议设置）
```

---

## 认证机制

如果在配置中设置了 `secret`（**强烈建议**），调用任何 API 时都必须在请求头中携带 `Authorization` 标头，值为配置的密钥。

### 请求示例

```bash
curl -H 'Authorization: secret' http://ip:port/traffic
```

> **安全警告**: 如果不设置密钥，任何能访问该地址的人都可以查询流量信息和踢用户下线。建议同时配合 ACL/防火墙限制对 API 端口的访问。

---

## GET `/traffic` — 查询用户流量

返回一个 JSON map，键为用户名（即认证后端返回的 `id` 字段），值为该用户的流量统计信息。

### 响应格式

```json
{
  "wang": {
    "tx": 514,
    "rx": 4017
  },
  "joe": {
    "tx": 7790,
    "rx": 446623
  }
}
```

| 字段 | 类型 | 说明 |
|------|------|------|
| `tx` | int64 | 该用户累计上传字节数（服务端视角 = 客户端下载量） |
| `rx` | int64 | 该用户累计下载字节数（服务端视角 = 客户端上传量） |

> **注意**: 这里的 `tx`/`rx` 是**服务端视角**。服务端的 `tx`（发送）对应客户端的 `rx`（接收），反之亦然。

### 查询参数

| 参数 | 值 | 说明 |
|------|-----|------|
| `clear` | `1` | 返回统计数据后将其清零 |

```bash
# 查询流量并在返回后清零
curl -H 'Authorization: secret' "http://ip:port/traffic?clear=1"
```

---

## POST `/kick` — 踢用户下线

提交一个要踢下线的用户列表，服务端会强制断开这些用户的连接。

### 请求格式

**Content-Type**: `application/json`

请求体为一个 JSON 字符串数组，包含要踢下线的用户名：

```json
["wang", "joe"]
```

### 请求示例

```bash
curl -X POST \
  -H 'Authorization: secret' \
  -H 'Content-Type: application/json' \
  -d '["wang", "joe"]' \
  http://ip:port/kick
```

### 重要注意事项

> **由于客户端内置了重连逻辑，被踢出后将尝试重新连接。为了避免需要反复踢出同一个用户，应该同时在认证后端中屏蔽该用户（如禁用账号或拒绝认证）。**

在本项目的架构中，这意味着：
1. 通过 `/kick` 接口立即断开用户连接
2. 同时通过主服务器的用户管理 API 将用户标记为禁用（[`PUT /api/v1/users/{userId}`](architecture-design.md#424-更新用户) 设置 `isActive: false`）

---

## GET `/online` — 查询在线用户

返回一个 JSON map，键为用户名，值为该用户当前的连接数。

> **连接数说明**: 这里的「连接数」指的是 Hysteria 客户端实例的数量（可以理解为设备数），而非代理连接数。一个用户可能在多个设备上同时使用同一个账号。

### 响应格式

```json
{
  "wang": 2,
  "joe": 1
}
```

| 字段 | 类型 | 说明 |
|------|------|------|
| `{username}` | int | 该用户当前在线的 Hysteria 客户端实例数量 |

### 请求示例

```bash
curl -H 'Authorization: secret' http://ip:port/online
```

---

## GET `/dump/streams` — 查询 TCP 流详情

返回一个 JSON 对象，反映当前由 Hysteria 代理的**所有 TCP 流**的详细信息。这是进行深度排查和流量审计的重要接口。

### 响应格式

```json
{
  "streams": [
    {
      "state": "estab",
      "auth": "user",
      "connection": 3191736581,
      "stream": 4,
      "req_addr": "192.0.2.1:80",
      "hooked_req_addr": "example.com:80",
      "tx": 3937,
      "rx": 4441,
      "initial_at": "2024-11-08T16:07:45.956956773+09:00",
      "last_active_at": "2024-11-08T16:07:47.121503203+09:00"
    }
  ]
}
```

### 字段详解

| 字段 | 类型 | 说明 |
|------|------|------|
| `state` | string | 流状态，详见下方[流状态说明](#流状态说明) |
| `auth` | string | 使用这个流的用户名（即认证后端返回的 `id`） |
| `connection` | uint32 | 承载这个流的 QUIC 连接标识 |
| `stream` | uint64 | 这个流在 QUIC 连接中的标识（`connection` + `stream` 组合唯一标识一个流） |
| `req_addr` | string | 这个流请求连接到的**原始地址**（IP:Port 格式） |
| `hooked_req_addr` | string | 协议嗅探出来的地址。如果嗅探未启用或未识别出域名，则为空字符串 |
| `tx` | uint64 | 该流的累计发送字节数（服务端视角 = 客户端下载量） |
| `rx` | uint64 | 该流的累计接收字节数（服务端视角 = 客户端上传量） |
| `initial_at` | string (RFC 3339) | 这个流的创建时间 |
| `last_active_at` | string (RFC 3339) | 这个流最近一次传输数据的时间 |

### 流状态说明

流状态的值定义在 Hysteria 源码中（[参考链接](https://github.com/apernet/hysteria/blob/3e8c20518db0e97ad67b638e85cbe643b26d777a/core/server/config.go#L223-L257)），常见状态包括：

| 状态 | 说明 |
|------|------|
| `estab` | 已建立，正在传输数据 |
| `syn_sent` | 已向目标发起连接，等待响应 |
| `clos_wait` | 正在关闭中 |

### 人类可读输出

在请求 `/dump/streams` 时，如果在请求头中额外加上 `Accept: text/plain`，可以获得类似于 `ss -atn` 的人类可读表格输出：

```bash
curl -H 'Authorization: secret' -H 'Accept: text/plain' http://ip:port/dump/streams
```

**响应示例**：

```text
State    Auth           Connection   Stream     TX-Bytes     RX-Bytes     Lifetime  Last-Active Req-Addr         Hooked-Req-Addr
ESTAB    user             BE3E0905        4         3937         4441       3.005s        1.84s 192.0.2.1:80     example.com:80
```

---

## 与本项目的集成方案

> 关联文档: [`architecture-design.md`](architecture-design.md)、[`hysteria-server-config.md`](hysteria-server-config.md)

### 架构定位

在项目的边缘节点架构中，每个 VPS 上运行着 Hysteria 服务端 + Edge Agent。流量统计 API 直接由 Hysteria 服务端提供，Edge Agent 可以定时调用这些 API 采集数据并上报到主服务器。

```
┌──────────────────────────────────────┐
│           边缘节点 (VPS)              │
│                                      │
│  ┌─────────────┐    ┌─────────────┐  │
│  │  Hysteria    │    │ Edge Agent  │  │
│  │  Server      │◄───│             │  │
│  │              │    │ - 流量采集   │  │
│  │ trafficStats │    │ - 用户管理   │  │
│  │   :9999      │    │ - 状态上报   │──┼──► 主服务器
│  └─────────────┘    └─────────────┘  │
│                                      │
└──────────────────────────────────────┘
```

### 推荐集成方式

在实际部署中，有以下两种方案可供选择：

#### 方案一：Edge Agent 内部采集（推荐）

Edge Agent 定时调用本地 Hysteria 的流量统计 API，汇总后上报到主服务器：

```csharp
// Edge Agent 定时任务伪代码
public async Task CollectAndReportTrafficAsync()
{
    // 1. 调用本地 Hysteria API
    var traffic = await _httpClient.GetFromJsonAsync<Dictionary<string, TrafficInfo>>(
        "http://127.0.0.1:9999/traffic?clear=1"
    );

    // 2. 获取在线用户
    var online = await _httpClient.GetFromJsonAsync<Dictionary<string, int>>(
        "http://127.0.0.1:9999/online"
    );

    // 3. 汇总后上报到主服务器
    await _masterClient.ReportTrafficAsync(new TrafficReport
    {
        NodeId = _config.NodeId,
        UserTraffic = traffic,
        OnlineUsers = online,
        ReportedAt = DateTime.UtcNow
    });
}
```

#### 方案二：主服务器直接调用

主服务器直接访问各边缘节点的流量统计 API（需要边缘节点开放端口或通过 VPN 组网）：

```csharp
// 主服务器定时采集
public async Task CollectEdgeTrafficAsync(NodeInfo node)
{
    var traffic = await _httpClient.GetFromJsonAsync<Dictionary<string, TrafficInfo>>(
        $"http://{node.IpAddress}:{node.TrafficStatsPort}/traffic?clear=1",
        headers: new { Authorization = node.TrafficStatsSecret }
    );
    // 存入数据库...
}
```

### 配置建议

当使用 Edge Agent 代理模式时，建议将 Hysteria 的 `trafficStats.listen` 绑定到 `127.0.0.1`，仅允许本地 Edge Agent 访问：

```yaml
trafficStats:
  listen: 127.0.0.1:9999      # 仅本地回环可访问
  secret: your_secret_here     # Edge Agent 也需要配置此密钥
```

### 数据流向

```
Hysteria Server                Edge Agent                  主服务器
     │                             │                           │
     │  (每 N 秒)                   │                           │
     │◄──── GET /traffic?clear=1 ──│                           │
     │───► {user: {tx, rx}}        │                           │
     │                             │──► POST /api/v1/nodes/   │
     │◄──── GET /online ──────────│    {nodeId}/heartbeat     │
     │───► {user: connections}     │    (含流量数据)            │
     │                             │                           │
```

### 与主服务器数据库的映射

Hysteria 流量统计 API 返回的数据与主服务器数据库的对应关系：

| Hysteria API 字段 | 主服务器数据库 |
|-------------------|---------------|
| `traffic[user].tx` | `Users.UsedTrafficBytes`（上传）+ `TrafficRecords.BytesOut` |
| `traffic[user].rx` | `Users.UsedTrafficBytes`（下载）+ `TrafficRecords.BytesIn` |
| `online[user]` | 用于更新 `Sessions` 表活跃会话 |
| `/dump/streams` | 用于详细流量审计和故障排查 |

> **注意角度差异**: Hysteria 的 `tx`/`rx` 是服务端视角。`tx` = 服务端发出 = 客户端收到 = 用户的下载流量。在本项目的 `TrafficRecords` 表中，应分别记入 `BytesOut`（对应 tx）和 `BytesIn`（对应 rx）。

---

## 参考链接

- [Hysteria 2 官方文档 - 流量统计 API](https://v2.hysteria.network/zh/docs/advanced/Traffic-Stats-API/)
- [Hysteria 2 GitHub 仓库](https://github.com/apernet/hysteria)
- [流状态源码定义](https://github.com/apernet/hysteria/blob/3e8c20518db0e97ad67b638e85cbe643b26d777a/core/server/config.go#L223-L257)

---

> **文档版本**: 基于 Hysteria 2 官方文档 (v2.hysteria.network) 整理  
> **最后更新**: 2026-05-21
