# Research: Feature Audit Claims Verification

- **Query**: Verify docs/feature-reality-north-star-audit.md claims against actual code
- **Scope**: All 22 feature claims plus claim-boundary ledger
- **Date**: 2026-07-26

## 问题

核对 `docs/feature-reality-north-star-audit.md` 的 22 项功能声明与声明边界账本是否与代码一致。

## 结论

22 项功能均按审计所述实现；声明边界账本中的 forbidden 声明未在代码/README 出现；无违规。

## 关键证据

| # | 功能 | 证据 | 状态 |
|---|---|---|---|
| 1 | Launch entry object | `LaunchEntry.cs` 全字段 + `LaunchArgumentBuilder.Build` 生成 `wt.exe` 计划 | CONFIRMED |
| 2 | Existing-directory contract | `LaunchArgumentBuilder.Build` 校验目录存在（`validateDirectory: true`） | CONFIRMED |
| 3 | Search/filter | `LauncherEntryQuery.FilterAndSort` 多字段过滤 | CONFIRMED |
| 4 | Direct single launch | `LauncherLaunchWorkflow.cs`、`LaunchPlan.cs`（确认/取消、dry-run、`已请求启动`） | CONFIRMED |
| 5 | Persistent checkbox + batch | `IsSelected`、`StartBatch`（首批失败后中止） | CONFIRMED |
| 6 | Named groups / presets | `LauncherPreset.cs`、`LauncherStoreValidator.cs`、PresetManager | CONFIRMED |
| 7 | First-class preset actions | MainWindowViewModel partials + `locked` 字段 | CONFIRMED |
| 8 | Preset-vs-selection preview | PresetManager 派生，无第二真相源 | CONFIRMED |
| 9 | Preset launch preview | `PreviewSelectedPresetCommand` 只生成计划不 `Process.Start` | CONFIRMED |
| 10 | Add/edit/delete | MWVM partials + snapshot/rollback | CONFIRMED |
| 11 | Custom order + drag reorder | `LauncherDragReorderSession.cs`、`LauncherDragReorderWorkflow.cs`、MoveUp/MoveDown | CONFIRMED |
| 12 | Sort modes | `LauncherEntryQuery.cs` custom/updated/alphabetical | CONFIRMED |
| 13 | View modes | `LauncherEntriesView.axaml` 三段 + 三个只读投影 | CONFIRMED |
| 14 | Adaptive density | `LauncherEntryLayoutCalculator.cs` + `_layoutDensity` 单一 owner | CONFIRMED |
| 15 | Global launch confirm | `RequireConfirmationForLaunches`，无 per-entry `confirm` | CONFIRMED |
| 16 | Portable settings | `LauncherSettings.cs` 全字段，无 AppData/registry | CONFIRMED |
| 17 | JSON persistence/backup/rollback | `FileLauncherJsonIO.cs` temp + `File.Replace` + backup rotation | CONFIRMED |
| 18 | Core/App/Tests 分离 | 三项目 + ArchUnitNET 规则 | CONFIRMED |
| 19 | self-contained win-x64 + native gate | verify.ps1 多检查类别 | CONFIRMED |
| 20 | Retired legacy surfaces | 主线无恢复的 WinForms/HTA 路径 | CONFIRMED |
| 21 | Benchmark + giants-shoulders | `docs/design-benchmark-digest.md`、`docs/giants-shoulders/` 存在 | CONFIRMED |
| 22 | AI Agent CLI discovery | `AgentCatalog.cs`、`AgentDiscoveryScanner.cs`、`DiscoverAgentsWindow.cs` | CONFIRMED |

## Claim-Boundary Ledger

| Claim ID | 代码/README 现状 | 审计状态 |
|---|---|---|
| `narrow-domain-north-star` | README 仅声明 narrow domain | ALLOWED -- compliant |
| `narrow-slice-stronger` | README 描述具体功能 | ALLOWED -- compliant |
| `small-beautiful-preserved` | 无插件/生态扩张 | ALLOWED -- compliant |
| `pre-handoff-zero-side-effect-boundary` | 用 `已请求启动`，非 terminal complete | ALLOWED -- compliant |
| `broad-launcher-platform-north-star` | 无宽泛声明 | FORBIDDEN -- NOT made |
| `unimprovable-product` | 无 unimprovable 声明 | FORBIDDEN -- NOT made |
| `stronger-than-every-github-similar` | 无全面优越声明 | FORBIDDEN -- NOT made |
| `absolute-confidence-zero-side-effects` | README 明确列风险 | FORBIDDEN -- NOT made |
| `post-wt-terminal-health` | 明确非 terminal healthy | FORBIDDEN -- NOT made |

## 关联路径

- `docs/feature-reality-north-star-audit.md`
- `src/avalonia-launcher/src/WTLauncher.Core/LaunchEntry.cs`、`LaunchArgumentBuilder.cs`、`LauncherEntryQuery.cs`
- `src/avalonia-launcher/src/WTLauncher.App/Infrastructure/FileLauncherJsonIO.cs`
- `src/avalonia-launcher/tests/WTLauncher.Tests/`
