# 后端 REST API 一览

> 说明：所有接口均以 `/api` 为前缀。需要令牌的接口使用 `Authorization: Bearer <token>` 传递。

## 认证

### POST /api/login
- 路径：`/api/login`
- 方法：`POST`
- 权限：公开（无令牌）；成功后返回令牌
- 作用：用户登录并获取会话令牌与基础信息
- 原理：根据用户名与MD5口令匹配激活用户，签发HS256令牌
- 请求体结构：
```json
{
  "username": "string",
  "login_password_md5": "string"
}
```
- 响应体结构：
```json
{
  "token": "string",
  "user": {
    "id": 1,
    "username": "string|null",
    "permission": "admin|user",
    "is_active": 1,
    "proxy_expire_ts": 0,
    "last_login_ts": 1730000000
  }
}
```
- fetch 请求示范：
```js
const res = await fetch("/api/login", {
  method: "POST",
  headers: { "content-type": "application/json" },
  body: JSON.stringify({ username: "alice", login_password_md5: "md5hex..." })
});
const data = await res.json();
// data.token 可用于后续 Authorization 头部
```

### POST /api/auth
- 路径：`/api/auth`
- 方法：`POST`
- 权限：公开；但服务端要求请求来源 IP 必须命中启用的节点 `node_server.is_active=1`
- 作用：代理节点与用户代理密码双向校验用于接入
- 原理：校验来源IP命中启用节点，再校验用户代理密码，返回标识
- 请求体结构：
```json
{
  "addr": "string",
  "auth": "string",  // 代理密码（users.proxy_password）
  "tx": 0            // 数字计数（示例字段）
}
```
- 响应体结构：
```json
{ "ok": true, "id": "username-or-id" }
```
失败示例：
```json
{ "ok": false, "id": "" }
```
- fetch 请求示范：
```js
const res = await fetch("/api/auth", {
  method: "POST",
  headers: { "content-type": "application/json" },
  body: JSON.stringify({ addr: "1.2.3.4:5678", auth: "proxy-pass", tx: 1 })
});
const data = await res.json();
```

## 用户

### GET /api/users/me
- 路径：`/api/users/me`
- 方法：`GET`
- 权限：需要令牌（任意角色）
- 作用：获取当前登录用户的基础信息
- 原理：从Bearer令牌解析会话并按ID查询用户
- 请求体结构：无
- 响应体结构：
```json
{
  "id": 1,
  "username": "string|null",
  "permission": "admin|user",
  "is_active": 1,
  "proxy_expire_ts": 0,
  "last_login_ts": 1730000000
}
```
- fetch 请求示范：
```js
const res = await fetch("/api/users/me", {
  headers: { "authorization": `Bearer ${token}` }
});
const me = await res.json();
```

### GET /api/users
- 路径：`/api/users`
- 方法：`GET`
- 权限：需要令牌（admin）
- 作用：管理员查询用户列表
- 原理：校验管理员令牌后查询users表
- 请求体结构：无
- 响应体结构：
```json
[
  { "id": 1, "name": "string", "email": "string", "created_at": "ISO" }
]
```
- fetch 请求示范：
```js
const res = await fetch("/api/users", {
  headers: { "authorization": `Bearer ${token}` }
});
const list = await res.json();
```

### GET /api/users/:id
- 路径：`/api/users/:id`
- 方法：`GET`
- 权限：需要令牌（admin）
- 作用：管理员获取指定用户详情
- 原理：校验管理员令牌后按ID查询users表
- 请求体结构：无
- 响应体结构：
```json
{ "id": 1, "name": "string", "email": "string", "created_at": "ISO" }
```
- fetch 请求示范：
```js
const res = await fetch("/api/users/1", {
  headers: { "authorization": `Bearer ${token}` }
});
const user = await res.json();
```

