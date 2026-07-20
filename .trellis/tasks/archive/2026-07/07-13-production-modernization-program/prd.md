# 生产级现代化全阶段落地

## Goal

在不扩大 WT Launcher 产品边界的前提下，落地 `docs/exemplar-gap-refactor-audit.md` 中全部 17 项非延期改造，使架构、核心工作流、视觉一致性、无障碍和响应式证据达到可持续维护的生产级标准。

## Confirmed facts

- `config/launcher.json` 是 entries、presets、settings 与 `defaultPresetId` 的唯一权威源；不得引入 AppData、注册表或数据库后备。
- `LauncherStore` 已提供原子保存、备份恢复与错误归类，必须继续作为唯一磁盘写入权威。
- 当前主要维护热点是大型 ViewModel partial、大型 XAML 和命令管理窗口的命令式视觉树，而不是缺少新的平台能力。
- 既有任务已经覆盖部分实现工作；本计划的子任务是实施与验收所有者，必须复用既有实现，不得重复建设。
- 用户已明确授权创建并连续实施全部非延期项；需要取舍时采用仓库证据和现代 Windows/Avalonia/WCAG 最佳实践，无需逐项再次确认。

## Scope and task map

| ID | Child task | Required outcome | Existing work to reuse |
| --- | --- | --- | --- |
| CQ-01 | `07-13-cq01-thin-main-viewmodel` | ViewModel 仅保留状态组合与命令委托 | `07-12-fma-p2-services`, `07-12-fma-p3-thin-vm` |
| CQ-02 | `07-13-cq02-mutation-transaction-boundary` | 统一 snapshot/mutate/save/rollback 边界 | `07-12-fma-p2-services` |
| CQ-03 | `07-13-cq03-command-management-xaml` | 命令管理稳定布局迁移到 XAML | `07-11-centralize-declutter` |
| CQ-04 | `07-13-cq04-mainwindow-xaml-modularity` | 按已证明的视图接缝拆分主窗口 XAML | CQ-01、CQ-03 产物 |
| CQ-05 | `07-13-cq05-typed-status-content` | 状态文案由类型化 formatter 统一拥有 | `07-12-fma-p4-quality` |
| CQ-06 | `07-13-cq06-service-typed-results` | 仅在需要分支的服务边界引入 typed result | `07-12-fma-p4-quality` |
| CQ-07 | `07-13-cq07-service-component-contract-tests` | 为抽取后的公共行为补契约测试 | FMA 与质量任务 |
| V-01 | `07-13-v01-visual-token-convergence` | 收敛重复视觉值并阻止 raw hex 漂移 | 现有 design token 资源 |
| V-02 | `07-13-v02-main-ui-declutter` | 完成管理迁移与主界面操作降噪 | `07-11-centralize-declutter` |
| V-03 | `07-13-v03-dialog-consistency` | 对话框间距、按钮和危险操作语义一致 | 现有 dialog primitives |
| V-04 | `07-13-v04-accessibility-production-audit` | 焦点、名称、对比度、DPI、动效形成生产证据 | `07-12-fma-p4-quality` |
| V-06 | `07-13-v06-responsive-visual-matrix` | 窄/默认/宽/高 DPI 响应式矩阵验真 | 现有布局计算测试与 GUI smoke |
| UX-01 | `07-13-ux01-frecency-ranking` | 完成无分析噪声的常用项排序 | `07-11-entry-launch-count-frecency` |
| UX-02 | `07-13-ux02-central-management` | 集中管理增删改合并与主列表即时闭环 | `07-11-centralize-declutter` |
| UX-03 | `07-13-ux03-existing-directory-quick-create` | 已有目录到命令模板再到条目的一条路径 | `07-11-directory-scan-wizard` |
| UX-04 | `07-13-ux04-contextual-feedback` | 保留状态栏权威并增加克制的就地反馈 | CQ-05、V-03 产物 |
| UX-05 | `07-13-ux05-first-run-empty-state` | 空配置提供添加、发现、配置路径和组说明 | 现有添加/发现命令 |

## Requirements

- R1：按依赖顺序实施：CQ-02 → CQ-01/CQ-06 → CQ-07 → UX-01..04 → CQ-03/CQ-04/CQ-05/V-01..03 → V-04/V-06/UX-05。
- R2：每个子任务必须拥有可独立验证的 PRD；复杂任务在启动前还必须有 `design.md` 与 `implement.md`。
- R3：既有任务与新子任务不得产生平行实现；新子任务引用既有任务成果，并以缺口关闭和验收为职责。
- R4：保持 Core 零 Avalonia 依赖、单一 JSON 权威源、原子持久化、失败回滚、启动前验证与确认策略等现有不变量。
- R5：遵循 ponytail/YAGNI：优先删除、复用和平台原生能力；不新增未被验收标准要求的抽象、依赖或产品范围。
- R6：所有非平凡逻辑至少留下一个可运行的聚焦检查；UI 样式行为以结构测试、GUI smoke 和视觉矩阵共同证明。
- R7：每个子任务完成后执行 Trellis 质量检查、必要的 spec 更新、独立提交和归档；父任务最后执行跨任务集成审计。

## Acceptance Criteria

- [ ] AC1：17 个子任务全部完成、独立验证、提交并归档，父任务显示 17/17 done。
- [ ] AC2：三个延期项 CQ-08、V-05、UX-06 未被偷偷实现，且没有插件市场、跨平台包装、数据库、AppData/registry 后备、Native AOT 强制或新视图模式等范围扩张。
- [ ] AC3：CQ-01..07 的架构与契约目标均有代码和测试证据，不以文件移动或 partial 数量减少冒充所有权下降。
- [ ] AC4：V-01..04、V-06 与 UX-01..05 均有对应运行时或人工验真证据，声明范围与证据强度一致。
- [ ] AC5：最终工作树在提交后干净，`git diff --check` 通过。
- [ ] AC6：从干净 HEAD 运行 `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\verify.ps1` 全部通过，包括测试、self-contained publish 与 GUI smoke。

## Out of scope

- CQ-08 性能硬阈值，除非本轮发现可复现回退。
- V-05 图标集，待视觉降噪完成后以真实可扫描性证据重评。
- UX-06 回执历史持久化，待支持案例证明需要后再设计。
- 审计文档中“Explicitly rejected or deferred”列出的所有平台和产品扩张。
