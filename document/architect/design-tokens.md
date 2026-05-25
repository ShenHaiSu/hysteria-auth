# 设计令牌规范 (Design Tokens)

> **所属文档**: [架构设计总览](README.md)  
> **最后更新**: 2026-05-22  
> **用途**: 定义全站统一的设计变量体系，作为 CSS 自定义属性、TailwindCSS 扩展、PrimeVue 主题覆盖的单一事实来源。

---

## 目录

- [1. 概述与命名规范](#1-概述与命名规范)
- [2. 色彩系统](#2-色彩系统)
- [3. 间距系统](#3-间距系统)
- [4. 字体与排版](#4-字体与排版)
- [5. 圆角与阴影](#5-圆角与阴影)
- [6. 动效系统](#6-动效系统)
- [7. 断点与响应式](#7-断点与响应式)
- [8. CSS 变量实现](#8-css-变量实现)

---

## 1. 概述与命名规范

### 1.1 设计令牌的价值

本规范确保全站视觉一致性，任何视觉决策都应从本文档中找到对应令牌，**严禁**在组件中硬编码颜色值或间距值。所有令牌通过 CSS 自定义属性 + TailwindCSS `@theme` 扩展同时生效，亮/暗色主题通过 `.dark` class 切换变量值。

### 1.2 Token 命名规范

```
{category}[-{property}][-{variant}][-{state}][.{scale}]

示例:
  color.bg.primary          → 主背景色
  color.text.muted           → 次要文字色
  color.border.light         → 浅色边框
  color.status.active        → 激活状态色
  color.status.online.text   → 在线状态文字色
  spacing.section.gap        → 区域间距
  font.size.body             → 正文字号
  radius.component           → 组件圆角
  shadow.card                → 卡片阴影
  motion.transition.base     → 基础过渡时长
  motion.easing.default      → 默认缓动函数
```

---

## 2. 色彩系统

### 2.1 主色系 (Brand Colors)

本项目的品牌色采用蓝色系，与 Hysteria 项目的技术属性匹配。

| Token | 色值 | TailwindCSS 别名* | 用途 |
|-------|------|-------------------|------|
| `color.brand.50` | `#EFF6FF` | `brand-50` | 最浅背景 |
| `color.brand.100` | `#DBEAFE` | `brand-100` | 浅色背景 / tag |
| `color.brand.200` | `#BFDBFE` | `brand-200` | hover 浅色 |
| `color.brand.300` | `#93C5FD` | `brand-300` | 边框 / 分隔 |
| `color.brand.400` | `#60A5FA` | `brand-400` | 次要强调 |
| `color.brand.500` | `#3B82F6` | `brand-500` | **主色 (Primary)** |
| `color.brand.600` | `#2563EB` | `brand-600` | hover 主色 |
| `color.brand.700` | `#1D4ED8` | `brand-700` | active 主色 |
| `color.brand.800` | `#1E40AF` | `brand-800` | 深色背景上 |
| `color.brand.900` | `#1E3A8A` | `brand-900` | 最深 |

> *通过 TailwindCSS `@theme` 扩展注入，可在 `class` 中直接使用：`bg-brand-500 text-brand-100`。

### 2.2 中性色系 (Neutral / Surface)

| Token | 亮色值 | 暗色值 | 用途 |
|-------|--------|--------|------|
| `color.bg.primary` | `#FFFFFF` | `#1F2937` (gray-800) | 主背景 |
| `color.bg.secondary` | `#F9FAFB` | `#111827` (gray-900) | 次级背景 |
| `color.bg.elevated` | `#FFFFFF` | `#1F2937` | 浮层/卡片背景 |
| `color.bg.overlay` | `rgba(0,0,0,0.5)` | `rgba(0,0,0,0.7)` | 模态框遮罩 |
| `color.text.primary` | `#111827` (gray-900) | `#F9FAFB` (gray-50) | 主文字 |
| `color.text.secondary` | `#6B7280` (gray-500) | `#9CA3AF` (gray-400) | 次要文字 |
| `color.text.muted` | `#9CA3AF` (gray-400) | `#6B7280` (gray-500) | 辅助/禁用文字 |
| `color.text.inverse` | `#FFFFFF` | `#111827` | 反色文字 |
| `color.border.light` | `#E5E7EB` (gray-200) | `#374151` (gray-700) | 浅色边框 |
| `color.border.default` | `#D1D5DB` (gray-300) | `#4B5563` (gray-600) | 默认边框 |
| `color.divider` | `#F3F4F6` (gray-100) | `#374151` (gray-700) | 分割线 |

### 2.3 语义色系 (Semantic / Status)

| Token | 色值 (亮/暗通用) | 用途 |
|-------|-----------------|------|
| `color.status.active` | `#22C55E` (green-500) | 激活/在线/成功 |
| `color.status.active.bg` | `#F0FDF4` / `#052E16` | 激活背景 |
| `color.status.inactive` | `#9CA3AF` (gray-400) | 禁用/离线 |
| `color.status.inactive.bg` | `#F9FAFB` / `#1F2937` | 禁用背景 |
| `color.status.pending` | `#F59E0B` (amber-500) | 待注册/处理中 |
| `color.status.pending.bg` | `#FFFBEB` / `#451A03` | 待处理背景 |
| `color.status.error` | `#EF4444` (red-500) | 错误/危险 |
| `color.status.error.bg` | `#FEF2F2` / `#450A0A` | 错误背景 |
| `color.status.warning` | `#F59E0B` (amber-500) | 警告 |
| `color.status.warning.bg` | `#FFFBEB` / `#451A03` | 警告背景 |
| `color.status.info` | `#3B82F6` (blue-500) | 信息提示 |

### 2.4 Sidebar 专用色

侧边栏使用独立的色值体系，以区分于主内容区。

| Token | 亮色值 | 暗色值 | 用途 |
|-------|--------|--------|------|
| `color.sidebar.bg` | `#1E293B` (slate-800) | `#0F172A` (slate-900) | 侧边栏背景 |
| `color.sidebar.text` | `#CBD5E1` (slate-300) | `#94A3B8` (slate-400) | 侧边栏文字 |
| `color.sidebar.text.active` | `#FFFFFF` | `#F1F5F9` (slate-100) | 选中菜单文字 |
| `color.sidebar.bg.active` | `color.brand.600` | `color.brand.700` | 选中菜单背景 |
| `color.sidebar.bg.hover` | `rgba(255,255,255,0.1)` | `rgba(255,255,255,0.08)` | hover 菜单背景 |
| `color.sidebar.border` | `rgba(255,255,255,0.1)` | `rgba(255,255,255,0.05)` | 侧边栏分割线 |

---

## 3. 间距系统

### 3.1 基础间距阶梯 (4px 基准)

采用 4px 基准等比阶梯，与 TailwindCSS 默认 spacing scale 保持一致：

| Token | 值 | TailwindCSS | 典型用途 |
|-------|----|------------|---------|
| `spacing.0` | `0` | `0` | 无边距 |
| `spacing.1` | `4px` | `1` | 极小间距（图标与文字） |
| `spacing.2` | `8px` | `2` | 紧凑间距（表单内边距） |
| `spacing.3` | `12px` | `3` | 小间距（卡片 padding） |
| `spacing.4` | `16px` | `4` | **基础间距**（标准 padding） |
| `spacing.5` | `20px` | `5` | 中等间距 |
| `spacing.6` | `24px` | `6` | 宽松间距（区域间隔） |
| `spacing.8` | `32px` | `8` | 大间距（页面 main padding） |
| `spacing.10` | `40px` | `10` | 更大间距 |
| `spacing.12` | `48px` | `12` | 特大间距 |

### 3.2 语义间距

| Token | 值 | 用途 |
|-------|----|------|
| `spacing.page.padding.x` | `24px` (spacing.6) | 页面内容区水平内边距 |
| `spacing.page.padding.y` | `24px` (spacing.6) | 页面内容区垂直内边距 |
| `spacing.section.gap` | `24px` (spacing.6) | 页面内 section 之间的间距 |
| `spacing.card.padding` | `20px` (spacing.5) | 卡片内边距 |
| `spacing.card.gap` | `16px` (spacing.4) | 卡片之间的栅格间距 |
| `spacing.form.field.gap` | `16px` | 表单字段之间的间距 |
| `spacing.table.cell.padding.y` | `10px` | 表格单元格垂直内边距 |
| `spacing.table.cell.padding.x` | `12px` | 表格单元格水平内边距 |
| `spacing.sidebar.width` | `240px` | 侧边栏展开宽度 |
| `spacing.sidebar.collapsed` | `64px` | 侧边栏折叠宽度 |
| `spacing.header.height` | `56px` | 顶部栏高度 |

---

## 4. 字体与排版

### 4.1 字体族

| Token | 值 | 用途 |
|-------|----|------|
| `font.family.sans` | `'Inter', 'Noto Sans SC', ui-sans-serif, system-ui, sans-serif` | 默认无衬线字体 |
| `font.family.mono` | `'JetBrains Mono', 'Fira Code', ui-monospace, monospace` | 代码/数字等宽字体 |

> 通过 Google Fonts 或自托管引入 `Inter` (拉丁) 和 `Noto Sans SC` (简体中文)，确保中英文均有无衬线一致体验。

### 4.2 字号与行高

| Token | 字号 | 行高 | TailwindCSS | 用途 |
|-------|------|------|-------------|------|
| `font.size.xs` | `12px` | `16px` (1.333) | `text-xs` | 辅助文字、标签 |
| `font.size.sm` | `13px` | `18px` (1.385) | `text-sm` | 表格内容、提示文字 |
| `font.size.base` | `14px` | `20px` (1.429) | `text-base` | **正文**（全局默认） |
| `font.size.lg` | `16px` | `24px` (1.5) | `text-lg` | 副标题、重点文字 |
| `font.size.xl` | `18px` | `28px` (1.556) | `text-xl` | 小标题 |
| `font.size.2xl` | `20px` | `28px` (1.4) | `text-2xl` | 页面标题 |
| `font.size.3xl` | `24px` | `32px` (1.333) | `text-3xl` | 登录页标题 |

> **全局基准字号**: `html { font-size: 13px }`（已在 [`public.css`](../../src/assets/styles/public.css) 中设置）。这使得 `text-sm` (13px) 与基准一致，`text-base` (14px) 用于正文。

### 4.3 字重

| Token | 值 | TailwindCSS | 用途 |
|-------|----|-------------|------|
| `font.weight.normal` | `400` | `font-normal` | 正文 |
| `font.weight.medium` | `500` | `font-medium` | 强调文字、按钮 |
| `font.weight.semibold` | `600` | `font-semibold` | 标题 |
| `font.weight.bold` | `700` | `font-bold` | 页面大标题 |

---

## 5. 圆角与阴影

### 5.1 圆角

| Token | 值 | TailwindCSS | 用途 |
|-------|----|-------------|------|
| `radius.none` | `0` | `rounded-none` | 无边角 |
| `radius.sm` | `4px` | `rounded-sm` | 小元素（tag / badge） |
| `radius.md` | `6px` | `rounded-md` | **默认**（按钮、输入框、卡片） |
| `radius.lg` | `8px` | `rounded-lg` | 大卡片、对话框 |
| `radius.xl` | `12px` | `rounded-xl` | 模态框 |
| `radius.full` | `9999px` | `rounded-full` | 胶囊 / 头像 |

### 5.2 阴影

| Token | 值 | 用途 |
|-------|----|------|
| `shadow.none` | `none` | 默认无阴影 |
| `shadow.sm` | `0 1px 2px 0 rgba(0,0,0,0.05)` | 微妙层级 |
| `shadow.md` | `0 4px 6px -1px rgba(0,0,0,0.1), 0 2px 4px -2px rgba(0,0,0,0.1)` | **卡片阴影** |
| `shadow.lg` | `0 10px 15px -3px rgba(0,0,0,0.1), 0 4px 6px -4px rgba(0,0,0,0.1)` | 下拉菜单 |
| `shadow.xl` | `0 20px 25px -5px rgba(0,0,0,0.1), 0 8px 10px -6px rgba(0,0,0,0.1)` | 模态框 |

---

## 6. 动效系统

### 6.1 过渡时长

| Token | 值 | 用途 |
|-------|----|------|
| `motion.duration.instant` | `0ms` | 即时（无动画） |
| `motion.duration.fast` | `100ms` | 微交互（hover 颜色变化） |
| `motion.duration.base` | `200ms` | **默认过渡**（toggle、fade、切换） |
| `motion.duration.slow` | `300ms` | 展开/折叠（sidebar、collapse） |
| `motion.duration.modal` | `300ms` | 模态框进出 |

### 6.2 缓动函数

| Token | 值 | 用途 |
|-------|----|------|
| `motion.easing.default` | `cubic-bezier(0.4, 0, 0.2, 1)` | **标准缓动** (Material 标准) |
| `motion.easing.decelerate` | `cubic-bezier(0.0, 0, 0.2, 1)` | 进场动画（减速停止） |
| `motion.easing.accelerate` | `cubic-bezier(0.4, 0, 1, 1)` | 退场动画（加速消失） |
| `motion.easing.emphasized` | `cubic-bezier(0.4, 0, 0.4, 1)` | 强调动画 |

### 6.3 预设动效模式

基于 TailwindCSS 的 `transition-*` 体系，定义统一动效类：

| 动效 Pattern | 实现 | 使用场景 |
|-------------|------|---------|
| **Fade** | `transition-opacity duration-200 ease-default` | 元素显隐、tooltip |
| **Scale Fade** | `transition-all duration-200 ease-decelerate` | 下拉菜单、popover |
| **Slide Down** | `transition-[max-height] duration-300 ease-default` | 折叠面板、手风琴 |
| **Slide Left** | `transition-transform duration-300 ease-emphasized` | 侧边栏折叠 |
| **Background Color** | `transition-colors duration-100 ease-default` | hover 状态、菜单项 |
| **Page Transition** | ~~`<Transition name="page-fade" mode="out-in">`~~ → 已禁用 | 路由切换（框架 bug，改用顶部加载浮窗） |

### 6.4 路由过渡动画（已禁用）

> **已禁用**：路由级别 `<Transition name="page-fade" mode="out-in">` 因框架内部 bug（vue-router 异步组件 + `mode="out-in"` 导致组件卸载异常）已移除。详见 [`design-style-guide.md §5.4`](../develop/design-style-guide.md#54-路由过渡已禁用--顶部加载浮窗替代)。

**替代方案 — 顶部加载浮窗**：

在 `transition.css` 中保留以下动画定义，供路由加载浮窗使用：

```css
/* src/assets/styles/transition.css */

/* 路由加载浮窗：滑入/滑出 */
.loading-bar-slide-enter-active {
  transition: transform 100ms cubic-bezier(0.0, 0, 0.2, 1);
}
.loading-bar-slide-leave-active {
  transition: transform 200ms cubic-bezier(0.4, 0, 1, 1);
}
.loading-bar-slide-enter-from,
.loading-bar-slide-leave-to {
  transform: translateY(-100%);
}

/* loading-bar 扫光动画 */
@keyframes loading-bar-indeterminate {
  0%   { transform: translateX(-100%); }
  100% { transform: translateX(100%); }
}
.animate-loading-bar {
  animation: loading-bar-indeterminate 1.2s cubic-bezier(0.4, 0, 0.2, 1) infinite;
}

/* 列表进入：逐项滑入 (配合 TransitionGroup) */
.list-enter-active {
  transition: all 300ms cubic-bezier(0.0, 0, 0.2, 1);
}
.list-enter-from {
  opacity: 0;
  transform: translateY(-8px);
}
```

---

## 7. 断点与响应式

### 7.1 断点定义

沿用 TailwindCSS 默认断点，满足管理后台宽屏优先设计：

| Token | 值 | TailwindCSS | 适用场景 |
|-------|----|-------------|---------|
| `breakpoint.sm` | `640px` | `sm:` | 仅最小窗口不崩溃 |
| `breakpoint.md` | `768px` | `md:` | 平板竖屏 |
| `breakpoint.lg` | `1024px` | `lg:` | 平板横屏 / 小笔记本 |
| `breakpoint.xl` | `1280px` | `xl:` | 标准桌面 |
| `breakpoint.2xl` | `1536px` | `2xl:` | 大屏显示器 |

### 7.2 响应式策略

> **强制要求**：本项目必须同时适配**移动端**（≥375px 视口）和**桌面端**（≥1280px 视口）两种主要界面大小。以下策略为全局默认行为，所有页面和组件必须遵循。

- **桌面优先** (`min-width`)：管理后台以桌面端为主要使用场景，移动端为完整功能体验的第二场景
- 侧边栏在 `< 1024px` 时默认隐藏，通过汉堡按钮触发覆盖式抽屉展开，不挤压内容区
- 表格在 `< 768px` 时切换为横向滚动（`overflow-x-auto`）或卡片列表视图
- 仪表盘卡片栅格：`grid-cols-2`（移动端）→ `md:grid-cols-3`（平板）→ `xl:grid-cols-4`（桌面端）
- 表单布局：移动端始终单列（`grid-cols-1`），桌面端可选双列（`md:grid-cols-2`）
- 对话框/弹窗：移动端全宽（`max-w-full`），桌面端限制最大宽度（`max-w-2xl`）
- 图表高度：移动端 240px～280px（简化图例），桌面端 320px～400px（完整图例）
- 所有可交互元素在移动端必须满足最小 44×44px 触摸区域

### 7.3 移动端 vs 桌面端语义断点映射

| 端 | 断点范围 | 语义标记 | 典型设备 |
|----|---------|---------|---------|
| 移动端 | 默认～`sm:`（< 640px） | `--viewport-mobile` | 智能手机竖屏 |
| 移动端宽屏 | `sm:`～`md:`（640～768px） | — | 智能手机横屏 |
| 平板端 | `md:`～`lg:`（768～1024px） | `--viewport-tablet` | 平板竖屏 |
| 桌面端过渡 | `lg:`～`xl:`（1024～1280px） | — | 小笔记本 |
| 桌面端 | `xl:`+（≥1280px） | `--viewport-desktop` | 标准桌面显示器 |
| 大屏桌面 | `2xl:`+（≥1536px） | — | 大屏/超宽显示器 |

---

## 8. CSS 变量实现

### 8.1 入口文件 (`src/assets/styles/variables.css`)

```css
/* src/assets/styles/variables.css */

/* ===== 品牌色 (亮暗通用) ===== */
:root {
  --brand-50: #EFF6FF;
  --brand-100: #DBEAFE;
  --brand-200: #BFDBFE;
  --brand-300: #93C5FD;
  --brand-400: #60A5FA;
  --brand-500: #3B82F6;
  --brand-600: #2563EB;
  --brand-700: #1D4ED8;
  --brand-800: #1E40AF;
  --brand-900: #1E3A8A;
}

/* ===== 亮色主题 ===== */
:root {
  --bg-primary: #FFFFFF;
  --bg-secondary: #F9FAFB;
  --bg-elevated: #FFFFFF;
  --text-primary: #111827;
  --text-secondary: #6B7280;
  --text-muted: #9CA3AF;
  --border-light: #E5E7EB;
  --border-default: #D1D5DB;
  --divider: #F3F4F6;

  /* Sidebar */
  --sidebar-bg: #1E293B;
  --sidebar-text: #CBD5E1;
  --sidebar-text-active: #FFFFFF;
  --sidebar-bg-active: var(--brand-600);
  --sidebar-bg-hover: rgba(255,255,255,0.1);
  --sidebar-border: rgba(255,255,255,0.1);

  /* Status */
  --status-active: #22C55E;
  --status-active-bg: #F0FDF4;
  --status-inactive: #9CA3AF;
  --status-inactive-bg: #F9FAFB;
  --status-pending: #F59E0B;
  --status-pending-bg: #FFFBEB;
  --status-error: #EF4444;
  --status-error-bg: #FEF2F2;
  --status-warning: #F59E0B;
  --status-warning-bg: #FFFBEB;
  --status-info: #3B82F6;
  --status-info-bg: #EFF6FF;
}

/* ===== 暗色主题 ===== */
.dark {
  --bg-primary: #1F2937;
  --bg-secondary: #111827;
  --bg-elevated: #1F2937;
  --text-primary: #F9FAFB;
  --text-secondary: #9CA3AF;
  --text-muted: #6B7280;
  --border-light: #374151;
  --border-default: #4B5563;
  --divider: #374151;

  /* Sidebar */
  --sidebar-bg: #0F172A;
  --sidebar-text: #94A3B8;
  --sidebar-text-active: #F1F5F9;
  --sidebar-bg-active: var(--brand-700);
  --sidebar-bg-hover: rgba(255,255,255,0.08);
  --sidebar-border: rgba(255,255,255,0.05);

  /* Status (背景加深) */
  --status-active-bg: #052E16;
  --status-inactive-bg: #1F2937;
  --status-pending-bg: #451A03;
  --status-error-bg: #450A0A;
  --status-warning-bg: #451A03;
  --status-info-bg: #172554;
}

/* ===== 排版 ===== */
:root {
  --font-sans: 'Inter', 'Noto Sans SC', ui-sans-serif, system-ui, sans-serif;
  --font-mono: 'JetBrains Mono', 'Fira Code', ui-monospace, monospace;
  --font-size-xs: 12px;
  --font-size-sm: 13px;
  --font-size-base: 14px;
  --font-size-lg: 16px;
  --font-size-xl: 18px;
  --font-size-2xl: 20px;
  --font-size-3xl: 24px;
}

/* ===== 圆角 ===== */
:root {
  --radius-sm: 4px;
  --radius-md: 6px;
  --radius-lg: 8px;
  --radius-xl: 12px;
}

/* ===== 动效 ===== */
:root {
  --duration-fast: 100ms;
  --duration-base: 200ms;
  --duration-slow: 300ms;
  --easing-default: cubic-bezier(0.4, 0, 0.2, 1);
  --easing-decelerate: cubic-bezier(0.0, 0, 0.2, 1);
  --easing-accelerate: cubic-bezier(0.4, 0, 1, 1);
  --easing-emphasized: cubic-bezier(0.4, 0, 0.4, 1);
}
```

### 8.2 使用方式

**在 CSS 中使用自定义属性**（`<style scoped>` 中）：

```css
.custom-card {
  background-color: var(--bg-elevated);
  color: var(--text-primary);
  border: 1px solid var(--border-light);
  border-radius: var(--radius-md);
  transition: box-shadow var(--duration-base) var(--easing-default);
}
.custom-card:hover {
  box-shadow: 0 4px 6px -1px rgba(0,0,0,0.1);
}
```

**在 TailwindCSS 中结合使用**：

```html
<!-- 布局级色彩 -->
<div class="bg-[var(--bg-primary)] text-[var(--text-primary)]">
  <!-- 品牌色 TailwindCSS 扩展 -->
  <Button class="bg-brand-500 hover:bg-brand-600 text-white rounded-md" />
  <!-- 状态色 -->
  <span class="text-[var(--status-active)] bg-[var(--status-active-bg)] px-2 py-1 rounded-sm text-sm">
    在线
  </span>
</div>
```

### 8.3 TailwindCSS `@theme` 扩展

```css
/* src/assets/styles/variables.css (追加) */

@theme {
  /* 品牌色系 */
  --color-brand-50: #EFF6FF;
  --color-brand-100: #DBEAFE;
  --color-brand-200: #BFDBFE;
  --color-brand-300: #93C5FD;
  --color-brand-400: #60A5FA;
  --color-brand-500: #3B82F6;
  --color-brand-600: #2563EB;
  --color-brand-700: #1D4ED8;
  --color-brand-800: #1E40AF;
  --color-brand-900: #1E3A8A;

  /* 字体 */
  --font-family-sans: 'Inter', 'Noto Sans SC', ui-sans-serif, system-ui, sans-serif;
  --font-family-mono: 'JetBrains Mono', 'Fira Code', ui-monospace, monospace;
}
```

> **注意**：TailwindCSS `@theme` 扩展仅覆盖**不随主题变化的**值（如品牌色、字体）。随亮/暗色变化的值（如 `--bg-primary`）必须使用 CSS 自定义属性并通过 `.dark` 覆盖。

---
