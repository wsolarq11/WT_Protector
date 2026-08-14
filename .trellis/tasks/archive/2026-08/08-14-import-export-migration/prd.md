# 配置导入导出与多机迁移

## Goal

让用户能把条目、启动组和设置导出为可审查文件，并在另一台机器导入时显式处理本机路径差异。目标不是云同步，而是“复制/迁移不丢配置”。

## Background（已确认事实）

- 当前所有状态在 `config/launcher.json` 一个文件中，已经是便携单文件，但路径是本机绝对路径，没有导入导出契约。
- `docs/feature-reality-north-star-audit.md` 明确把 import/export 记为 future-workload；friend handoff zip 已解决“给朋友空配置”，但没解决“带现有配置迁移”。
- `docs/giants-shoulders/launcher-first-class-patterns.md` 记录 Windows Terminal JSON Fragments 的 additive 外部配置教训，可作为导出格式参考。
- 导入必须保持现有统一 store 语义：entries + presets + settings + defaultPresetId 一起校验，不允许出现半写状态。

## Requirements

- R1 导出：用户可选择导出全部或当前选中条目；输出为可读 JSON，包含 entries、presets、settings（不导出本机窗口坐标/搜索历史可选）。
- R2 导入：从文件导入并做 schema/引用校验；冲突处理显式可见，不允许静默覆盖。
- R3 路径映射：导入时检测 `directory` 与当前机器不匹配的条目，提供“保持原路径 / 手动映射 / 跳过”的确认界面。
- R4 合并策略：可选“新增”或“替换”；重复 id 必须给用户选择，不做自动改名。
- R5 写盘仍走 `LauncherStore` + snapshot + rollback；导入失败时原配置不变。
- R6 UI 入口放在“管理”菜单，不新增第二个配置编辑界面。

## Acceptance Criteria

- [ ] 可导出一份通过现有 LauncherStoreFile schema 校验的 JSON。
- [ ] 可导入到空配置和已有配置；重复 id、失效 preset 引用、非法字段被拒绝并显示原因。
- [ ] 路径映射界面可把导入目录替换为本机路径，之后条目可启动。
- [ ] 导入中途失败时 `config/launcher.json` 保持导入前内容。
- [ ] 新增测试覆盖导出 schema、导入校验、冲突、路径映射和回滚；`tools/verify.ps1` 保持绿。

## Out of Scope

- 云同步、自动跨机同步、注册表/AppData 状态。
- 自动猜测路径、自动迁移目录。
- 插件市场或任意格式导入（只支持本项目 schema 版本）。

## Open Questions

- 导出文件是否应包含本机绝对路径，还是默认脱敏并要求导入时映射。
- 是否需要支持“导出不含 AI CLI 命令/敏感命令”的安全选项。