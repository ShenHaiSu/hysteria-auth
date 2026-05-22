# Hysteria Auth Web 管理面板 — 前端开发强制规范

> **文档版本**：v1.0  
> **最后更新**：2026-05-22  
> **定位**：本规范为强制执行标准，所有代码编写必须严格遵循。**禁止在没有规范和标准的情况下直接开始代码编写。**
> **关联文档**：[架构设计总览](../architect/README.md) · [核心模块设计](../architect/core-design.md) · [设计令牌规范](../architect/design-tokens.md) · [样式/设计/用色方案](./design-style-guide.md)

---

## 目录

- [0. 核心宪法](#0-核心宪法)
- [1. 代码风格规范](#1-代码风格规范)
- [2. 项目结构规范](#2-项目结构规范)
- [3. TypeScript 类型规范](#3-typescript-类型规范)
- [4. Vue 组件编写规范](#4-vue-组件编写规范)
- [5. 状态管理规范](#5-状态管理规范)
- [6. API 调用规范](#6-api-调用规范)
- [7. 路由与守卫规范](#7-路由与守卫规范)
- [8. 国际化规范](#8-国际化规范)
- [9. 样式编写规范](#9-样式编写规范)
- [10. 错误处理规范](#10-错误处理规范)
- [11. 性能规范](#11-性能规范)
- [12. Git 提交规范](#12-git-提交规范)
- [13. 禁止事项清单](#13-禁止事项清单)

---

## 0. 核心宪法

以下规则为最高优先级，任何情况不得违反：

> **规则 0.1** — 所有组件必须使用 `<script setup lang="ts">`，禁止使用 Options API。
>
> **规则 0.2** — 所有文件内的模块导入必须使用 `@/` 别名，禁止使用相对路径 `../../../`。
>
> **规则 0.3** — 所有用户可见文案必须通过 `vue-i18n` 的 `$t()` 或 `t()` 函数输出，禁止硬编码中文/英文。
>
> **规则 0.4** — 所有颜色值、间距值、动效参数必须引用 [`design-tokens.md`](../architect/design-tokens.md) 中定义的设计令牌，禁止在组件中硬编码。
>
> **规则 0.5** — 视图层（`src/views/`）不得直接调用 `axios` 或 API 模块，必须通过 Store 或 Composable 中转。
>
> **规则 0.6** — 每次提交前必须通过 `bun run type-check`，0 错误容忍。禁止提交类型检查不通过的代码。
>
> **规则 0.7** — 所有对后端 API 的请求参数和响应数据必须有完整的 TypeScript 类型定义。
>
> **规则 0.8** — 所有页面和可复用组件必须同时适配移动端（≥375px 视口）和桌面端（≥1280px 视口），禁止开发仅在单一端可用的界面。具体要求：
>   - 每个新增页面必须在移动端（375px）和桌面端（1920px）两种视口宽度下验证通过
>   - 布局类组件（侧边栏、顶部栏）必须同时考虑两端的交互模式差异
>   - 表格在移动端必须提供横向滚动或卡片替代方案
>   - 弹窗/对话框在移动端必须占满视口宽度或接近全屏
>   - 图表在移动端必须降低复杂度（减少数据点、简化图例）以适配窄屏

---

## 1. 代码风格规范

### 1.1 缩进与格式

- **缩进**：2 个空格（由 Prettier 强制）
- **行尾**：LF
- **引号**：单引号 `'`（字符串），模板字符串反引号 `` ` ``（含变量时）
- **分号**：不需要（由 Prettier 强制）
- **行宽**：120 字符
- **尾逗号**：所有多行结构必须加尾逗号（由 Prettier 强制）

### 1.2 命名规范

| 类型 | 规范 | 示例 | 说明 |
|------|------|------|------|
| 页面组件文件 | PascalCase + `View` 后缀 | `UserListView.vue` | |
| 对话框组件文件 | PascalCase + `Dialog` 后缀 | `UserFormDialog.vue` | |
| 通用组件文件 | PascalCase + `App` 前缀 | `AppStatusBadge.vue` | |
| Composable 文件 | camelCase + `use` 前缀 | `useAuth.ts` | |
| Store 文件 | camelCase + `.store` 后缀 | `auth.store.ts` | |
| API 模块文件 | kebab-case | `users.ts` | |
| 路由表文件 | kebab-case + `.route` 后缀 | `users.route.ts` | |
| 守卫文件 | kebab-case + `.guard` 后缀 | `auth.guard.ts` | |
| 类型文件 | kebab-case + `.types` 后缀 | `user.types.ts` | |
| 语言包文件 | kebab-case | `users.json` | |
| 工具函数文件 | kebab-case | `export-xlsx.ts` | |
| 布局文件 | PascalCase + `Layout` 后缀 | `DefaultLayout.vue` | |

### 1.3 变量/函数命名

```typescript
// ✅ 正确
const userList = ref<UserDto[]>([])
const isLoading = ref(false)
const fetchUsers = async () => { /* ... */ }
const handleDelete = (id: number) => { /* ... */ }

// ❌ 错误
const users = ref([])           // 缺少类型
const loading = ref(false)      // 缺少语义前缀
const getUsers = () => {}       // 非 async 不应使用动词前缀
```

| 元素 | 规范 | 示例 |
|------|------|------|
| 响应式状态 | camelCase + 名词 | `userList`, `currentUser`, `searchQuery` |
| 布尔状态 | `is`/`has`/`can` 前缀 | `isLoading`, `hasError`, `canEdit` |
| 异步函数 | camelCase + `fetch`/`load` 前缀 | `fetchUsers()`, `loadNodeDetail()` |
| 事件处理函数 | camelCase + `handle`/`on` 前缀 | `handleSubmit()`, `onRowClick()` |
| 计算属性 | camelCase + 名词 | `activeUsers`, `filteredNodes` |
| Props | camelCase | `isActive`, `totalTrafficBytes` |
| Events | kebab-case | `@update:model-value`, `@user-deleted` |

### 1.4 注释规范

```typescript
// ✅ 正确 — JSDoc 用于公共 API
/**
 * 获取用户列表
 * @param params - 分页和筛选参数
 * @returns 分页用户列表
 */
async function fetchUsers(params: UserListParams): Promise<PageResponse<UserDto>> {
  // ...
}

// ✅ 正确 — 行内注释用于解释非显而易见的逻辑
const isOnline = lastHeartbeat && (Date.now() - new Date(lastHeartbeat).getTime()) < 90_000

// ❌ 错误 — 废话注释
// 获取用户列表
async function fetchUsers() { }
```

---

## 2. 项目结构规范

### 2.1 强制目录结构

项目的完整目录结构必须严格遵循 [`directory-structure.md`](../architect/directory-structure.md) 中定义的结构。任何新增文件必须放在正确的目录中：

```
src/
├── api/           ← 所有 HTTP 请求相关代码
│   ├── modules/   ← 按功能域拆分的 API 函数
│   └── adapters/  ← 数据适配器
├── assets/        ← 静态资源
│   └── styles/    ← 全局样式
├── components/    ← 可复用组件
│   ├── common/    ← 通用基础组件
│   ├── layout/    ← 布局组件
│   ├── charts/    ← 图表组件
│   ├── forms/     ← 表单组件
│   └── modals/    ← 模态框组件
├── composables/   ← 组合式函数
├── directives/    ← 自定义指令
├── layouts/       ← 布局模板
├── locales/       ← 国际化语言包
│   ├── zh-CN/
│   └── en-US/
├── router/        ← 路由配置
│   ├── routes/    ← 路由表
│   └── guards/    ← 导航守卫
├── stores/        ← Pinia Store
├── types/         ← TypeScript 类型定义
├── utils/         ← 工具函数
└── views/         ← 页面视图
```

### 2.2 文件大小限制

- **组件文件**：不超过 300 行。超过时拆分子组件。
- **Store 文件**：不超过 200 行。超过时拆分为多个 Store 或用 Composable 辅助。
- **类型文件**：不超过 100 行。超过时拆分。
- **语言包 JSON**：每个文件不超过 80 个 key。

---

## 3. TypeScript 类型规范

### 3.1 类型定义原则

```typescript
// ✅ 正确 — interface 优先
export interface UserDto {
  id: number
  username: string
  isActive: boolean
}

// ✅ 正确 — 联合类型用于枚举
export type AdminRole = 'super_admin' | 'admin' | 'readonly'
export type ProvisionStatus = 'pending' | 'provisioned'

// ❌ 错误 — 使用 TypeScript enum（树摇效果差）
enum AdminRole {
  SUPER_ADMIN = 'super_admin',
  ADMIN = 'admin',
}
```

### 3.2 API 类型必须完整

```typescript
// ✅ 正确 — 每个 API 函数有完整的输入输出类型
export const userApi = {
  getList(params: UserFilters & PageRequest): Promise<AxiosResponse<PageResponse<UserDto>>> {
    return http.get('/users', { params })
  },

  create(data: CreateUserRequest): Promise<AxiosResponse<UserDto>> {
    return http.post('/users', data)
  },
}

// ❌ 错误 — 缺少类型或使用 any
export const userApi = {
  getList(params: any) {
    return http.get('/users', { params })
  },
}
```

### 3.3 禁止使用的类型

| 禁止 | 原因 | 替代方案 |
|------|------|---------|
| `any` | 完全跳过类型检查 | `unknown` + 类型守卫 |
| `as` 类型断言（无校验） | 可能隐藏运行时错误 | 类型守卫函数 |
| `enum` | 运行时开销，树摇差 | Union type |
| `Function` 类型 | 不精确 | 具体函数签名 `(x: string) => void` |
| `Object` 类型 | 几乎等于 `any` | `Record<string, unknown>` 或具体 interface |

> **例外**：`as` 断言仅在以下场景允许：`e.target!.result as ArrayBuffer`（FileReader 处理）、`JSON.parse()` 的结果断言（配合类型守卫）。

---

## 4. Vue 组件编写规范

### 4.1 组件结构顺序

```vue
<!-- ✅ 正确 — 结构顺序 -->
<script setup lang="ts">
// 1. 类型导入
import type { UserDto } from '@/types/user.types'

// 2. 运行时导入
import { ref, computed, onMounted } from 'vue'
import { useUsersStore } from '@/stores/users.store'

// 3. Props 和 Emits
const props = withDefaults(defineProps<{
  userId?: number
}>(), {
  userId: undefined,
})

const emit = defineEmits<{
  'user-deleted': [id: number]
}>()

// 4. Composables 和 Stores
const usersStore = useUsersStore()
const { t } = useI18n()

// 5. 响应式状态
const isLoading = ref(false)

// 6. 计算属性
const activeUsers = computed(() => { /* ... */ })

// 7. 方法
async function handleDelete(id: number) { /* ... */ }

// 8. 生命周期
onMounted(() => { /* ... */ })
</script>

<template>
  <!-- 模板 -->
</template>

<style scoped>
/* 仅 TailwindCSS 无法表达的样式 */
</style>
```

### 4.2 Props 定义

```typescript
// ✅ 正确 — TypeScript 泛型 Props
const props = withDefaults(defineProps<{
  userId: number
  isActive?: boolean
  items?: UserDto[]
}>(), {
  isActive: true,
  items: () => [],
})

// ❌ 错误 — 运行时 Props（丢失类型安全）
const props = defineProps({
  userId: Number,
  isActive: Boolean,
})
```

### 4.3 Emits 定义

```typescript
// ✅ 正确 — TypeScript 泛型 Emits
const emit = defineEmits<{
  'update:model-value': [value: string]
  'user-deleted': [id: number]
  submit: [data: CreateUserRequest]
}>()
```

### 4.4 模板规范

```vue
<template>
  <!-- ✅ 正确 — v-permission 指令控制权限 -->
  <Button
    v-permission="['super_admin', 'admin']"
    :label="t('common.actions.delete')"
    severity="danger"
    :loading="isDeleting"
    @click="handleDelete(user.id)"
  />

  <!-- ✅ 正确 — 条件渲染用 v-if（不是 v-show，除非频繁切换） -->
  <DataTable v-if="!isLoading" :value="usersStore.items">
    <!-- ... -->
  </DataTable>
  <AppLoading v-else />

  <!-- ❌ 错误 — 大量 v-if/v-else 链式嵌套 -->
  <div v-if="a">
    <div v-if="b">
      <div v-if="c">...</div>
    </div>
  </div>
</template>
```

### 4.5 组件通信规范

| 场景 | 方案 | 说明 |
|------|------|------|
| 父→子 | Props | 单向数据流 |
| 子→父 | Emits | 事件通知 |
| 跨层级共享 | Pinia Store | 全局状态 |
| 兄弟组件通信 | Pinia Store | 不要通过父组件中转 |
| 对话框开闭 | `v-model` + Props | 标准模式 |

---

## 5. 状态管理规范

### 5.1 Store 定义

```typescript
// ✅ 正确 — Pinia Setup Store 语法
// src/stores/users.store.ts
import { ref, computed } from 'vue'
import { defineStore } from 'pinia'
import { userApi } from '@/api/modules/users'
import type { UserDto, UserFilters } from '@/types/user.types'

export const useUsersStore = defineStore('users', () => {
  // State
  const items = ref<UserDto[]>([])
  const total = ref(0)
  const page = ref(1)
  const pageSize = ref(20)
  const isLoading = ref(false)
  const filters = ref<UserFilters>({})
  const searchQuery = ref('')

  // Getters
  const hasMore = computed(() => page.value * pageSize.value < total.value)

  // Actions
  async function fetchUsers() {
    isLoading.value = true
    try {
      const res = await userApi.getList({
        ...filters.value,
        search: searchQuery.value || undefined,
        page: page.value,
        pageSize: pageSize.value,
      })
      items.value = res.data.items
      total.value = res.data.total
    } finally {
      isLoading.value = false
    }
  }

  function resetFilters() {
    filters.value = {}
    searchQuery.value = ''
    page.value = 1
    fetchUsers()
  }

  return {
    items, total, page, pageSize, isLoading, filters, searchQuery,
    hasMore,
    fetchUsers, resetFilters,
  }
})
```

### 5.2 Store 使用规则

- **Store 不直接操作 DOM**：Store 中禁止访问 `document`、`window`（除 `localStorage`）
- **Store 不持有组件实例引用**：禁止在 Store 中存储 `ref<HTMLElement>` 等
- **Store 之间的依赖**：只允许 `authStore` 被其他 Store 引用（用于获取 token/role）
- **避免循环依赖**：A Store 引用 B Store，B Store 就不能引用 A Store

### 5.3 状态持久化

```typescript
// ✅ 正确 — 使用 localStorage 封装
import { storage } from '@/utils/storage'

export const useAuthStore = defineStore('auth', () => {
  const token = ref<string | null>(storage.get('token'))

  function setToken(newToken: string) {
    token.value = newToken
    storage.set('token', newToken)
  }

  function clearToken() {
    token.value = null
    storage.remove('token')
  }
})
```

---

## 6. API 调用规范

### 6.1 调用层级

```
视图层 (View)         → 调用 Store Action
状态层 (Store)         → 调用 API 模块函数
API 层 (api/modules/)  → 调用 axios 实例
```

**视图层禁止直接调用 `api/modules/` 中的函数。** 唯一的例外是 Composable 中可以调用 API（如 `useAuth.ts` 中的 `refreshToken`）。

### 6.2 请求处理模式

```typescript
// ✅ 正确 — Store 中的标准请求处理
async function deleteUser(id: number) {
  try {
    await userApi.delete(id)
    toast.success(t('users.toast.deleteSuccess'))
    await fetchUsers() // 刷新列表
  } catch (err) {
    // 错误已在 axios 拦截器中统一处理
    // 此处仅处理需要特殊逻辑的错误
    console.error('Delete user failed:', err)
  }
}
```

### 6.3 请求参数规范

```typescript
// ✅ 正确 — GET 请求参数通过 params 传递
http.get('/users', {
  params: {
    page: 1,
    pageSize: 20,
    search: 'keyword',
    isActive: true,
  },
})

// ✅ 正确 — POST/PUT 请求参数通过 body 传递
http.post('/users', {
  username: 'user123',
  password: 'password123',
})

// ❌ 错误 — 手动拼接 URL
http.get(`/users?page=1&pageSize=20&search=keyword`)
```

---

## 7. 路由与守卫规范

### 7.1 路由定义

```typescript
// ✅ 正确 — 每个功能域独立路由文件
// src/router/routes/users.route.ts
import type { RouteRecordRaw } from 'vue-router'

export const userRoutes: RouteRecordRaw[] = [
  {
    path: 'users',
    name: 'Users',
    component: () => import('@/views/users/UserListView.vue'),
    meta: {
      title: '用户管理',
      icon: 'pi pi-users',
      roles: ['super_admin', 'admin', 'readonly'],
    },
  },
  {
    path: 'users/:id',
    name: 'UserDetail',
    component: () => import('@/views/users/UserDetailView.vue'),
    meta: {
      title: '用户详情',
      roles: ['super_admin', 'admin', 'readonly'],
      hidden: true,
    },
  },
]
```

### 7.2 路由 Meta 强制字段

每个路由的 `meta` 必须包含：

| 字段 | 类型 | 必须 | 说明 |
|------|------|------|------|
| `title` | string | ✅ | 页面标题，用于 document.title 和面包屑 |
| `roles` | AdminRole[] | ✅（受保护路由） | 允许访问的角色列表 |
| `requiresAuth` | boolean | ❌ | 是否需要认证，默认 `true` |
| `hidden` | boolean | ❌ | 是否在侧边栏隐藏，详情页等设为 `true` |

### 7.3 新增路由流程

1. 在对应的 `src/router/routes/xxx.route.ts` 中添加路由
2. 确保 `meta.title` 已定义
3. 确保 `meta.roles` 已定义（受保护路由）
4. 无需修改其他文件（`routes/index.ts` 中已聚合）

---

## 8. 国际化规范

### 8.1 Key 命名规范

```
{domain}.{section}.{key}

✅ 正确：
  users.table.columns.username
  users.form.fields.totalTraffic.label
  users.toast.createSuccess
  common.actions.save
  validation.required

❌ 错误：
  username              — 缺少 domain 前缀
  users_username        — 使用下划线而非点分隔
  userManagementTitle   — 使用驼峰而非点分隔
```

### 8.2 模板中的使用

```vue
<template>
  <!-- ✅ 正确 — 使用 $t() -->
  <h2>{{ $t('users.title') }}</h2>
  <Button :label="$t('common.actions.save')" />

  <!-- ❌ 错误 — 硬编码文案 -->
  <h2>用户管理</h2>
  <Button label="保存" />
</template>
```

### 8.3 Script 中的使用

```typescript
// ✅ 正确
const { t } = useI18n()

function handleError() {
  toast.error(t('users.toast.deleteFailed'))
}
```

### 8.4 语言包结构

每个语言包 JSON 文件的结构必须与 `zh-CN` 对应文件完全一致。新增 key 时必须同时在两个语言文件中添加。

---

## 9. 样式编写规范

### 9.1 TailwindCSS 优先原则

- **优先使用 TailwindCSS 原子类**完成所有布局和样式
- **仅在以下情况**使用 `<style scoped>`：
  1. TailwindCSS 无法表达的复杂样式（如深层伪元素 `::webkit-scrollbar`）
  2. 复用度高的样式集（建议提取为 TailwindCSS `@utility` 或 `@apply`）
  3. Vue `<Transition>` 动画的 CSS 类

### 9.2 颜色/间距/动效令牌

```vue
<template>
  <!-- ✅ 正确 — 使用 CSS 变量引用设计令牌 -->
  <div
    class="bg-[var(--bg-primary)] text-[var(--text-primary)] border-[var(--border-light)] rounded-md"
  >
    内容
  </div>

  <!-- ✅ 正确 — 使用 TailwindCSS @theme 扩展的品牌色 -->
  <Button class="bg-brand-500 hover:bg-brand-600 text-white" />

  <!-- ✅ 正确 — 使用 dark: 变体处理暗色模式 -->
  <div class="bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100" />

  <!-- ❌ 错误 — 硬编码颜色值 -->
  <div class="bg-[#FFFFFF] text-[#111827]" />

  <!-- ❌ 错误 — 硬编码间距 -->
  <div class="p-[24px] m-[16px]" />
</template>
```

详细规范参见 [`design-style-guide.md`](./design-style-guide.md)。

---

## 10. 错误处理规范

### 10.1 错误分层处理

| 层级 | 处理内容 | 示例 |
|------|---------|------|
| axios 拦截器 | Token 过期 → 自动跳转登录 | `token_expired` → `authStore.logout()` |
| axios 拦截器 | 网络错误 → Toast 提示 | `Network Error` → "网络连接失败" |
| Store Action | 业务异常 → Toast 提示 | 删除失败 → "用户删除失败" |
| 视图层 | UI 回退 → 恢复表单状态 | 提交失败 → 按钮恢复可点击 |

### 10.2 错误代码映射

所有错误代码的处理必须按照 [`master-panel-api.md`](../exchange/master-panel-api.md) §1.2 中的"全局错误代码表"进行映射。

```typescript
// src/api/index.ts — 响应拦截器
switch (errorData?.code) {
  case 'token_expired':
  case 'token_invalid':
  case 'unauthorized':
    authStore.logout()
    router.push({ name: 'Login', query: { redirect: router.currentRoute.value.fullPath } })
    break
  case 'forbidden':
    toast.error(t('common.error.forbidden'))
    break
  case 'rate_limited':
    toast.warning(t('common.error.rateLimited'))
    break
  case 'internal_error':
    toast.error(t('common.error.internalError'))
    break
  default:
    // 其他错误由调用方处理
    break
}
```

### 10.3 try-catch 规范

```typescript
// ✅ 正确 — 明确处理已知错误
try {
  await userApi.delete(id)
  toast.success(t('users.toast.deleteSuccess'))
} catch (err) {
  // 检查是否为 axios 错误
  if (axios.isAxiosError(err) && err.response?.data?.error?.code === 'not_found') {
    toast.error(t('users.toast.notFound'))
  }
  // 其他错误已在拦截器中处理，此处静默
}
```

---

## 11. 性能规范

### 11.1 路由懒加载

```typescript
// ✅ 正确 — 所有页面组件使用动态 import
{
  path: 'users',
  component: () => import('@/views/users/UserListView.vue'),
}

// ❌ 错误 — 静态 import（打包在首屏）
import UserListView from '@/views/users/UserListView.vue'
```

### 11.2 计算属性缓存

```typescript
// ✅ 正确 — 使用 computed（有缓存）
const activeUsers = computed(() =>
  items.value.filter(u => u.isActive)
)

// ❌ 错误 — 使用方法（每次渲染重新计算）
function getActiveUsers() {
  return items.value.filter(u => u.isActive)
}
```

### 11.3 列表渲染优化

```vue
<!-- ✅ 正确 — 使用 key -->
<div v-for="user in users" :key="user.id">
  {{ user.username }}
</div>

<!-- ❌ 错误 — 缺少 key 或使用 index -->
<div v-for="(user, index) in users" :key="index">
```

### 11.4 第三方库按需引入

```typescript
// ✅ 正确 — ECharts 按需引入
import * as echarts from 'echarts/core'
import { LineChart } from 'echarts/charts'
import { GridComponent } from 'echarts/components'
import { CanvasRenderer } from 'echarts/renderers'

echarts.use([CanvasRenderer, LineChart, GridComponent])

// ❌ 错误 — 全量引入（增加 ~300KB 打包体积）
import * as echarts from 'echarts'
```

### 11.5 防抖与节流

```typescript
// ✅ 正确 — 搜索输入使用防抖
import { useDebounce } from '@/composables/useDebounce'

const searchQuery = ref('')
const debouncedSearch = useDebounce(searchQuery, 300)

watch(debouncedSearch, () => {
  usersStore.fetchUsers()
})
```

---

## 12. Git 提交规范

### 12.1 Commit Message 格式

```
<type>(<scope>): <description>

[optional body]
```

| Type | 说明 |
|------|------|
| `feat` | 新功能 |
| `fix` | Bug 修复 |
| `refactor` | 重构（不改变功能） |
| `style` | 样式/格式化变更 |
| `docs` | 文档变更 |
| `test` | 测试相关 |
| `chore` | 构建/工具变更 |
| `perf` | 性能优化 |

**示例**：
```
feat(users): 实现用户列表分页和搜索功能
fix(auth): 修复Token过期后无限重定向问题
refactor(api): 统一错误处理逻辑到拦截器
```

### 12.2 提交前检查

每一次 `git commit` 之前必须：

1. `bun run type-check` — 0 错误
2. `bun run format` — 代码格式化
3. 确认无 console.log（调试日志）
4. 确认无注释掉的代码块

---

## 13. 禁止事项清单

以下行为在项目中**严格禁止**：

| # | 禁止行为 | 原因 | 违规示例 |
|---|---------|------|---------|
| 1 | 使用 Options API | 架构规范 | `export default { data() {}, methods: {} }` |
| 2 | 在 View 中直接调用 `axios` 或 API 模块 | 违反分层架构 | `axios.get('/api/v1/users')` 出现在 View 中 |
| 3 | 在组件中硬编码颜色值 | 破坏设计一致性 | `style="color: #3B82F6"` |
| 4 | 在组件中硬编码间距值 | 破坏设计一致性 | `style="margin: 24px"` |
| 5 | 使用 `../../../` 相对路径 | 破坏可维护性 | `import { useAuth } from '../../../composables/useAuth'` |
| 6 | 使用 `any` 类型 | 破坏类型安全 | `function getData(): any {}` |
| 7 | 提交 `console.log` 调试代码 | 污染控制台 | `console.log('debug:', data)` |
| 8 | 提交被注释的代码块 | 污染代码库 | 大段被 `//` 注释的废弃代码 |
| 9 | 硬编码用户可见文案 | 破坏国际化 | `<h2>用户管理</h2>` |
| 10 | 跳过类型检查提交代码 | 破坏 CI/CD | 直接 `git commit` 跳过 `type-check` |
| 11 | 在 Store 中操作 DOM | 破坏关注点分离 | `document.title = 'xxx'` 在 Store 中 |
| 12 | 跨阶段开发 | 破坏开发节奏 | 在 Phase 2 中实现 Phase 5 的图表功能 |
| 13 | 开发仅桌面端可用的界面 | 违反多端适配要求 | 表格操作仅在宽屏下可用，移动端无法访问等效功能 |
| 14 | 在移动端隐藏核心功能 | 违反多端适配要求 | 使用 `hidden sm:block` 隐藏用户管理入口或关键操作按钮 |

---

## 附录 A：代码审查检查清单

每个 PR / MR 必须通过以下检查：

- [ ] 无 TypeScript 类型错误
- [ ] 无 ESLint 警告
- [ ] 所有文案已国际化（`$t()` 包裹）
- [ ] 无硬编码颜色/间距/动效值
- [ ] 所有 import 使用 `@/` 别名
- [ ] 所有 API 请求有完整类型
- [ ] 页面组件使用路由懒加载
- [ ] 无 `any` 类型
- [ ] 无 `console.log` 调试日志
- [ ] 无注释掉的代码
- [ ] Props 和 Emits 有 TypeScript 类型
- [ ] 命名符合规范
- [ ] 文件放在正确的目录
- [ ] 新功能有对应的语言包 key（中英文）
- [ ] 移动端（375px 视口）和桌面端（1920px 视口）两端的布局和功能均正常

## 附录 B：快速参考

```bash
# 类型检查
bun run type-check

# 代码格式化
bun run format

# 开发服务器
bun run dev

# 生产构建
bun run build

# 生产预览
bun run preview
```