### POST /api/users
- 路径：`/api/users`
- 方法：`POST`
- 权限：需要令牌（admin）
- 作用：管理员新增用户
- 原理：校验管理员令牌后插入users表
- 请求体结构：
```json
{ "name": "string", "email": "string" }
```
- 响应体结构：
```json
{ "id": 2, "name": "string", "email": "string", "created_at": "ISO" }
```
- fetch 请求示范：
```js
const res = await fetch("/api/users", {
  method: "POST",
  headers: {
    "content-type": "application/json",
    "authorization": `Bearer ${token}`
  },
  body: JSON.stringify({ name: "Bob", email: "bob@example.com" })
});
const created = await res.json();
```

### PUT /api/users/:id
- 路径：`/api/users/:id`
- 方法：`PUT`
- 权限：需要令牌（admin）
- 作用：管理员更新用户
- 原理：校验管理员令牌后更新users表
- 请求体结构：
```json
{ "name": "string", "email": "string" }
```
- 响应体结构：
```json
{ "id": 1, "name": "string", "email": "string", "created_at": "ISO" }
```
- fetch 请求示范：
```js
const res = await fetch("/api/users/1", {
  method: "PUT",
  headers: {
    "content-type": "application/json",
    "authorization": `Bearer ${token}`
  },
  body: JSON.stringify({ name: "Alice", email: "alice@newmail.com" })
});
const updated = await res.json();
```

### DELETE /api/users/:id
- 路径：`/api/users/:id`
- 方法：`DELETE`
- 权限：需要令牌（admin）
- 作用：管理员删除用户
- 原理：校验管理员令牌后删除users表记录
- 请求体结构：无
- 响应体结构：
```json
{ "success": true }
```
- fetch 请求示范：
```js
const res = await fetch("/api/users/1", {
  method: "DELETE",
  headers: { "authorization": `Bearer ${token}` }
});
const result = await res.json();
```

## 节点

### GET /api/nodes
- 路径：`/api/nodes`
- 方法：`GET`
- 权限：需要令牌（任意角色）；使用普通用户角色携带 `user` 令牌时，默认附加 `is_active=1` 过滤；携带 `admin` 令牌可查询全部
- 作用：查询代理节点列表
- 原理：按筛选条件拼接SQL；普通用户默认附加is_active=1
- 请求体结构：无；支持查询参数：
```
server_group=string
ip_address=string
domain=string
is_active=0|1
expire_from=unixSec
expire_to=unixSec
```
- 响应体结构（列表）：
```json
[
  {
    "id": 1,
    "ip_address": "1.2.3.4",
    "domain": "example.com|null",
    "server_group": "groupA",
    "salamander": "string",
    "idc_name": "string|null",
    "rent_ts": 1730000000,
    "expire_ts": 0,
    "fee": 0,
    "is_active": 1,
    "created_at": "ISO",
    "updated_at": "ISO|null"
  }
]
```
- fetch 请求示范：
```js
const res = await fetch("/api/nodes?server_group=groupA&is_active=1", {
  headers: { "authorization": `Bearer ${token}` }
});
const nodes = await res.json();
```

### POST /api/nodes
- 路径：`/api/nodes`
- 方法：`POST`
- 权限：需要令牌（admin）
- 作用：管理员新增节点记录
- 原理：校验管理员令牌后插入node_server表
- 请求体结构：
```json
{
  "ip_address": "string",
  "domain": "string|null",
  "server_group": "string",
  "salamander": "string",
  "idc_name": "string|null",
  "rent_ts": 1730000000,
  "expire_ts": 0,
  "fee": 0,
  "is_active": 1
}
```
- 响应体结构：同 GET /api/nodes 列表中的单项结构
- fetch 请求示范：
```js
const res = await fetch("/api/nodes", {
  method: "POST",
  headers: {
    "content-type": "application/json",
    "authorization": `Bearer ${token}`
  },
  body: JSON.stringify({
    ip_address: "1.2.3.4",
    domain: null,
    server_group: "groupA",
    salamander: "obf-key",
    idc_name: "aws",
    rent_ts: Math.floor(Date.now()/1000),
    expire_ts: 0,
    fee: 0,
    is_active: 1
  })
});
const created = await res.json();
```

