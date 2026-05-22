# Hysteria Auth Web 管理面板 — 开发阶段规划

> **文档版本**：v2.0  
> **最后更新**：2026-05-22  
> **关联文档**：[架构设计总览](../architect/README.md) · [核心模块设计](../architect/core-design.md) · [设计令牌规范](../architect/design-tokens.md) · [面板 API](../exchange/master-panel-api.md)

---

## 目录

- [阶段总览](#阶段总览)
- [Phase 0：基础设施搭建](./phase-0-infrastructure.md)
- [Phase 1：认证与布局框架](./phase-1-auth-layout.md)
- [Phase 2：用户管理模块](./phase-2-user-management.md)
- [Phase 3：节点管理模块](./phase-3-node-management.md)
- [Phase 4：仪表盘与管理功能](./phase-4-dashboard-admin.md)
- [Phase 5：图表可视化与数据导出](./phase-5-charts-export.md)
- [Phase 6：国际化完善与最终交付](./phase-6-i18n-delivery.md)

---

## 阶段总览

```
Phase 0         Phase 1          Phase 2          Phase 3          Phase 4           Phase 5          Phase 6
基础设施 ───► 认证+布局 ───► 用户管理 ───► 节点管理 ───► 仪表盘+管理 ───► 图表+导出 ───► i18n+交付

  ~2天           ~3天            ~3天            ~2天            ~3天             ~3天            ~2天
```

| 阶段 | 名称 | 核心交付物 | 详细文档 |
|------|------|-----------|----------|
| Phase 0 | 基础设施搭建 | 完整目录结构、类型体系、API 层、Store 骨架、路由框架、CSS 变量体系 | [→](./phase-0-infrastructure.md) |
| Phase 1 | 认证与布局框架 | 登录页、主布局（侧边栏+顶部栏）、认证守卫、权限守卫、主题切换 | [→](./phase-1-auth-layout.md) |
| Phase 2 | 用户管理模块 | 用户列表（分页/筛选/搜索）、用户详情、创建/编辑用户、删除用户、重置流量 | [→](./phase-2-user-management.md) |
| Phase 3 | 节点管理模块 | 节点列表、节点详情、预注册节点、轮换密钥、状态历史、在线管理 | [→](./phase-3-node-management.md) |
| Phase 4 | 仪表盘与管理功能 | 仪表盘概览、管理员管理（仅 super_admin）、审计日志 | [→](./phase-4-dashboard-admin.md) |
| Phase 5 | 图表可视化与数据导出 | ECharts 图表（流量趋势/CPU/内存/带宽）、Excel 导出、Excel 批量导入用户 | [→](./phase-5-charts-export.md) |
| Phase 6 | 国际化完善与最终交付 | 完整中英文语言包、UI 细节打磨、错误处理完善、构建优化 | [→](./phase-6-i18n-delivery.md) |

---

## 阶段间依赖关系图

```
Phase 0 ──────► Phase 1 ──────► Phase 2 ──────► Phase 3
                   │                               │
                   │                               │
                   └───────────┬───────────────────┘
                               │
                               ▼
                           Phase 4 ──────► Phase 5 ──────► Phase 6
```

### 依赖说明

| 依赖关系 | 强度 | 说明 |
|----------|------|------|
| **Phase 0 → Phase 1** | 🔴 强依赖 | Phase 1 的所有代码都依赖 Phase 0 创建的基础设施（类型、API 层、Store、路由框架、CSS 变量） |
| **Phase 1 → Phase 2** | 🔴 强依赖 | Phase 2 需要登录认证、布局框架、通用组件 |
| **Phase 2 → Phase 3** | 🟡 弱依赖 | 节点管理相对独立，但共享通用组件和模式 |
| **Phase 2/3 → Phase 4** | 🟠 中依赖 | 仪表盘需要用户和节点数据，管理员管理和审计日志相对独立 |
| **Phase 4 → Phase 5** | 🟠 中依赖 | 图表需要仪表盘、节点详情、用户详情的页面骨架已就绪 |
| **Phase 5 → Phase 6** | 🟡 弱依赖 | 国际化可并行推进，但建议在功能稳定后再统一提取文案 |

---

## 总体注意事项

1. **每个 Phase 的验证清单必须 100% 通过才能进入下一阶段**，不允许跳过验证
2. **所有阶段的代码必须通过 `bun run type-check`**，0 错误容忍
3. **移动端与桌面端双端适配**：从 Phase 1 开始，每个 Phase 交付的页面和组件必须同时在移动端（375px 视口）和桌面端（1920px 视口）下验证通过。布局框架（Phase 1）建立响应式基础后，后续所有阶段必须继承并遵循。具体要求见 [设计样式规范 §9](../develop/design-style-guide.md#9-响应式设计规范)
4. **Git 提交规范**：每个 Phase 完成后打 tag（如 `phase-0-done`），每个任务完成后提交
5. **Mock 数据策略**：如果后端 API 尚未就绪，在 `src/api/` 层使用 MSW (Mock Service Worker) 或 axios mock adapter 进行开发
6. **代码审查**：每个 Phase 的核心代码（Store、API 层、路由守卫）需经过审查再进入下一阶段
7. **不要跨阶段开发**：例如不要在 Phase 2 中提前实现 Phase 5 的图表功能

### 全局编码规范

| 规范 | 说明 |
|------|------|
| 组件语法 | 所有文件必须使用 `<script setup lang="ts">`，禁止使用 Options API |
| 导入别名 | 所有 import 使用 `@/` 别名，不要使用相对路径 `../../../` |
| 类型安全 | 禁止使用 `any`（除非有充分理由并注释说明） |
| 状态管理 | 全局状态使用 Pinia Store，组件本地状态使用 `ref`/`reactive` |
| HTTP 请求 | 所有 API 调用通过 `src/api/` 层，View 层不直接使用 axios |
