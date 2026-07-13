# Design

## Task shape

父任务只维护原始目标、子任务顺序和最终集成验收，不直接承载实现。

1. `07-13-codex-trellis-setup`：安装与接线事实审计、最小修复、可运行验证。
2. `07-13-exemplar-gap-refactor-audit`：复合典范定义、双方源码证据、差距矩阵与重构路线。

## Evidence model

- 当前状态优先于历史意图。
- 官方 CLI、官方 Codex 文档、当前配置和实际 hook 输出用于证明平台集成。
- codebase-memory 图、具体源码片段、测试和 XAML 用于证明代码/体验结论。
- 外部项目只提供可复用模式，不授权复制产品范围或依赖体系。

## Rollback

- Codex 接线修改局限于 `.codex/`；若 hook 造成启动问题，可删除新增事件注册恢复 Trellis 0.6.6 默认状态。
- 审计文档不改变产品运行时。
