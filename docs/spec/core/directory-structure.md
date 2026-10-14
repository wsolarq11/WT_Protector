# Directory Structure — Core Layer

> 5 层架构：每层只依赖下层。结构事实（文件数 / partial / 测试 / gate 步数）单一源于 `AGENTS.md` 结构基线 + `structure-baseline-up-to-date`；本文件不硬编码漂移数字。

## 5 层

| 层 | 位置 | 职责 |
|----|------|------|
| 0 Core | `WTLauncher.Core/` | 纯数据/规则，零 Avalonia/Mvvm 依赖 |
| 1 Command mutations | `ViewModels/MainWindowViewModel*.cs` | validate → mutate → save |
| 2 Persistence + rollback | `ViewModels` + `Infrastructure/` | `StoreGrain.Dispatch` save-first-then-commit |
| 3 Orchestration | `MainWindowViewModel*.cs` | 单 UI 状态所有者 |
| 4 Views | `Views/` | XAML + code-behind + drag-drop |

## Layer 0 — Core

关键文件：`LaunchEntry.cs`、`LaunchPlan.cs`、`LauncherPreset.cs`、`LauncherSettings.cs`、`LauncherStoreFile.cs`；`LaunchArgumentBuilder.cs`（wt.exe 引号）；`LauncherJson.cs`（原子写 + AOT source-gen）；`LauncherSettingsDefaults.cs`（缺失可选键归一）；`LauncherDesignTokens.cs`（单一 token 源）；`LauncherStore.cs`（加载/校验/迁移）；纯规则 `LauncherEntryOrder` / `LauncherEntryLayoutCalculator` / `LauncherDragGhostLayoutCalculator` / `LauncherEntryQuery` / `LauncherQuickMatch` / `LauncherEntryGrouper`；目录与发现 `DirectoryPath` / `AgentDiscoveryScanner` / `AgentCatalog` / `AgentCatalogEntry` / `AgentBrandMatcher`；运行历史与更新 `LauncherRunHistory` / `LauncherUpdateManifest` / `LauncherHotkey`；视图模式枚举 `LauncherEntryViewMode.cs`（Cards/Details/Names）。

规则：Core 禁止引用 `Avalonia.*`、`CommunityToolkit.Mvvm`；想加则逻辑应上移 ViewModels。

## Layer 1 — 命令本地 mutation

命令处理拥有 validate → mutate → save；共享规则留在窄 helper（`LauncherEntryDrafts`、`LauncherEntryReferences`、`Features/Entries/EntryListViewModel`）。单一调用方不建第二个 VM workflow。

## Layer 2 — 持久化 + 回滚

`Infrastructure/`：`StoreGrain`（单状态容器，全部 mutation 走 `Dispatch`/`DispatchErrorOr`，分析器强制唯一写路径）；`FileStorePersistence`（实现 `IStorePersistence`，Save 有实例 `SemaphoreSlim(1,1)`）；`FileLauncherJsonIO`（实现 `ILauncherJsonIO`）。回滚 = save-first-then-commit：reducer 结果先落盘再入内存，保存失败内存不变。存储为单文件 `config/launcher.json`。

## Layer 3 — 编排

`MainWindowViewModel` = 1 主文件 + 15 partials（Batch / CommandManagement / DirectoryManagement / Discovery / Drag / Entries / Grouping / History / IEntryCardHost / IEntryListViewModel / Layout / Migration / Presets / Reorder / Update）。单一可写 UI 状态所有者；可复用规则委托 Core；dialog `Func<>` 经 `MainWindow` 构造注入。进一步拆分走组合（`LaunchOrchestrator`、`PresetManager`、`AppStateMachine`、`Features/Entries/EntryListViewModel`、`RunHistoryViewModel`），不再加 partial。

## Layer 4 — Views

`Views/`：XAML 三视图（Cards/Details/Names）、drag ghost + insert 指示器、code-behind 仅窗口化。完整清单见 [Views / Directory Structure](../views/directory-structure.md)。

## Naming

- 文件/partial：PascalCase；partial = `MainWindowViewModel.<Area>.cs`。
- 测试：`<TypeUnderTest>Tests.cs` 或 `<TypeUnderTest>Tests.<Area>Tests.cs`。
- 命名空间：`WTLauncher.Core`、`WTLauncher.App.ViewModels` / `WTLauncher.App.Views` / `WTLauncher.App.Features`、`WTLauncher.Tests`。

## Where to Put New Code

| 新增 | 位置 |
|------|------|
| 无 UI 依赖的纯数据/规则 | `WTLauncher.Core/` |
| 校验/规范化 | Core（纯）或 ViewModels helper |
| 用户命令 | `MainWindowViewModel.cs` 或 `MainWindowViewModel.<Area>.cs` |
| 自带 VM + 投影的 feature | `WTLauncher.App/Features/<Feature>/` |
| Entry 级 VM 属性 | `LauncherEntryViewModel.cs` / `.Layout.cs` / `.Drag.cs` |
| 对话框窗口 | `Views/`（每对话框一文件 + `.axaml`） |
| XAML 样式 | `App.axaml` resources 或局部 `Styles` |
