# Execution Plan — Command Management Dialog

## Pre-flight

- [ ] 确认 `task.py current` 指向 `07-11-command-management-dialog`，status=in_progress。
- [ ] 基线：`tools/verify.ps1` 当前 PASS（508 测试，gate 绿）。
- [ ] 父任务 `07-11-centralize-declutter` 处于 planning 状态。

## Phase A: Core helper（无 UI 依赖，先打底）

1. **A1** — 新建 `src/avalonia-launcher/src/WTLauncher.Core/CommandUsageAnalyzer.cs`：
   - 静态类，纯函数。
   - `Analyze(IEnumerable<LaunchEntry> entries) → IReadOnlyList<CommandRow>` 去重 + 引用计数。
   - `AnalyzeTemplates(IImmutableDictionary<string, string> templates, IEnumerable<LaunchEntry> entries) → IReadOnlyList<TemplateRow>` 模板列表 + 引用计数。
   - 用 `StringComparer.OrdinalIgnoreCase` 去重。
2. **A2** — 加 `CommandRow` / `TemplateRow` record 到 `WTLauncher.Core`（与 LaunchEntry 同 namespace）。
3. **A3** — 单元测试 `CommandUsageAnalyzerTests`：
   - 空集合 → 空 list
   - 单 entry → 1 行（引用 = 1）
   - 多 entry 同 command → 1 行（引用 = N）
   - ordinal case-insensitive 验证
   - 模板列表 + `$codex` 引用计数 = 14 / `$claude` = 2 / `$opencode` = 1
4. **A4** — `dotnet build` 0 warning。

## Phase B: Edit record + Result 类型

5. **B1** — 新建 `src/avalonia-launcher/src/WTLauncher.Core/CommandManagementEdit.cs`：
   - 抽象 record `CommandManagementEdit` + 8 个 sealed 子类型（RenameCommand / MergeCommands / DeleteCommand / UpdateTemplateKey / UpdateTemplateValue / AddTemplate / DeleteTemplate）。
6. **B2** — 新建 `CommandManagementResult` record（Success / StatusMessage / UpdatedCommandRows / UpdatedTemplateRows）。
7. **B3** — 编译。

## Phase C: ViewModel + Dialog（UI 层）

8. **C1** — 新建 `src/avalonia-launcher/src/WTLauncher.App/ViewModels/CommandManagementViewModel.cs`：
   - `ObservableObject`，含 `ObservableCollection<CommandRowDisplay> CommandRows` + `ObservableCollection<TemplateRowDisplay> TemplateRows`。
   - `_activeTab` 字段（0 = 命令，1 = 模板）+ `ActiveTab` 属性 + 切换方法。
   - 构造接收：初始 command/template rows + `Func<CommandManagementEdit, CommandManagementResult>` editCallback。
   - 方法：`RenameCommand` / `MergeCommands` / `DeleteCommand` / `UpdateTemplateKey` / `UpdateTemplateValue` / `AddTemplate` / `DeleteTemplate` — 每个调 callback 然后刷新 rows。
   - `DisplayRow` 类包装 CommandRow/TemplateRow + IsSelected / IsDuplicateFlag。
9. **C2** — 新建 `src/avalonia-launcher/src/WTLauncher.App/Views/CommandManagementWindow.cs`：
   - 纯 C# code-behind Window，与 `DiscoverAgentsWindow` 同模式。
   - TabControl 两个 tab：命令引用 / 命令模板。
   - DataGrid 表格（Avalonia.Controls.DataGrid）。
   - 双击 → 弹 `CommandEditDialog`（新写，简陋 code-behind dialog）。
   - 右键 menu：编辑 / 合并 / 删除。
   - 快捷键：F2 编辑、Delete 删除、Ctrl+F 查重。
10. **C3** — 新建 `CommandEditDialog.cs`：根据 edit 类型显示不同字段（rename / merge / template-edit / template-add）。
11. **C4** — 编译。

## Phase D: Owner 集成（MainWindowViewModel）

12. **D1** — 新建 `MainWindowViewModel.CommandManagement.cs` partial：
    - 字段 `_openCommandManagementDialog` (Func<CommandManagementInput, CommandManagementResult?>) 默认返回 null。
    - 公共属性 `OpenCommandManagementDialog` (getter/setter, null-check)。
    - `[RelayCommand] OpenCommandManagement`：
      - 构造 `CommandManagementInput` (snapshot 当前的 command rows + template rows)。
      - 调 `_openCommandManagementDialog(input)` → 拿 edit sequence（dialog 内部循环）。
      - 对每个 edit：snapshot → mutate → save → 若失败 break + rollback。
      - 返回 result。
    - 内部方法 `ApplyCommandManagementEdit(CommandManagementEdit)` 处理每种 edit 类型 → mutate `_entries` / `_settings.CommandTemplates`。
13. **D2** — `MainWindow.axaml.cs`：
    - 注入 `viewModel.OpenCommandManagementDialog = input => { var window = new CommandManagementWindow(...); ...; return result; }`。
14. **D3** — `MainWindow.axaml`：
    - Cards 工具栏加 "管理" Button，Command=`{Binding OpenCommandManagementCommand}`，与"发现"按钮并列。
    - Content="管理"，Classes="secondary"。
15. **D4** — 编译。

## Phase E: 测试

16. **E1** — `MainWindowViewModelTests.CommandManagementTests.cs`：
    - 注入 mock callback，覆盖：
      - RenameCommand 单引用 → entry.command 更新
      - RenameCommand 多引用（模拟 14 codex）→ 全部更新
      - MergeCommands → 失效方 entry 引用更新到 keeper
      - DeleteCommand 引用 > 0 → 阻止
      - UpdateTemplateKey → 所有 entry.Command == `$oldKey` 自动更新
      - UpdateTemplateValue → entry.command 不变，只改模板字典
      - AddTemplate → 加到 _settings.CommandTemplates
      - DeleteTemplate 引用 > 0 → 阻止
      - DeleteTemplate 引用 = 0 → 删除
      - save 失败 → snapshot/rollback 还原 _settings.CommandTemplates
17. **E2** — 现有测试保持通过（504 基线 + 10 新 = 514）。
18. **E3** — `dotnet test` 全 PASS。

## Phase F: Gate + 收尾

19. **F1** — `tools/verify.ps1` 16 项全 PASS。
20. **F2** — Grep `catch \(Exception\)` 在新文件中 = 0 命中。
21. **F3** — 手动 smoke：构建 → 启动 → 打开命令管理 dialog → 验证 Tab 1 / Tab 2 内容 → 双击裸 `codex` 改成 `$codex` → 验证 entry.command 更新 + 重新启动 entry 立即吃到模板参数。
22. **F4** — 更新 `.trellis/spec/views/` 或 `core/` 相关 spec（如需）。
23. **F5** — Commit。

## Rollback Points

- Phase A/B 出问题 → 删 CommandUsageAnalyzer + edit records，无下游依赖。
- Phase C 出问题 → 删 ViewModel + Window，无下游依赖。
- Phase D 出问题 → revert MainWindowViewModel.CommandManagement.cs + MainWindow.axaml 改动。
- 整体出问题 → `git revert` 整个 commit。

## Validation Commands

```powershell
# Quick rebuild + test
dotnet build src/avalonia-launcher/WTLauncher.Avalonia.sln -c Release -warnaserror
dotnet test src/avalonia-launcher/WTLauncher.Avalonia.sln -c Release --filter "FullyQualifiedName~CommandUsageAnalyzer|FullyQualifiedName~CommandManagement"

# Full gate
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\verify.ps1

# Release build + smoke
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-release.ps1
```
