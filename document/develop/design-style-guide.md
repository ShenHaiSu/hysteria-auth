# Hysteria Auth Web 管理面板 — 样式/设计/用色方案强制规范

> **文档版本**：v1.0  
> **最后更新**：2026-05-22  
> **定位**：本规范为强制执行标准，定义全站视觉设计的单一事实来源。**任何视觉决策必须依据本文档，禁止在没有规范和标准的情况下直接开始代码编写。**
> **关联文档**：[设计令牌规范](../architect/design-tokens.md) · [前端开发规范](./frontend-standards.md) · [架构设计总览](../architect/README.md)

---

## 目录

- [0. 核心宪法](#0-核心宪法)
- [1. 色彩系统 — 完整用色方案](#1-色彩系统--完整用色方案)
- [2. 字体与排版规范](#2-字体与排版规范)
- [3. 间距与布局规范](#3-间距与布局规范)
- [4. 圆角与阴影规范](#4-圆角与阴影规范)
- [5. 动效与过渡规范](#5-动效与过渡规范)
- [6. 组件视觉规范](#6-组件视觉规范)
- [7. 页面布局规范](#7-页面布局规范)
- [8. 状态展示规范](#8-状态展示规范)
- [9. 响应式设计规范](#9-响应式设计规范)
- [10. 图标使用规范](#10-图标使用规范)
- [附录 A：色彩速查表](#附录-a色彩速查表)
- [附录 B：可视化组件对照](#附录-b可视化组件对照)

---

## 0. 核心宪法

> **规则 0.1** — 所有颜色必须引用 [`design-tokens.md`](../architect/design-tokens.md) 中定义的 CSS 变量或 TailwindCSS `@theme` 扩展，**禁止在任何 `.vue` 文件中硬编码颜色十六进制值**（`#xxxxxx`、`rgb()`、`hsl()`）。
>
> **规则 0.2** — 所有间距必须使用 TailwindCSS spacing scale（`p-4`、`m-6`、`gap-8`），**禁止硬编码 `px` 值**。唯一例外是 `variables.css` 中定义的 CSS 变量本身。
>
> **规则 0.3** — 所有动效/过渡必须使用 `variables.css` 中定义的 `--duration-*` 和 `--easing-*` 变量，**禁止硬编码过渡参数**。
>
> **规则 0.4** — 亮色/暗色双主题 100% 覆盖，**每个组件都必须在两种主题下视觉正确**。
>
> **规则 0.5** — 全局基准字号为 **13px**（`html { font-size: 13px }`），所有 TailwindCSS 尺寸类基于此基准计算。正文使用 `text-base`（14px）。
>
> **规则 0.6** — 所有用户可见的流量数据必须通过 [`AppTrafficText.vue`](../../src/components/common/AppTrafficText.vue) 组件展示，统一自动单位换算（bytes→KB/MB/GB/TB）。

---

## 1. 色彩系统 — 完整用色方案

### 1.1 色彩全景

本项目使用三层色彩架构：

```
┌─────────────────────────────────────────────────────────────┐
│                    色彩系统架构                               │
│                                                              │
│  第一层：品牌色 (Brand)   — 始终不变，通过 TailwindCSS        │
│         @theme 扩展 → bg-brand-500, text-brand-100           │
│                                                              │
│  第二层：语义色 (Surface) — 随亮/暗切换变化，通过 CSS 变量    │
│         :root / .dark → var(--bg-primary), var(--text-...)   │
│                                                              │
│  第三层：状态色 (Status)  — 具有语义含义，部分背景随主题变化  │
│         var(--status-active), var(--status-error-bg)         │
└─────────────────────────────────────────────────────────────┘
```

### 1.2 品牌色 — 蓝色系

品牌色为**全站唯一允许在 TailwindCSS class 中直接使用的颜色系列**。这些颜色不随亮/暗主题变化。

| TailwindCSS Class | 色值 | 使用场景 |
|-------------------|------|---------|
| `bg-brand-50` | `#EFF6FF` | 信息提示背景 |
| `bg-brand-100` | `#DBEAFE` | 选中行背景、tag 背景 |
| `bg-brand-500` | `#3B82F6` | **主按钮、主色强调** |
| `bg-brand-600` | `#2563EB` | 主按钮 hover |
| `bg-brand-700` | `#1D4ED8` | 主按钮 active |
| `text-brand-500` | `#3B82F6` | 链接文字、强调文字 |
| `text-brand-100` | `#DBEAFE` | 深色背景上的文字 |
| `border-brand-300` | `#93C5FD` | 聚焦边框 |
| `border-brand-500` | `#3B82F6` | 选中边框 |

### 1.3 语义色 — 亮色/暗色对照

**以下变量为布局级色彩的核心来源，所有组件必须使用这些变量，不得直接使用 TailwindCSS 内置 gray 系列**（如 `bg-gray-900`），因为内置色不会随主题切换。

| 用途 | CSS 变量 | 亮色值 | 暗色值 | 推荐 TailwindCSS Class |
|------|---------|--------|--------|----------------------|
| 页面主背景 | `var(--bg-primary)` | `#FFFFFF` | `#1F2937` | `bg-[var(--bg-primary)]` |
| 次级背景（卡片容器） | `var(--bg-secondary)` | `#F9FAFB` | `#111827` | `bg-[var(--bg-secondary)]` |
| 浮层/卡片背景 | `var(--bg-elevated)` | `#FFFFFF` | `#1F2937` | `bg-[var(--bg-elevated)]` |
| 主文字 | `var(--text-primary)` | `#111827` | `#F9FAFB` | `text-[var(--text-primary)]` |
| 次要文字 | `var(--text-secondary)` | `#6B7280` | `#9CA3AF` | `text-[var(--text-secondary)]` |
| 辅助/禁用文字 | `var(--text-muted)` | `#9CA3AF` | `#6B7280` | `text-[var(--text-muted)]` |
| 浅色边框 | `var(--border-light)` | `#E5E7EB` | `#374151` | `border-[var(--border-light)]` |
| 默认边框 | `var(--border-default)` | `#D1D5DB` | `#4B5563` | `border-[var(--border-default)]` |
| 分割线 | `var(--divider)` | `#F3F4F6` | `#374151` | `bg-[var(--divider)]` |

### 1.4 侧边栏专用色

侧边栏使用**独立色值体系**，不受主内容区亮/暗色变量影响。亮色模式下侧边栏始终为深色。

| 用途 | CSS 变量 | 亮色值 | 暗色值 | 推荐 TailwindCSS Class |
|------|---------|--------|--------|----------------------|
| 侧边栏背景 | `var(--sidebar-bg)` | `#1E293B` | `#0F172A` | `bg-[var(--sidebar-bg)]` |
| 侧边栏文字 | `var(--sidebar-text)` | `#CBD5E1` | `#94A3B8` | `text-[var(--sidebar-text)]` |
| 选中菜单文字 | `var(--sidebar-text-active)` | `#FFFFFF` | `#F1F5F9` | `text-[var(--sidebar-text-active)]` |
| 选中菜单背景 | `var(--sidebar-bg-active)` | `brand-600` | `brand-700` | `bg-[var(--sidebar-bg-active)]` |
| 菜单 hover | `var(--sidebar-bg-hover)` | `rgba(255,255,255,0.1)` | `rgba(255,255,255,0.08)` | `hover:bg-[var(--sidebar-bg-hover)]` |

### 1.5 状态色 — 语义含义

状态色背景随主题变化，但前景色（文字/图标）在亮暗下通用：

| 状态 | 前景色 CSS 变量 | 色值 | 背景色 CSS 变量 | 亮色背景 | 暗色背景 |
|------|---------------|------|---------------|---------|---------|
| 激活/在线/成功 | `var(--status-active)` | `#22C55E` | `var(--status-active-bg)` | `#F0FDF4` | `#052E16` |
| 禁用/离线 | `var(--status-inactive)` | `#9CA3AF` | `var(--status-inactive-bg)` | `#F9FAFB` | `#1F2937` |
| 待注册/处理中 | `var(--status-pending)` | `#F59E0B` | `var(--status-pending-bg)` | `#FFFBEB` | `#451A03` |
| 错误/危险 | `var(--status-error)` | `#EF4444` | `var(--status-error-bg)` | `#FEF2F2` | `#450A0A` |
| 警告 | `var(--status-warning)` | `#F59E0B` | `var(--status-warning-bg)` | `#FFFBEB` | `#451A03` |
| 信息提示 | `var(--status-info)` | `#3B82F6` | `var(--status-info-bg)` | `#EFF6FF` | `#172554` |

**状态标签模板**：
```html
<!-- 在线状态标签 -->
<span class="text-[var(--status-active)] bg-[var(--status-active-bg)] px-2 py-0.5 rounded-sm text-xs font-medium">
  ● 在线
</span>

<!-- 离线状态标签 -->
<span class="text-[var(--status-inactive)] bg-[var(--status-inactive-bg)] px-2 py-0.5 rounded-sm text-xs font-medium">
  ○ 离线
</span>
```

### 1.6 用色决策流程图

```
需要设置颜色？
    │
    ├─ 是品牌色相关（按钮、链接、强调）？
    │     → 使用 TailwindCSS brand 类：bg-brand-500, text-brand-600
    │
    ├─ 是背景/文字/边框颜色？
    │     → 使用 CSS 变量：bg-[var(--bg-primary)], text-[var(--text-secondary)]
    │
    ├─ 是侧边栏颜色？
    │     → 使用 CSS 变量：bg-[var(--sidebar-bg)], text-[var(--sidebar-text)]
    │
    ├─ 是状态标签颜色？
    │     → 使用 CSS 变量：text-[var(--status-active)], bg-[var(--status-active-bg)]
    │
    └─ 以上都不是？
          → 重新审视设计，确认是否需要自定义颜色。如确需，先在设计令牌中定义
```

---

## 2. 字体与排版规范

### 2.1 字体族

| Token | 值 | 应用 |
|-------|----|------|
| `font.family.sans` | `'Inter', 'Noto Sans SC', ui-sans-serif, system-ui, sans-serif` | **全站默认字体** |
| `font.family.mono` | `'JetBrains Mono', 'Fira Code', ui-monospace, monospace` | 代码、预注册令牌、密钥展示 |

**使用方式**：
- 全局默认通过 `variables.css` 中的 `--font-family-sans` 自动应用到 `body`
- 等宽字体手动应用：`class="font-mono"`（通过 TailwindCSS `@theme` 扩展）

### 2.2 字号阶梯

> ⚠️ **全局基准**：`html { font-size: 13px }`，以下 TailwindCSS class 均基于此基准计算实际像素。

| TailwindCSS Class | 实际字号 | 行高 | 使用场景 |
|-------------------|---------|------|---------|
| `text-xs` | 12px | 16px | 标签、Badge、辅助信息 |
| `text-sm` | **13px** | 18px | 表格内容、提示文字、菜单项 |
| `text-base` | 14px | 20px | **正文**（表单、描述、卡片内容） |
| `text-lg` | 16px | 24px | 副标题、统计数字 |
| `text-xl` | 18px | 28px | 小标题、对话框标题 |
| `text-2xl` | 20px | 28px | 页面标题 |
| `text-3xl` | 24px | 32px | 登录页标题 |

### 2.3 字重方案

| TailwindCSS Class | 字重 | 使用场景 |
|-------------------|------|---------|
| `font-normal` | 400 | 正文、描述文字 |
| `font-medium` | 500 | **重点文字、按钮文字、导航菜单** |
| `font-semibold` | 600 | 标题、卡片标题 |
| `font-bold` | 700 | 页面大标题、登录页品牌名 |

### 2.4 排版模式

```html
<!-- 页面标题 -->
<h1 class="text-2xl font-semibold text-[var(--text-primary)]">
  {{ $t('users.title') }}
</h1>

<!-- 卡片标题 -->
<h3 class="text-lg font-semibold text-[var(--text-primary)]">
  {{ $t('users.detail.basicInfo') }}
</h3>

<!-- 统计数字 -->
<span class="text-lg font-bold text-[var(--text-primary)]">
  1,234
</span>

<!-- 表格内容 -->
<span class="text-sm text-[var(--text-secondary)]">
  {{ user.email }}
</span>

<!-- 辅助信息 -->
<span class="text-xs text-[var(--text-muted)]">
  最后更新: 2026-05-22
</span>
```

### 2.5 文案对齐

- 表格数字列：**右对齐**（`text-right`）
- 表格文本列：**左对齐**（默认）
- 表格状态列：**居中对齐**（`text-center`）
- 操作按钮列：**居中对齐**（`text-center`）
- 统计卡片数值：**左对齐**
- 表单标签：**右对齐**（当 label 在左侧时）或**左对齐**（当 label 在上方时）

---

## 3. 间距与布局规范

### 3.1 间距阶梯

所有间距必须使用 TailwindCSS spacing scale，不得使用任意值 `p-[23px]` 等。

| TailwindCSS Class | 值 | 使用场景 |
|-------------------|----|---------|
| `p-1` / `gap-1` | 4px | 极小间距（图标与文字紧贴） |
| `p-2` / `gap-2` | 8px | 紧凑间距（表单字段内边距、Badge 内边距） |
| `p-3` / `gap-3` | 12px | 小间距（紧凑卡片 padding） |
| `p-4` / `gap-4` | 16px | **标准间距**（卡片之间、表单字段之间） |
| `p-5` / `gap-5` | 20px | 中等间距（卡片内边距） |
| `p-6` / `gap-6` | 24px | **页面级间距**（内容区内边距、区块之间） |
| `p-8` / `gap-8` | 32px | 大间距（页面主 padding） |

### 3.2 语义间距速查

| 场景 | 推荐间距 Class | 值 |
|------|---------------|-----|
| 页面内容区水平内边距 | `px-6` | 24px |
| 页面内容区垂直内边距 | `py-6` | 24px |
| 页面内区块间距 | `mb-6` 或 `gap-y-6` | 24px |
| 卡片内边距 | `p-5` | 20px |
| 卡片栅格间距 | `gap-4` | 16px |
| 表单字段间距 | `gap-4` | 16px |
| 表格单元格内边距 | PrimeVue DataTable 默认 | 10px/12px |
| 按钮内边距（默认） | `px-4 py-2` | 16px/8px |
| 对话框内边距 | `p-6` | 24px |

### 3.3 布局模式

**页面级布局**：
```html
<!-- 所有页面统一使用此布局结构 -->
<div class="p-6 space-y-6">
  <!-- 页面标题栏 -->
  <div class="flex items-center justify-between">
    <h1 class="text-2xl font-semibold text-[var(--text-primary)]">...</h1>
    <div class="flex items-center gap-3">...</div>
  </div>

  <!-- 筛选/搜索栏 -->
  <div class="flex flex-wrap items-center gap-4">...</div>

  <!-- 主内容区 -->
  <div class="bg-[var(--bg-elevated)] rounded-md border border-[var(--border-light)] p-5">
    ...
  </div>
</div>
```

**卡片栅格**：
```html
<!-- 统计卡片：4列 → 2列 → 1列 响应式 -->
<div class="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-4 gap-4">
  <StatCard />
  <StatCard />
  <StatCard />
  <StatCard />
</div>
```

**表单布局**：
```html
<!-- 标准表单：两列布局 -->
<div class="grid grid-cols-1 md:grid-cols-2 gap-4">
  <FormField label="用户名">
    <InputText />
  </FormField>
  <FormField label="邮箱">
    <InputText />
  </FormField>
</div>
```

---

## 4. 圆角与阴影规范

### 4.1 圆角

| TailwindCSS Class | 值 | 使用场景 |
|-------------------|----|---------|
| `rounded-sm` | 4px | 小元素：Badge、Tag、小按钮 |
| `rounded-md` | 6px | **默认圆角**：按钮、输入框、卡片、下拉菜单 |
| `rounded-lg` | 8px | 大卡片、对话框 |
| `rounded-xl` | 12px | 模态框 |
| `rounded-full` | 9999px | 胶囊形状、头像 |

**使用规则**：
- 同类组件圆角必须统一：所有卡片用 `rounded-md`，所有对话框用 `rounded-lg`
- PrimeVue 组件的圆角通过 `pt` 属性统一覆盖为 `rounded-md`

### 4.2 阴影

| TailwindCSS Class | 值 | 使用场景 |
|-------------------|----|---------|
| `shadow-sm` | `0 1px 2px 0 rgba(0,0,0,0.05)` | 微妙层级（表头固定） |
| `shadow-md` | `0 4px 6px -1px rgba(0,0,0,0.1)` | **卡片阴影** |
| `shadow-lg` | `0 10px 15px -3px rgba(0,0,0,0.1)` | 下拉菜单、Popover |
| `shadow-xl` | `0 20px 25px -5px rgba(0,0,0,0.1)` | 模态框遮罩之上 |

---

## 5. 动效与过渡规范

### 5.1 时长

| CSS 变量 | 值 | 使用场景 | TailwindCSS Class |
|----------|----|---------|-------------------|
| `--duration-fast` | 100ms | hover 颜色变化 | `duration-100` |
| `--duration-base` | 200ms | **默认过渡**（toggle、fade、切换） | `duration-200` |
| `--duration-slow` | 300ms | 侧边栏折叠、模态框进出、展开折叠 | `duration-300` |

### 5.2 缓动函数

| CSS 变量 | 使用场景 |
|----------|---------|
| `--easing-default` | **标准缓动**，适用于大部分过渡 |
| `--easing-decelerate` | 进场动画（元素出现） |
| `--easing-accelerate` | 退场动画（元素消失） |
| `--easing-emphasized` | 侧边栏折叠、路由过渡 |

### 5.3 预设动效

| 动效 | TailwindCSS Class 组合 | 使用场景 |
|------|----------------------|---------|
| **Fade** | `transition-opacity duration-200` | Toast、Tooltip 显隐 |
| **Color** | `transition-colors duration-100` | 按钮 hover、菜单项 hover |
| **Slide Down** | `transition-[max-height] duration-300` | 手风琴、折叠面板 |
| **Slide Left** | `transition-transform duration-300` | 侧边栏折叠 |
| **All** | `transition-all duration-200` | 下拉菜单、Popover |

### 5.4 路由过渡

```vue
<!-- App.vue 中的路由过渡 -->
<RouterView v-slot="{ Component }">
  <Transition name="page-fade" mode="out-in">
    <component :is="Component" />
  </Transition>
</RouterView>
```

```css
/* transition.css — 页面淡入淡出 */
.page-fade-enter-active,
.page-fade-leave-active {
  transition: opacity var(--duration-base) var(--easing-default);
}
.page-fade-enter-from,
.page-fade-leave-to {
  opacity: 0;
}
```

---

## 6. 组件视觉规范

### 6.1 按钮

| 类型 | 样式 | TailwindCSS Class |
|------|------|-------------------|
| 主按钮 | 品牌色背景 + 白色文字 | `bg-brand-500 hover:bg-brand-600 text-white rounded-md` |
| 次要按钮 | 透明背景 + 边框 | `border border-[var(--border-default)] text-[var(--text-primary)] rounded-md` |
| 危险按钮 | 红色背景 + 白色文字 | `bg-red-500 hover:bg-red-600 text-white rounded-md` |
| 文字按钮 | 无背景无边框 | `text-brand-500 hover:text-brand-600` |

**PrimeVue Button 统一 severity 映射**：
- `severity="primary"` → 品牌色（通过 PrimeVue 主题覆盖 `--p-primary-color: var(--brand-500)`）
- `severity="secondary"` → 次要按钮
- `severity="danger"` → 红色
- `severity="info"` → 品牌浅色
- `severity="success"` → 绿色
- `severity="warn"` → 琥珀色

### 6.2 输入框

```html
<!-- 标准输入框样式 -->
<InputText
  class="w-full"
  :class="{
    'border-[var(--border-default)]': !hasError,
    'border-[var(--status-error)]': hasError,
  }"
/>
```

- 聚焦边框：品牌色 `border-brand-500`（由 PrimeVue 默认行为）
- 错误边框：`border-[var(--status-error)]`
- 禁用状态：`opacity-60 cursor-not-allowed`

### 6.3 卡片

```html
<!-- 标准卡片 -->
<div class="bg-[var(--bg-elevated)] rounded-md border border-[var(--border-light)] shadow-md p-5">
  <!-- 卡片标题 -->
  <h3 class="text-lg font-semibold text-[var(--text-primary)] mb-4">...</h3>
  <!-- 卡片内容 -->
</div>
```

### 6.4 DataTable

使用 PrimeVue DataTable，统一样式覆盖：

- 表头背景：`bg-[var(--bg-secondary)]`
- 表头文字：`text-[var(--text-secondary)] text-sm font-medium`
- 表格行 hover：`hover:bg-[var(--bg-secondary)]`
- 表格行选中：`bg-brand-50`（亮色）/ `bg-brand-900`（暗色）
- 单元格内边距：默认
- 分页器：PrimeVue Paginator 默认样式

### 6.5 对话框

```html
<!-- 标准对话框（PrimeVue Dialog） -->
<Dialog
  v-model:visible="visible"
  :header="$t('users.form.createTitle')"
  class="w-full max-w-2xl"
  :pt="{
    root: { class: 'rounded-lg' },
    content: { class: 'bg-[var(--bg-elevated)]' },
  }"
>
  <!-- 内容 -->
  <template #footer>
    <Button :label="$t('common.actions.cancel')" severity="secondary" @click="visible = false" />
    <Button :label="$t('common.actions.save')" @click="handleSubmit" :loading="isSubmitting" />
  </template>
</Dialog>
```

### 6.6 Toast

- 位置：`top-right`
- 持续时间：成功 3000ms / 错误 5000ms
- 分组：全局使用默认 group

### 6.7 侧边栏

```html
<!-- 侧边栏菜单项 -->
<router-link
  v-for="item in menuItems"
  :key="item.name"
  :to="{ name: item.name }"
  class="flex items-center gap-3 px-4 py-3 text-[var(--sidebar-text)] hover:bg-[var(--sidebar-bg-hover)] transition-colors duration-100 rounded-md mx-2"
  :class="{ 'bg-[var(--sidebar-bg-active)] text-[var(--sidebar-text-active)]': isActive(item) }"
>
  <i :class="item.meta.icon" class="text-lg" />
  <span v-show="!sidebarCollapsed" class="text-sm font-medium">{{ item.meta.title }}</span>
</router-link>
```

---

## 7. 页面布局规范

### 7.1 整体布局

```
┌──────────────────────────────────────────────────────────┐
│ AppLayout                                                 │
│ ┌──────────┬─────────────────────────────────────────────┐│
│ │ Sidebar  │ Header (h-14)                               ││
│ │          ├─────────────────────────────────────────────┤│
│ │ w-60     │                                             ││
│ │          │ Content Area                                ││
│ │          │ (p-6 space-y-6)                             ││
│ │          │                                             ││
│ │          │                                             ││
│ │          │                                             ││
│ │          ├─────────────────────────────────────────────┤│
│ │          │ Footer                                      ││
│ └──────────┴─────────────────────────────────────────────┘│
└──────────────────────────────────────────────────────────┘
```

### 7.2 尺寸参数

| 元素 | 宽度 | 高度 |
|------|------|------|
| 侧边栏（展开） | `240px`（`w-60`） | 全屏高度 |
| 侧边栏（折叠） | `64px`（`w-16`） | 全屏高度 |
| 顶部栏 | 自适应 | `56px`（`h-14`） |
| 内容区 | 自适应 | `calc(100vh - 56px)` |

### 7.3 各页面标准结构

**列表页**：
```
┌──────────────────────────────────────────┐
│ [页面标题]                  [+ 创建按钮]  │
├──────────────────────────────────────────┤
│ [搜索框]  [筛选器1]  [筛选器2]  ...       │
├──────────────────────────────────────────┤
│ DataTable                                │
│                                           │
├──────────────────────────────────────────┤
│ Paginator                                │
└──────────────────────────────────────────┘
```

**详情页**：
```
┌──────────────────────────────────────────┐
│ [← 返回]  [页面标题]        [操作按钮们]  │
├──────────────────┬───────────────────────┤
│ 基本信息卡片      │ 流量统计 / 状态历史    │
│                  │                       │
│                  │  [图表区域]            │
│                  │                       │
└──────────────────┴───────────────────────┘
```

---

## 8. 状态展示规范

### 8.1 加载状态

| 场景 | 展示方式 | 说明 |
|------|---------|------|
| 页面首次加载 | 骨架屏（Skeleton）| 避免闪烁，使用 PrimeVue Skeleton |
| 表格数据加载 | DataTable loading 属性 | 表格内 Loading 遮罩 |
| 按钮操作进行中 | 按钮 loading 属性 | 禁用按钮 + 旋转图标 |
| 图表数据加载 | 半透明遮罩 + Spinner | 保持容器尺寸不变 |
| 全局操作（如登录） | 全屏 Loading | 阻止用户操作 |

### 8.2 空状态

```html
<!-- 列表无数据 -->
<div class="flex flex-col items-center justify-center py-12 text-[var(--text-muted)]">
  <i class="pi pi-inbox text-4xl mb-4" />
  <p class="text-lg font-medium">{{ $t('common.empty.title') }}</p>
  <p class="text-sm">{{ $t('common.empty.description') }}</p>
</div>
```

### 8.3 错误状态

```html
<!-- 数据加载失败 -->
<div class="flex flex-col items-center justify-center py-12 text-[var(--status-error)]">
  <i class="pi pi-exclamation-triangle text-4xl mb-4" />
  <p class="text-lg font-medium">{{ $t('common.error.loadFailed') }}</p>
  <Button :label="$t('common.actions.retry')" severity="secondary" class="mt-4" @click="retry" />
</div>
```

### 8.4 状态标签规范

使用 [`AppStatusBadge.vue`](../../src/components/common/AppStatusBadge.vue) 统一展示：

| 状态类型 | 标签文字 | 颜色 |
|---------|---------|------|
| 激活 | ● 激活 | `var(--status-active)` / `var(--status-active-bg)` |
| 禁用 | ○ 禁用 | `var(--status-inactive)` / `var(--status-inactive-bg)` |
| 在线 | ● 在线 | `var(--status-active)` / `var(--status-active-bg)` |
| 离线 | ○ 离线 | `var(--status-inactive)` / `var(--status-inactive-bg)` |
| 待注册 | ◉ 待注册 | `var(--status-pending)` / `var(--status-pending-bg)` |
| 已注册 | ● 已注册 | `var(--status-active)` / `var(--status-active-bg)` |

---

## 9. 响应式设计规范

### 9.1 断点策略

使用 TailwindCSS 默认断点，桌面优先（`min-width`）：

| 断点 | 宽度 | 策略 |
|------|------|------|
| 默认 | < 640px | 最小不崩溃 |
| `sm:` | ≥ 640px | 手机横屏 |
| `md:` | ≥ 768px | 平板竖屏 |
| `lg:` | ≥ 1024px | **侧边栏折叠临界点** |
| `xl:` | ≥ 1280px | 标准桌面 |
| `2xl:` | ≥ 1536px | 大屏 |

### 9.2 响应式规则

| 规则 | 实现 |
|------|------|
| 侧边栏 < 1024px 默认折叠 | `lg:w-60` 仅在 lg 以上展开 |
| 表格 < 768px 横向滚动 | `overflow-x-auto` |
| 仪表盘卡片栅格 | `grid-cols-1 sm:grid-cols-2 xl:grid-cols-4` |
| 表单两列 → 单列 | `grid-cols-1 md:grid-cols-2` |
| 搜索栏换行 | `flex flex-wrap` |
| 详情页图表区 | 始终 100% 宽度，内容自适应 |

---

## 10. 图标使用规范

### 10.1 图标库

- **全部使用 PrimeIcons**（`pi pi-xxx`），已随 PrimeVue 安装
- **禁止**引入其他图标库（如 Font Awesome、Material Icons）

### 10.2 图标尺寸

| 场景 | Class | 说明 |
|------|-------|------|
| 菜单图标 | `text-lg` | 侧边栏菜单项 |
| 按钮内图标 | `text-base` | 与按钮文字对齐 |
| 页面标题图标 | `text-xl` | 标题前缀 |
| 状态图标 | `text-base` | 空状态/错误状态 |
| 统计卡片图标 | `text-2xl` | 大号强调 |

### 10.3 菜单图标对照

| 菜单 | 图标 |
|------|------|
| 仪表盘 | `pi pi-home` |
| 用户管理 | `pi pi-users` |
| 节点管理 | `pi pi-server` |
| 管理员管理 | `pi pi-shield` |
| 审计日志 | `pi pi-history` |

---

## 附录 A：色彩速查表

### 亮色模式速查

| 元素 | 背景色 | 文字色 | 边框色 |
|------|--------|--------|--------|
| 页面主体 | `var(--bg-primary)` = `#FFFFFF` | `var(--text-primary)` = `#111827` | — |
| 卡片 | `var(--bg-elevated)` = `#FFFFFF` | `var(--text-primary)` | `var(--border-light)` = `#E5E7EB` |
| 表头 | `var(--bg-secondary)` = `#F9FAFB` | `var(--text-secondary)` = `#6B7280` | `var(--border-light)` |
| 侧边栏 | `var(--sidebar-bg)` = `#1E293B` | `var(--sidebar-text)` = `#CBD5E1` | — |
| 主按钮 | `bg-brand-500` = `#3B82F6` | `text-white` | — |
| 在线标签 | `var(--status-active-bg)` = `#F0FDF4` | `var(--status-active)` = `#22C55E` | — |
| 离线标签 | `var(--status-inactive-bg)` = `#F9FAFB` | `var(--status-inactive)` = `#9CA3AF` | — |

### 暗色模式速查

| 元素 | 背景色 | 文字色 | 边框色 |
|------|--------|--------|--------|
| 页面主体 | `var(--bg-primary)` = `#1F2937` | `var(--text-primary)` = `#F9FAFB` | — |
| 卡片 | `var(--bg-elevated)` = `#1F2937` | `var(--text-primary)` | `var(--border-light)` = `#374151` |
| 表头 | `var(--bg-secondary)` = `#111827` | `var(--text-secondary)` = `#9CA3AF` | `var(--border-light)` |
| 侧边栏 | `var(--sidebar-bg)` = `#0F172A` | `var(--sidebar-text)` = `#94A3B8` | — |
| 主按钮 | `bg-brand-500` = `#3B82F6` | `text-white` | — |
| 在线标签 | `var(--status-active-bg)` = `#052E16` | `var(--status-active)` = `#22C55E` | — |
| 离线标签 | `var(--status-inactive-bg)` = `#1F2937` | `var(--status-inactive)` = `#9CA3AF` | — |

---

## 附录 B：可视化组件对照

### 图表配色方案

ECharts 图表使用以下配色（与设计令牌体系一致）：

| 数据系列 | 亮色 | 暗色 | 说明 |
|---------|------|------|------|
| 系列 1（主数据） | `#3B82F6` | `#60A5FA` | 品牌蓝 |
| 系列 2（次要数据） | `#22C55E` | `#4ADE80` | 绿色 |
| 系列 3 | `#F59E0B` | `#FBBF24` | 琥珀色 |
| 系列 4 | `#EF4444` | `#F87171` | 红色 |
| 下载流量 | `#3B82F6` | `#60A5FA` | 品牌蓝 |
| 上传流量 | `#22C55E` | `#4ADE80` | 绿色 |
| CPU 使用率 | `#3B82F6` | `#60A5FA` | 品牌蓝 |
| 内存使用率 | `#F59E0B` | `#FBBF24` | 琥珀色 |
| 网络流入 | `#22C55E` | `#4ADE80` | 绿色 |
| 网络流出 | `#3B82F6` | `#60A5FA` | 品牌蓝 |
| 活跃连接数 | `#8B5CF6` | `#A78BFA` | 紫色 |
| 图表背景 | `var(--bg-elevated)` | — | CSS 变量 |
| 图表文字 | `var(--text-secondary)` | — | CSS 变量 |
| 网格线 | `var(--border-light)` | — | CSS 变量 |

### 统计卡片配色

| 卡片 | 图标背景色 | 图标色 |
|------|-----------|--------|
| 用户总数 | `bg-brand-100` | `text-brand-500` |
| 活跃用户 | `var(--status-active-bg)` | `var(--status-active)` |
| 在线用户 | `bg-brand-100` | `text-brand-500` |
| 节点总数 | `var(--status-info-bg)` | `var(--status-info)` |
| 活跃节点 | `var(--status-active-bg)` | `var(--status-active)` |
| 今日流量 | `bg-brand-100` | `text-brand-500` |
| 本月流量 | `var(--status-info-bg)` | `var(--status-info)` |
