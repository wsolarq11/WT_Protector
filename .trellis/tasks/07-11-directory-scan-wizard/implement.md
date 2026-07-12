# Implement — Discovery 扫描向导扩展

## 实施顺序

### Step 1 — Core 新 record

**文件（新增）**：
- `src/avalonia-launcher/src/WTLauncher.Core/ExistingDirectoryRow.cs`
- `src/avalonia-launcher/src/WTLauncher.Core/DirectoryWizardResult.cs`

或合并到单个文件 `src/avalonia-launcher/src/WTLauncher.Core/DiscoveryWizardRecords.cs`（ponytail：少文件）。

**改动**：写两个 record（design.md 已示）。

**验证**：
```bash
dotnet build src/avalonia-launcher/WTLauncher.Avalonia.sln -warnaserror
```

### Step 2 — ViewModel 扩展

**文件**：
- `src/avalonia-launcher/src/WTLauncher.App/ViewModels/DiscoverAgentsViewModel.cs`

**改动**：
1. 新增 `DiscoveryMode` enum（顶部）。
2. 添加 ObservableProperty：`Mode` / `SelectedDirectory` / `SelectedCommandTemplateKey` / `EntryName` / `CommandPreview`。
3. 添加 `ExistingDirectories: ObservableCollection<ExistingDirectoryRow>` + `CommandTemplateKeys: IReadOnlyList<string>`。
4. 构造函数新增 3 参数：`existingDirectories` / `commandTemplateKeys` / `expandCommandTemplate`。
5. 添加 commands：`SelectDirectoryCommand` / `SelectCommandTemplateCommand`。
6. 添加 partial OnXxxChanged 触发 `OnPropertyChanged(nameof(CanCreateFromDirectory))` 和 `RefreshCommandPreview()`。
7. 添加 `CanCreateFromDirectory` 属性 + `GetDirectoryWizardResult()` 方法。
8. 添加 `DefaultNameFromDirectory` 静态辅助。

**验证**：
```bash
dotnet build src/avalonia-launcher/WTLauncher.Avalonia.sln -warnaserror
```

### Step 3 — ViewModel 单元测试

**文件（新增）**：
- `src/avalonia-launcher/tests/WTLauncher.Tests/DiscoverAgentsViewModelDirectoryModeTests.cs`

**测试用例**：
1. `Constructor_DirectoryPickMode_ListsExistingDirectoriesSortedByRefCountDesc`
2. `SelectDirectory_SetsSelectedAndFillsDefaultEntryName`（验证文件夹名提取）
3. `SelectCommandTemplate_UpdatesCommandPreview`
4. `CanCreateFromDirectory_FalseUntilBothSelected`
5. `GetDirectoryWizardResult_ReturnsExpectedRecord`
6. `GetDirectoryWizardResult_NullWhenCanCreateFalse`
7. `DefaultNameFromDirectory_HandlesTrailingSeparator`
8. `SwitchMode_DoesNotResetOtherModeState`（PATH 模式扫描结果在切到目录模式再切回后仍存在）

**验证**：
```bash
dotnet test src/avalonia-launcher/WTLauncher.Avalonia.sln -c Release --filter "FullyQualifiedName~DiscoverAgentsViewModelDirectoryMode"
```

### Step 4 — View 扩展

**文件**：
- `src/avalonia-launcher/src/WTLauncher.App/Views/DiscoverAgentsWindow.cs`

**改动**：
1. 顶部添加 mode RadioButton 组（`pathRadioButton` + `directoryRadioButton`，同 GroupName）。
2. 现有 PATH 模式 panel 包装为 `pathPanel`（含 Rows + ScanButton + StatusText）。
3. 新建 `directoryPanel`（含 ExistingDirectories scroll list + command ComboBox + EntryName TextBox + CommandPreview TextBlock）。
4. 确认按钮文字根据 mode 动态切换。
5. 确认按钮 Click 处理：PATH 模式 Close(viewModel.GetSelected())，目录模式 Close(viewModel.GetDirectoryWizardResult())。
   - 实际上 Close 参数 owner 自己取：design.md ADR-5 选了 owner 查 VM.Mode 方案，dialog Close(true/false?) 表示确认/取消。
   - 具体形态：dialog 关闭返回 `bool?`（true=确认）；owner 检查 VM.Mode 决定调哪个 Get 方法。

