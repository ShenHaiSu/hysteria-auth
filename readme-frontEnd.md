# Hysteria-Auth 前端项目说明文档

本项目是基于 Vue 3、Vite 7 和 PrimeVue 4 构建的 Hysteria-Auth 管理面板前端界面。

---

## 🛠 使用指南 (针对使用者)

本部分介绍如何安装依赖并完成项目的静态构建。

### 环境准备

在开始之前，请确保您的系统中已安装以下环境：
- **Node.js**: 推荐版本 `^20.19.0` 或 `>=22.12.0`。
- **pnpm**: 推荐使用最新版本的 `pnpm`。如果未安装，可以通过 `npm install -g pnpm` 安装。

### 安装与构建步骤

1. **进入前端目录**
   打开终端并进入 `frontEnd` 文件夹：
   ```bash
   cd e:/Programs/hysteria-auth/frontEnd
   ```

2. **安装依赖**
   使用 `pnpm` 安装项目所需的所有依赖包：
   ```bash
   pnpm install
   ```

3. **执行静态构建**
   运行以下命令进行生产环境构建。该命令会自动进行类型检查并生成优化的静态文件：
   ```bash
   pnpm build
   ```

4. **查看构建结果**
   构建完成后，生成的静态文件将根据 `vite.config.ts` 的配置，自动输出到后端的静态资源目录中：
   - **输出路径**: `backEnd/static`
   - **文件结构**:
     - `js/`: 存放 JavaScript 脚本文件。
     - `css/`: 存放 CSS 样式文件。
     - `assets/`: 存放其它静态资源（如图标、图片等）。

   您可以直接运行后端程序，它将自动托管这些生成的静态文件。

---

## 💻 开发者说明 (针对开发者)

本部分详细介绍项目所使用的技术栈、文件结构及模块化设计。

### 核心技术栈

- **框架**: [Vue 3](https://vuejs.org/) (Composition API, `<script setup>`)
- **构建工具**: [Vite 7](https://vitejs.dev/)
- **UI 组件库**: [PrimeVue 4](https://primevue.org/)
- **CSS 框架**: [Tailwind CSS 4](https://tailwindcss.com/)
- **状态管理**: [Pinia](https://pinia.vuejs.org/)
- **路由管理**: [Vue Router 4](https://router.vuejs.org/)
- **HTTP 客户端**: [Axios](https://axios-http.com/)
- **类型系统**: TypeScript

### 文件结构说明

项目采用模块化目录结构，方便维护和扩展：

```text
frontEnd/
├── src/
│   ├── asset/          # 静态资源与全局配置 (菜单定义、国际化、生成逻辑)
│   ├── components/     # 可复用的业务组件 (按功能模块划分)
│   ├── composable/     # 组合式函数 (封装业务逻辑与状态)
│   ├── fetch/          # API 请求层 (对接后端接口)
│   ├── layout/         # 页面布局组件
│   ├── router/         # 路由配置与导航守卫
│   ├── stores/         # Pinia 状态管理
│   ├── view/           # 页面级组件 (路由入口)
│   ├── App.vue         # 根组件
│   └── main.ts         # 入口文件
├── index.html          # HTML 模板
├── package.json        # 项目依赖与脚本配置
└── vite.config.ts      # Vite 配置文件
```

### 开发环境配置

- **本地开发代理**: 在开发模式下（`pnpm dev`），Vite 会将 `/api` 和 `/auth` 的请求代理到 `http://localhost:5172`（后端默认开发端口）。
- **自定义输出**: 项目配置了自定义的 Rollup 打包逻辑，确保不同类型的资源按目录分类存放。

### 核心模块功能

1. **认证模块 (Auth)**
   - 包含登录逻辑、Token 管理以及路由访问权限控制。
   - 核心文件：`composable/auth.ts`, `stores/auth.ts`, `router/middleware/auth.ts`。

2. **用户管理 (User Management)**
   - 提供用户列表展示、添加、修改及删除功能。
   - 核心文件：`view/User.vue`, `components/User/` 目录下的组件。

3. **节点管理 (Node Management)**
   - 负责 Hysteria 节点的配置与监控。
   - 核心文件：`view/Node.vue`, `components/Node/` 目录下的组件。

4. **配置下载 (Config Generation)**
   - 根据用户需求生成 Hysteria 客户端配置文件。
   - 核心文件：`view/ConfigDownload.vue`, `asset/configGenerate.ts`。

5. **仪表盘 (Dashboard/Home)**
   - 展示服务器状态、用户统计及节点概览。
   - 核心文件：`view/Home.vue`, `components/Home/` 目录下的统计组件。

### 开发规范

- **状态管理**: 默认禁止使用 `reactive`，统一使用 `ref` 来完成响应式数据定义。
- **组件风格**: 遵循 Vue 3 组合式 API 规范，优先使用 `composable` 提取复用逻辑。
- **UI/UX**: 基于 PrimeVue 4 的最新设计规范，并结合 Tailwind CSS 进行快速样式定制。
