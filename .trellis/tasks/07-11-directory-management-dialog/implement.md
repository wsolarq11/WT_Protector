# Execution Plan — Directory Management Dialog

## Pre-flight

- [ ] 确认 `task.py current` 指向 `07-11-directory-management-dialog`,status=in_progress。
- [ ] 基线: `tools/verify.ps1` 当前 PASS(530 测试)。
- [ ] Child 3 已归档(`07-11-command-management-dialog` 在 archive/2026-07/)。

## Phase A: Core helper(records + analyzer)

1. **A1** — 新建 `src/avalonia-launcher/src/WTLauncher.Core/DirectoryUsageAnalyzer.cs`:
   - `DirectoryRow` record (Directory, ReferenceCount, ReferencingEntryNames)
   - 静态类 `DirectoryUsageAnalyzer.Analyze(IEnumerable<LaunchEntry>) → IReadOnlyList<DirectoryRow>`
   - `CanDeleteDirectory(string directory, IEnumerable<LaunchEntry>) → bool`
   - 用 `StringComparer.OrdinalIgnoreCase` 去重(Windows 路径 case-insensitive)
2. **A2** — 新建 `src/avalonia-launcher/src/WTLauncher.Core/DirectoryManagementEdit.cs`:
   - 抽象 record `DirectoryManagementEdit` + 3 个 sealed 子类型(RenameDirectory / MergeDirectories / DeleteDirectory)
   - `DirectoryManagementResult` record
3. **A3** — 单元测试 `DirectoryUsageAnalyzerTests`:
   - 空集合 → 空 list
   - 单 entry → 1 行(引用 = 1)
   - 多 entry 同 directory → 1 行(引用 = N)
   - ordinal case-insensitive 验证(`D:\Foo` ≡ `d:\foo`)
4. **A4** — `dotnet build` 0 warning。

## Phase B: ViewModel + Dialog

5. **B1** — 新建 `src/avalonia-launcher/src/WTLauncher.App/ViewModels/DirectoryManagementViewModel.cs`:
   - `ObservableObject`,`ObservableCollection<DirectoryRow> DirectoryRows`
   - `SelectedDirectoryRow` 属性
   - `StatusMessage` 属性
   - 方法: `RenameDirectory` / `MergeDirectories` / `DeleteDirectory` — 调 `_applyEdit` callback 然后刷新 rows
6. **B2** — 新建 `src/avalonia-launcher/src/WTLauncher.App/Views/DirectoryManagementWindow.cs`:
   - 纯 C# code-behind,与 `CommandManagementWindow` 同模式
   - 单 tab + StackPanel 行渲染(无 DataGrid 依赖)
   - 工具栏按钮: 编辑/重命名、合并、删除
   - 双击 → 弹 `DirectoryEditDialog`
7. **B3** — 新建 `src/avalonia-launcher/src/WTLauncher.App/Views/DirectoryEditDialog.cs`:
   - 单字段(directory)+ "📁 浏览..." 按钮 → `OpenFolderDialog`
   - 显示"将影响 N 个 entry"提示
8. **B4** — 编译。

## Phase C: Owner 集成(MainWindowViewModel + MainWindow)

9. **C1** — 新建 `MainWindowViewModel.DirectoryManagement.cs` partial:
    - `_openDirectoryManagementDialog` (Func<Func<DirectoryManagementEdit, DirectoryManagementResult>, Task>) 默认 no-op
    - `OpenDirectoryManagementDialog` 公共属性(getter/setter, null-check)
    - `[RelayCommand] OpenDirectoryManagementAsync`
    - `ApplyDirectoryManagementEdit(edit)` → snapshot/mutate/save/rollback
10. **C2** — `MainWindow.axaml.cs`:
    - 注入 `viewModel.OpenDirectoryManagementDialog = async applyEdit => { ... }`
11. **C3** — `MainWindow.axaml`:
    - Cards 工具栏加"📁 地址" Button,Command=`{Binding OpenDirectoryManagementCommand}`,与"管理"按钮并列
12. **C4** — 编译。

## Phase D: 测试

13. **D1** — `MainWindowViewModelTests.DirectoryManagementTests.cs`:
    - 注入 mock callback,覆盖:
      - RenameDirectory 单引用 → entry.directory 更新
      - RenameDirectory 多引用 → 全部更新
      - MergeDirectories → 失效方 entry.directory 更新
      - DeleteDirectory 引用 > 0 → 阻止(VM 层)
      - save 失败 → snapshot/rollback
14. **D2** — 现有测试保持通过(530 + 8 = 538)。
15. **D3** — `dotnet test` 全 PASS。

## Phase E: Gate + 收尾

16. **E1** — `tools/verify.ps1` 全 PASS。
17. **E2** — Grep `catch \(Exception\)` 在新文件中 = 0 命中。
18. **E3** — 手动 smoke: 启动 → 打开地址管理 dialog → 双击重复 directory 改一个 → 验证 2 个 entry.directory 都更新 + 启动 entry 仍正常。
19. **E4** — Commit。
20. **E5** — `task.py archive 07-11-directory-management-dialog`。

## Rollback Points

- Phase A 出问题 → 删 DirectoryUsageAnalyzer + edit records,无下游依赖。
- Phase B 出问题 → 删 ViewModel + Window,无下游依赖。
- Phase C 出问题 → revert MainWindowViewModel.DirectoryManagement.cs + MainWindow.axaml 改动。
- 整体出问题 → `git revert` 整个 commit。

## Validation Commands

```powershell
dotnet build src/avalonia-launcher/WTLauncher.Avalonia.sln -c Release -warnaserror
dotnet test src/avalonia-launcher/WTLauncher.Avalonia.sln -c Release --filter "FullyQualifiedName~DirectoryUsageAnalyzer|FullyQualifiedName~DirectoryManagement"
powershell -NoProfile -ExecutionPolicy Bypass -File tools/verify.ps1
```