**验证**：
```bash
dotnet build src/avalonia-launcher/WTLauncher.Avalonia.sln -warnaserror
```

### Step 5 — Owner 集成

**文件**：
- `src/avalonia-launcher/src/WTLauncher.App/ViewModels/MainWindowViewModel.cs`（或 `MainWindowViewModel.Discovery.cs` 若存在）

**改动**：
1. `RequestDiscoverAgentsAsync`（或等价 factory）扩展：构造 VM 时多传 3 个参数（existingDirectories / commandTemplateKeys / expandCommandTemplate）。
2. dialog Close 后判断 `_viewModel.Mode`：
   - `PathScan` → 走现有 `GetSelected()` → `AddDiscoveredAgentsAsync` 路径。
   - `DirectoryPick` → 走新 `AddEntryFromDirectoryWizard` 路径。
3. 新增 `AddEntryFromDirectoryWizard(DirectoryWizardResult result)` 方法：复用现有 AddEntry mutation（snapshot → new LaunchEntry → save → rollback）。
4. 新增 `ExpandCommandTemplate(string key, string directory)` 辅助（或复用现有 `LaunchArgumentBuilder.ResolveCommandTemplate` 之类）。

**验证**：
```bash
dotnet build src/avalonia-launcher/WTLauncher.Avalonia.sln -warnaserror
```

### Step 6 — Owner 集成测试

**文件**：
- `src/avalonia-launcher/tests/WTLauncher.Tests/MainWindowViewModelTests.DiscoveryWizardTests.cs`（新增）

**测试用例**：
1. `DirectoryPick_AddEntry_PersistsAndRollsBackOnFailure`
2. `DirectoryPick_CommandTemplateExpansion_PreviewMatchesLaunchArgumentBuilder`
3. `DirectoryPick_NameDefaultsToFolderName_WhenUserDoesNotEdit`
4. `DirectoryPick_Cancel_DoesNotMutateEntries`

**验证**：
```bash
dotnet test src/avalonia-launcher/WTLauncher.Avalonia.sln -c Release --filter "FullyQualifiedName~DiscoveryWizard"
```

### Step 7 — 完整 gate

**命令**：
```bash
powershell -NoProfile -ExecutionPolicy Bypass -File ./tools/verify.ps1
```

**期望**：16 检查全绿；测试数应增加 ≥12 条（Step 3 的 8 + Step 6 的 4）。

## 风险点与回滚锚

| 风险 | 触发条件 | 回滚 |
|------|---------|------|
| RadioButton group 与现有 dialog 风格冲突 | Step 4 视觉异常 | 改用 ToggleButton + 手动切换，或加 TabStrip |
| `expandCommandTemplate` 与现有 `LaunchArgumentBuilder` API 不对齐 | Step 5 编译失败 | grep `commandTemplates` 用法，找现有展开函数复用 |
| AddEntry 路径不是单一函数（可能拆在多处） | Step 5 找不到复用点 | grep `_entries.Add` / `AddEntry` 所有调用点，找最贴近的入口 |
| 现有 `RequestDiscoverAgentsAsync` 返回类型与新模式冲突 | Step 5 类型不匹配 | 拆成两个 factory：`RequestDiscoverAgentsPathScanAsync` + `RequestDiscoverAgentsDirectoryPickAsync`，从 MainWindow 注入两个按钮 |

## 完成检查清单

- [ ] Step 1-7 全部绿
- [ ] 新增 12 条单元测试全过
- [ ] 572+ → 584+ 测试数（假设 frecency 子任务先落地）
- [ ] GUI smoke 在 mode 切换 + 创建 entry 后不崩溃
- [ ] commit message 风格：`feat(discovery): add directory scan wizard mode (child 5)`

## 跟进检查（实施后）

- [ ] 更新父任务进度 `[4/5 done]` → `[5/5 done]`
- [ ] 父任务收尾 commit（含跨 child 集成测试 + 父 prd 收敛）
- [ ] journal 记录 session