### PUT /api/nodes/:id
- 路径：`/api/nodes/:id`
- 方法：`PUT`
- 权限：需要令牌（admin）
- 作用：管理员更新节点记录（含共享属性）
- 原理：校验管理员令牌后更新；触发器同步组内共享属性
- 请求体结构：可部分字段更新（组内共享属性由触发器自动传播）
```json
{
  "ip_address": "string",
  "domain": "string|null",
  "server_group": "string",
  "salamander": "string",
  "idc_name": "string|null",
  "rent_ts": 1730000000,
  "expire_ts": 0,
  "fee": 0,
  "is_active": 1
}
```
- 响应体结构：同 GET /api/nodes 列表中的单项结构
- fetch 请求示范：
```js
const res = await fetch("/api/nodes/1", {
  method: "PUT",
  headers: {
    "content-type": "application/json",
    "authorization": `Bearer ${token}`
  },
  body: JSON.stringify({ salamander: "new-key", is_active: 1 })
});
const updated = await res.json();
```

### DELETE /api/nodes/:id
- 路径：`/api/nodes/:id`
- 方法：`DELETE`
- 权限：需要令牌（admin）
- 作用：管理员删除节点
- 原理：校验管理员令牌后删除node_server记录
- 请求体结构：无
- 响应体结构：
```json
{ "success": true }
```
- fetch 请求示范：
```js
const res = await fetch("/api/nodes/1", {
  method: "DELETE",
  headers: { "authorization": `Bearer ${token}` }
});
const result = await res.json();
```

## 代理配置下发

### GET /api/proxy/config
- 路径：`/api/proxy/config`
- 方法：`GET`
- 权限：需要令牌（user/admin）；且要求用户启用未到期（`is_active=1` 且 `proxy_expire_ts` 未过期或为 0/NULL）
- 作用：下发当前用户可用的代理节点配置
- 原理：校验用户状态未过期，查询启用且未到期节点
- 请求体结构：无
- 响应体结构：
```json
{
  "items": [
    { "server_group": "groupA", "ip_address": "1.2.3.4", "domain": null, "salamander": "string" }
  ]
}
```
- fetch 请求示范：
```js
const res = await fetch("/api/proxy/config", {
  headers: { "authorization": `Bearer ${token}` }
});
const cfg = await res.json();
```

## 状态

### GET /api/status/server
- 路径：`/api/status/server`
- 方法：`GET`
- 权限：公开
- 作用：查看服务器运行状态概览
- 原理：采集OS/CPU/内存/网络/环境等信息
- 请求体结构：无
- 响应体结构（示例，字段较多，这里仅列出关键）：
```json
{
  "runtime": { "bun": { "version": "x.y.z" }, "node_compat": { "platform": "win32" } },
  "os": { "hostname": "string", "platform": "win32", "uptime_sec": 123 },
  "cpu": { "count": 8, "model": "string", "speed_mhz": 2300 },
  "memory": { "total_bytes": 17179869184, "free_bytes": 8589934592 },
  "network": { "interfaces": [{ "iface": "Ethernet", "address": "192.168.1.2", "family": "IPv4" }] },
  "env": { "keys": ["PORT", "NODE_ENV"], "safe": { "PORT": "5172" } },
  "time": { "now_iso": "2025-01-01T00:00:00.000Z", "tz": "Asia/Shanghai" }
}
```
- fetch 请求示范：
```js
const res = await fetch("/api/status/server");
const status = await res.json();
```

### GET /api/status/client
- 路径：`/api/status/client`
- 方法：`GET`
- 权限：公开
- 作用：查看客户端请求上下文信息
- 原理：解析请求头、URL、Cookies、转发链与IP
- 请求体结构：无
- 响应体结构（示例，字段较多，这里仅列出关键）：
```json
{
  "method": "GET",
  "url": "http://localhost:5172/status/client",
  "path": "/status/client",
  "query": {},
  "ip": "127.0.0.1",
  "headers": { "user-agent": "string", "origin": "string" },
  "cookies": { "sid": "abc" },
  "ua": "string",
  "origin": "string",
  "referer": "string",
  "forwarded": { "x_forwarded_for": "", "x_real_ip": "" },
  "time": { "now_iso": "2025-01-01T00:00:00.000Z" }
}
```
- fetch 请求示范：
```js
const res = await fetch("/api/status/client");
const client = await res.json();
```
