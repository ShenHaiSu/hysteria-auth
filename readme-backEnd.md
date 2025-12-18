# 后端设计规范（多代理服务器节点管理）

## 概述

- 本规范描述一个可扩展的多代理服务器节点管理后台，统一用户鉴权与节点分组运维。
- 系统以 `server_group` 作为节点归组依据，保障同组内共享属性的一致性，并提供严格的基于角色的访问控制与到期/启用状态管理。
- 数据层由 `users` 与 `node_server` 两张核心表构成，配套索引与触发器确保性能与一致性。

## 数据模型

- `users`（用户）
  - 说明：记录用户身份与鉴权信息。
  - 关键字段：
    - `username`（唯一）、`login_password_md5`（登录口令摘要）、`proxy_password`（代理认证口令）
    - `permission`（`admin` | `user`）、`is_active`（启用状态）
    - `proxy_expire_ts`（代理到期时间，单位秒；NULL/0 表示长期有效）
    - `register_ts`、`last_login_ts`、`register_ip`、`last_login_ip`
    - `created_at`、`updated_at`
- `node_server`（代理节点）
  - 说明：记录代理接入点与分组信息。
  - 关键字段：
    - `ip_address`（必填）、`domain`（可选）
    - `server_group`（分组标识）
    - 共享属性：`idc_name`（供应商）、`salamander`（混淆口令）、`rent_ts`（购买时间，默认当前）、`expire_ts`（到期时间，NULL/0 表示长期有效）、`fee`（租用费用）
    - `is_active`（启用状态）、`created_at`、`updated_at`
  - 说明：同一物理/逻辑节点的多个地址（如 IPv4/IPv6）以多条记录表示，通过 `server_group` 归组并共享属性。

## 访问控制与权限

- 角色定义：
  - `admin`：拥有用户与节点的完整管理权限（新增、修改、删除、全量查询与认证）。
  - `user`：仅可登录、查询启用节点、获取自身配置并参与代理认证。
- 查询约束：
  - `admin` 查询节点不受 `is_active` 限制。
  - `user` 仅可查询 `is_active=1` 的节点。

## 业务规则

- 组内属性联动：
  - 当同组（`server_group`）内任一记录更新共享属性（`idc_name`、`salamander`、`rent_ts`、`expire_ts`、`fee`），系统应自动同步到组内其他记录，确保一致性。
- 启用状态：
  - 节点 `is_active=0` 表示禁用，所有用户将无法使用该节点进行认证。
- 到期规则：
  - 用户认证：要求 `users.is_active=1`，且 `proxy_expire_ts` 未到期或为 NULL/0。
  - 节点认证：要求 `node_server.is_active=1`，且 `expire_ts` 未到期或为 NULL/0。
- 混淆口令：
  - `salamander` 为组内统一混淆/加密口令，需随组内更新而同步。
- 多地址归组：
  - 同一节点的多 IP/IPv4/IPv6 使用多条记录，但共享同一 `server_group` 与共享属性。

## 接口规范（建议）

- 认证与用户：
  - `POST /api/login`：登录并返回会话令牌与用户信息。
  - `GET /api/users/me`：查询当前用户信息。
  - `GET /api/users`（admin）：分页查询用户。
  - `POST /api/users`（admin）：新增用户。
  - `PUT /api/users/{id}`（admin）：更新用户。
  - `DELETE /api/users/{id}`（admin）：删除用户。
- 节点管理：
  - `GET /api/nodes`：查询节点列表；`user` 默认加 `is_active=1` 过滤；支持按 `server_group`、`ip_address`、`domain`、`is_active`、到期范围筛选。
  - `POST /api/nodes`（admin）：新增节点记录，指定 `server_group`；按地址逐条录入。
  - `PUT /api/nodes/{id}`（admin）：更新节点；如更新共享属性，应同步组内其他记录。
  - `DELETE /api/nodes/{id}`（admin）：删除节点。
- 代理配置下发：
  - `GET /api/proxy/config`：返回当前登录用户可用的代理节点与必要参数（如 `ip_address/domain`、`salamander`），仅包含启用且未到期的节点。

## 认证流程（概要）

- 登录：校验 `username` 与 `login_password_md5`，成功后签发会话令牌。
- 代理接入：
  - 用户侧校验：`is_active=1` 且 `proxy_expire_ts` 未到期或为 NULL/0。
  - 节点侧校验：`is_active=1` 且 `expire_ts` 未到期或为 NULL/0。
  - 下发参数：提供 `salamander` 与接入点（`ip_address` 或 `domain`）。
  - 异常处理：任一条件不满足则拒绝认证，并记录日志。

## 数据库约束与触发器

- 索引建议：
  - `node_server(ip_address, domain)` 唯一索引，避免重复接入点。
  - `node_server(server_group)`、`node_server(expire_ts)`、`node_server(is_active)` 常用筛选索引。
  - `users(username)` 唯一索引；`users(proxy_expire_ts)`、`users(last_login_ts)`、`users(is_active)`、`users(permission)` 常用筛选索引。
- 触发器建议：
  - 节点更新写入 `updated_at`。
  - 共享属性在组内传播，并写入 `updated_at`。

## 运维流程

- 新增节点：在目标 `server_group` 下逐条添加 IP/IPv6；共享属性由组内任何记录更新均可。
- 属性变更：修改 `salamander`、`idc_name`、`fee` 等共享属性时，仅需更新组内任一记录，触发器将同步。
- 到期处理：到期后可选择自动/人工将 `is_active` 置为 0；或仅依赖 `expire_ts` 在认证阶段拒绝。
- 退役节点：删除记录或设置 `is_active=0`。

## 安全与合规

- 口令存储：当前使用 `login_password_md5`；建议迁移为带盐强散列（如 `bcrypt`/`argon2`），并建立口令轮换策略。
- 日志与审计：记录登录尝试、认证失败、节点变更与到期处理，便于审计与回溯。
- 最小权限：所有管理接口仅对 `admin` 开放；`user` 仅保留必要访问能力。

## 性能与扩展

- 索引覆盖：`server_group`、`is_active`、到期字段应具备良好索引支持，以满足高并发查询。
- 一致性保障：数据库触发器提供强一致；应用层批量更新应采用事务，避免并发竞争。
- 可扩展字段：可根据需要在 `node_server` 增加端口、地域、协议版本、权重等，支持更复杂的路由与分流。

## 安全与合规

- 口令存储：目前使用 `login_password_md5`，建议后续升级为带盐的强散列（如 `bcrypt/argon2`），并引入口令轮换策略。
- 日志与审计：记录登录尝试、认证失败、节点变更与到期处理，便于审计与回溯。
- 访问控制：所有管理接口仅对 `admin` 开放；对 `user` 做最小权限原则。

## 性能与扩展

- 索引覆盖：`server_group`、`is_active`、到期字段的组合筛选需具备良好索引支持。
- 分组一致性：数据库层触发器保障强一致；应用层仍应在事务内处理批量更新，避免竞争。
- 可扩展字段：可在 `node_server` 增加端口、地域、协议版本等，以满足更复杂的接入。

## 开发注意事项

- Windows 环境优先；在 IDE 外部已具备实时预览，无需本地 dev 运行。
- 使用 PrimeVue/TailwindCSS 时，请以最新官方文档为准。
- Vue 前端开发默认使用 `ref`，不使用 `reactive`。
