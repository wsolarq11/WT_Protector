Status: Archived
Purpose: 记录 2026-07-13 的 exemplar 差距与重构审计结论及推荐顺序。
Original scope: WT Launcher 自身五层/FMA 目标 + CodexSwitchUI 视觉模式 + 既有 benchmark 库。

# WT Launcher exemplar gap and refactor audit

## 结论

- 窄域安全契约已异常强：typed Core 规则、单一原子 JSON store、回滚式 mutation、启动 preflight/confirmation、架构测试、self-contained publish 与 GUI smoke。
- 最大缺口不是平台范围，而是增长中的一等公民功能仍收敛进少数超大 UI/orchestration 文件，抬高视觉一致性与例行功能成本。
- 正确 exemplar 是复合的：代码质量取本仓五层/FMA 目标，视觉质量取 CodexSwitchUI 组件/主题分离，运营质量取既有 launcher benchmark 库。
- CodexSwitch 不是全盘代码标杆：其 `MainWindowViewModel.cs` 实测 5291 行，只适合参考组件、主题、图标与打包模式。

## 关键证据

### 集中热点（2026-07-13 实测，行数已过时）

| Surface | 当日行数 | 含义 |
| --- | --- | --- |
| `MainWindow.axaml` | 992 | 三视图 + 资源 + 模板 + shell 同一变更面 |
| `CommandManagementWindow.cs` | 432 | 命令式组装视觉树，难主题化/测试 |
| `MainWindowViewModel.Presets.cs` | 473 | CRUD/apply/preview/launch/UI 状态同 owner |
| `MainWindowViewModel.Layout.cs` | 427 | 筛选/排序/尺寸/模式/滚动/持久化耦合 |
| `MainWindowViewModel.Batch.cs` | 338 | 选择与启动同 owner |
| Token 漂移 | — | 硬编码 margin/radius/字号/颜色仍存在 |

### 重构矩阵（P0 优先）

- CQ-01（P0）：完成 service 抽取，让 `MainWindowViewModel` 只做状态组合与命令委派。
- CQ-02（P0）：每个 mutation 家族收敛到一个 service 契约，`LauncherStore` 保持唯一磁盘权威。
- UX-01（P0）：完成 launch-count/frecency 排序。
- CQ-03/CQ-04（P1）：`CommandManagementWindow` 转声明式 XAML；`MainWindow.axaml` 按 toolbar/cards/details/names/status 四缝拆分。
- V-01（P1）：硬编码值提升为资源 ownership，门禁禁止 approved 之外的 raw hex。

## 推荐顺序

1. 先完成 FMA Phase 2-3，再做 central management、frecency、directory-scan。
2. 再转 `CommandManagementWindow` 为 XAML，删重复 raw 视觉值。
3. 然后拆分 `MainWindow.axaml`，做 accessibility/responsive 审计，收紧 service 边界的 typed 失败结果。
4. 仅在有证据时再加 icon set、first-run onboarding 或会话 receipt。

## 明确拒绝

无插件市场/扩展运行时；无跨平台打包；无新数据库或隐藏 AppData；无 CodexSwitchUI 依赖；无 Native AOT 强制要求；无第四视图模式或 dashboard。

## 核验注记（2026-08-15）

- snapshot → mutate → save → rollback 描述为审计日状态；快照类已于 2026-07-21 移除，rollback 现由 `StoreGrain.Dispatch`（save-first-then-commit）承担。
- 集中热点行数均已变化：`MainWindow.axaml` 现 341 行，`CommandManagementWindow` 已拆为 `.axaml`（168）+ `.axaml.cs`（158）。

## 关联路径

- 设计契约：`DESIGN.md`
- 功能审计：`docs/feature-reality-north-star-audit.md`
