# Hysteria-Auth

本项目是一个针对多 **Hysteria2** 代理服务器的中控管理面板，旨在提供统一的用户鉴权、节点分配与运维管理方案。通过模块化的设计，实现了高效的节点分组管理、灵活的权限控制以及自动化的配置分发。

### ⚡ 极致性能的后端实现

后端完全采用 **TypeScript** 编写，原生运行于 **Bun** 环境。
- **零外部依赖**: 不依赖任何第三方库，极致纯粹，确保了系统的超轻量与超简洁。
- **超高并发承载**: 得益于 Bun 的高性能运行时与无依赖设计，具备卓越的 QPS 承载能力，能够轻松应对大规模代理认证请求。

---

## 🚀 核心特性

- **多节点中控管理**: 支持多物理/逻辑节点的统一管理，通过 `server_group` 实现同组节点属性（如端口、混淆、到期时间）的自动同步。
- **严格的角色访问控制 (RBAC)**: 区分 `admin` 与 `user` 角色。管理员拥有全量管理权，普通用户仅能查看启用节点并获取个人代理配置。
- **自动化安全防护**: 内置暴力请求防御、恶意爬虫拦截（UA 过滤）以及蜜罐路由，配合持久化黑名单系统，确保服务端特征隐藏与运行安全。
- **智能化配置下发**: 根据用户状态（是否过期、是否启用）和节点状态，动态生成并下发 Hysteria2 客户端配置文件。
- **实时监控与状态**: 内置服务器运行状态概览（CPU、内存、网络等）及客户端请求上下文分析接口。
- **全栈式设计**: 结合 Vue 3 前端与高性能后端（支持 Bun/Node 运行环境），提供流畅的管理体验。

---

## 🛠️ 技术栈

### 前端 (frontEnd)
- **框架**: [Vue 3](https://vuejs.org/) (Composition API)
- **UI 组件库**: [PrimeVue 4](https://primevue.org/) & [Tailwind CSS 4](https://tailwindcss.com/)
- **状态管理**: [Pinia](https://pinia.vuejs.org/)
- **构建工具**: [Vite 7](https://vitejs.dev/)
- **语言**: TypeScript

### 后端 (backEnd)
- **运行环境**: 原生 [Bun](https://bun.sh/) (极致性能)
- **开发语言**: TypeScript (100%)
- **依赖库**: **无任何第三方依赖 (Zero Dependencies)**
- **认证方案**: JWT (HS256) 无状态认证
- **数据层**: 支持触发器驱动的属性联动（SQLite/PostgreSQL 等）
- **规范**: 统一的 REST API 设计，支持多代理节点同步逻辑。

---

## 📂 项目结构

```text
hysteria-auth/
├── frontEnd/           # 前端源代码
│   ├── src/
│   │   ├── asset/      # 静态资源与配置逻辑
│   │   ├── components/ # 业务组件
│   │   ├── fetch/      # API 请求层
│   │   ├── view/       # 页面入口
│   │   └── ...
├── backEnd/            # 后端源代码 (包含静态资源托管)
│   ├── static/         # 前端构建产物 (js, css, assets)
│   └── ...
├── readme-frontEnd.md  # 前端详细文档
├── readme-backEnd.md   # 后端设计规范
└── readme-restApi.md   # REST API 接口文档
```

---

## 📖 业务逻辑概要

### 1. 节点归组 (Server Group)
系统引入 `server_group` 概念。同一分组内的节点（如 IPv4/IPv6 地址）共享以下核心属性：
- **连接参数**: `proxy_port`、`salamander` (混淆口令)
- **运维信息**: `idc_name` (供应商)、`fee` (费用)、`rent_ts` (购买时间)、`expire_ts` (到期时间)
当组内任一记录更新上述共享属性时，数据库触发器会自动同步至同组所有节点。

### 2. 多重安全防护
- **限流与封禁**: 基于内存的请求频率监测。单 IP 请求 > 100次/分触发预警，> 200次/分自动记入 `ip_blacklist` 并封禁 60 分钟。
- **特征隐藏**: 自动拦截 `curl`, `sqlmap` 等工具特征，并设置敏感文件陷阱（蜜罐）。移除 `X-Powered-By` 并伪装 `Server: nginx`。
- **持久化黑名单**: 所有触发安全规则的 IP 均会被持久化存储，支持跨重启生效，并支持每小时自动清理过期记录。

### 3. 鉴权与认证流程
- **登录鉴权**: 校验 `username` 与 `login_password_md5`，成功后签发 JWT HS256 无状态令牌。
- **代理接入校验**:
    - **用户侧**: 需满足 `is_active=1` 且 `proxy_expire_ts` 未到期（或为 0/NULL）。
    - **节点侧**: 需满足 `is_active=1` 且 `expire_ts` 未到期（或为 0/NULL）。
- **配置下发**: 仅下发对当前用户有效、且节点状态正常的 `salamander` 与接入地址。

### 4. API 接口概览
所有接口均以 `/api` 为前缀，主要分为以下类别：
- **身份认证**: `/api/login`, `/api/logout`, `/api/auth`
- **用户运维**: `/api/users` (支持管理员权限下的全量 CRUD)
- **节点管理**: `/api/nodes` (支持按 `server_group`、`is_active` 等条件筛选)
- **配置下发**: `/api/proxy/config` (获取当前用户可用的节点列表与连接参数)
- **系统监控**: `/api/status/server` (运行状态), `/api/status/client` (请求分析)

---

## 🛠️ 快速开始

### 前端构建
1. 进入目录：`cd frontEnd`
2. 安装依赖：`pnpm install`
3. 执行构建：`pnpm build` (构建产物将自动输出到 `backEnd/static`)

### 后端运行
1. 确保已安装 Bun 或 Node.js 环境。
2. 配置数据库及环境变量。
3. 启动后端程序，系统将自动托管前端静态资源。

---

*更多详细信息请参阅各模块对应的 `readme-*.md` 文件。*
