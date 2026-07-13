# Design — Discovery 扫描向导扩展

## 架构与边界

| 层 | 改动 | 不变量保持 |
|----|------|------------|
| 0 — Core | 复用现有 `DirectoryRow` 与 `DirectoryUsageAnalyzer` | Core 无新增类型、无 UI 依赖 |
| 1 — Mutation | 复用 `AddEntry` 路径（无新 mutation 代码） | snapshot/save/rollback 不变 |
| 2 — 持久化 | 无新逻辑 | 原子写路径不变 |
| 3 — 编排 | `MainWindowViewModel` 注入两个 dialog factory（已有 PATH discovery + 新增 directory wizard），通过 callback 拿结果 | 单源真不变 |
| 4 — 视图 | `DiscoverAgentsWindow` 新增 mode 切换 + 第二 panel | 纯 code-behind，无新 XAML 文件 |

## 实施收敛（2026-07-13）

- 不新增 `ExistingDirectoryRow` / `DirectoryWizardResult` Core records；直接复用现有 `DirectoryRow` 和 `EntryDraft`。
- 对话框返回 App 层类型化 union：`DiscoveredAgentsSelection` 或 `ExistingDirectorySelection`，取消为 `null`。
- `IDialogService` 接收 owner 已分析好的 directories 与 command templates；View 不接触持久化。
- MainWindow owner 使用 `EntryService.CreateEntryFromDraft` 验证，再复用 `SaveEntryCollectionChanges` 的 snapshot/save/rollback 边界。

## 原始设计记录

### `ExistingDirectoryRow`（新 record，Core 层）

```csharp
namespace WTLauncher.Core;

/// <summary>
/// Discovery 目录模式的一行 — 已有 entry 中去重后的 directory + 引用计数。
/// ponytail: 不可变 record, Core 层无 UI 依赖, 与 DiscoveredAgent 同模式。
/// </summary>
public sealed record ExistingDirectoryRow(
    string Directory,
    int ReferenceCount);
```

### `DirectoryWizardResult`（新 record，Core 层）

dialog 返回给 owner 的结果：

```csharp
/// <summary>
/// 目录扫描向导用户选择的结果。null 表示取消。
/// ponytail: record, owner 拿到后构造 LaunchEntry 走 AddEntry 路径。
/// </summary>
public sealed record DirectoryWizardResult(
    string Directory,
    string CommandTemplateKey,
    string EntryName);
```

## ViewModel 改动

### `DiscoverAgentsViewModel` 扩展

新增 mode 状态 + 目录模式数据：

```csharp
public enum DiscoveryMode { PathScan, DirectoryPick }

[ObservableProperty]
private DiscoveryMode _mode = DiscoveryMode.PathScan;

// 目录模式数据
public ObservableCollection<ExistingDirectoryRow> ExistingDirectories { get; } = new();

[ObservableProperty]
private ExistingDirectoryRow? _selectedDirectory;

// 可选 command 模板 keys（来自 settings.commandTemplates）
public IReadOnlyList<string> CommandTemplateKeys { get; }

[ObservableProperty]
private string? _selectedCommandTemplateKey;

[ObservableProperty]
private string _entryName = string.Empty;  // 默认填文件夹名

[ObservableProperty]
private string _commandPreview = string.Empty;  // 展开后的命令预览
```

切换 mode 时不重新构造 VM，只切 `IsVisible` 绑定。

构造函数新增参数：

```csharp
public DiscoverAgentsViewModel(
    Func<AgentDiscoveryResult> scanFunc,
    IEnumerable<string> alreadyAddedExecNames,
    IReadOnlyList<ExistingDirectoryRow> existingDirectories,  // 新增
    IReadOnlyList<string> commandTemplateKeys,                // 新增
    Func<string, string, string> expandCommandTemplate)      // 新增: (key, directory) → 展开命令
```

### 新增 commands

```csharp
[RelayCommand]
private void SelectDirectory(ExistingDirectoryRow? row)
{
    if (row is null) return;
    SelectedDirectory = row;
    EntryName = DefaultNameFromDirectory(row.Directory);  // 自动填名
    RefreshCommandPreview();
}

[RelayCommand]
private void SelectCommandTemplate(string? key)
{
    SelectedCommandTemplateKey = key;
    RefreshCommandPreview();
}

private void RefreshCommandPreview()
{
    if (SelectedDirectory is null || SelectedCommandTemplateKey is null)
    {
        CommandPreview = string.Empty;
        return;
    }
    CommandPreview = _expandCommandTemplate(SelectedCommandTemplateKey, SelectedDirectory.Directory);
}

public bool CanCreateFromDirectory => SelectedDirectory is not null && SelectedCommandTemplateKey is not null;

public DirectoryWizardResult? GetDirectoryWizardResult()
{
    if (!CanCreateFromDirectory) return null;
    return new DirectoryWizardResult(
        SelectedDirectory!.Directory,
        SelectedCommandTemplateKey!,
        string.IsNullOrWhiteSpace(EntryName) ? DefaultNameFromDirectory(SelectedDirectory.Directory) : EntryName.Trim());
}

private static string DefaultNameFromDirectory(string directory)
{
    var trimmed = directory.TrimEnd('\\', '/');
    var idx = Math.Max(trimmed.LastIndexOf('\\'), trimmed.LastIndexOf('/'));
    return idx < 0 ? trimmed : trimmed.Substring(idx + 1);
}
```

partial OnXxxChanged hooks 触发 `OnPropertyChanged(nameof(CanCreateFromDirectory))`。

### 现有 PATH 模式不变

`ScanAsync` / `ToggleSelect` / `GetSelected` 完全保留，PATH 模式 panel 仍用 Rows。

## View 改动

### `DiscoverAgentsWindow` 顶部 mode 切换

```csharp
var pathRadioButton = new RadioButton { Content = "发现 AI Agent", IsChecked = true };
var directoryRadioButton = new RadioButton { Content = "从已有目录创建" };
pathRadioButton.GroupName = "discoveryMode";
directoryRadioButton.GroupName = "discoveryMode";

// 绑定到 _viewModel.Mode（双向）
pathRadioButton.IsCheckedChanged += (_, _) => { if (pathRadioButton.IsChecked == true) _viewModel.Mode = DiscoveryMode.PathScan; };
directoryRadioButton.IsCheckedChanged += (_, _) => { if (directoryRadioButton.IsChecked == true) _viewModel.Mode = DiscoveryMode.DirectoryPick; };

_viewModel.PropertyChanged += (_, e) =>
{
    if (e.PropertyName == nameof(DiscoverAgentsViewModel.Mode))
    {
        pathPanel.IsVisible = _viewModel.Mode == DiscoveryMode.PathScan;
        directoryPanel.IsVisible = _viewModel.Mode == DiscoveryMode.DirectoryPick;
        // 确认按钮文字也切
        confirmButton.Content = _viewModel.Mode == DiscoveryMode.PathScan ? "添加选中" : "创建启动项";
    }
};
```

### `directoryPanel`（新 StackPanel）

布局：
```
[已有目录列表 ScrollViewer (RadioButton 列表)]
[命令 ComboBox (ItemsSource=CommandTemplateKeys)]
[Name TextBox (默认填文件夹名)]
[命令预览 TextBlock (灰底只读)]
```

### 确认按钮逻辑

```csharp
confirmButton.Click += (_, _) =>
{
    if (_viewModel.Mode == DiscoveryMode.PathScan)
        Close(_viewModel.GetSelected());  // 现有路径
    else
        Close(_viewModel.GetDirectoryWizardResult());  // 新路径
};
```

返回类型从 `IReadOnlyList<DiscoveredAgent>` 改为 `object?`（union），调用方按 mode 分发。或者更干净：dialog 关闭后由 owner 检查 `_viewModel.Mode` 决定走哪个路径。

### ponytail: 不引入 union 返回类型，直接两个 Close 路径

实际上 dialog 关闭时 owner 不知道是哪种结果。两个选项：
1. **Option A**：dialog 持有 `_viewModel.Mode`，关闭前 owner 已经知道（因为 owner 注入了 VM）。
2. **Option B**：返回 `object?`，owner is-match 判断。

选 A 更类型安全。Owner 持有 VM 引用，关闭后调 `_viewModel.Mode` 决定调 `GetSelected()` 还是 `GetDirectoryWizardResult()`。

## Owner 改动（`MainWindowViewModel`）

### 注入 dialog factory

现有 `RequestDiscoverAgentsAsync`（在 MainWindowViewModel.cs 中）扩展：

```csharp
// 已有
private async Task<IReadOnlyList<DiscoveredAgent>> RequestDiscoverAgentsAsync()
{
    var existingDirs = _entries
        .GroupBy(e => e.Directory, StringComparer.OrdinalIgnoreCase)
        .Select(g => new ExistingDirectoryRow(g.Key, g.Count()))
        .OrderByDescending(r => r.ReferenceCount)
        .ThenBy(r => r.Directory, StringComparer.OrdinalIgnoreCase)
        .ToList();

    var templateKeys = _settings.CommandTemplates.Keys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase).ToList();

    var vm = new DiscoverAgentsViewModel(
        scanFunc: AgentDiscoveryScanner.Scan.bind(catalog),  // 已有
        alreadyAddedExecNames: ...,                          // 已有
        existingDirectories: existingDirs,                   // 新
        commandTemplateKeys: templateKeys,                   // 新
        expandCommandTemplate: ExpandCommandTemplate);       // 新

    if (await _showDiscoverAgentsDialog(vm) is not true)
        return Array.Empty<DiscoveredAgent>();

    if (vm.Mode == DiscoveryMode.DirectoryPick)
    {
        var wizardResult = vm.GetDirectoryWizardResult();
        if (wizardResult is not null)
            await AddEntryFromDirectoryWizard(wizardResult);
        return Array.Empty<DiscoveredAgent>();  // 目录模式不返回 agents
    }

    return vm.GetSelected();
}
```

### 新增 `AddEntryFromDirectoryWizard`

```csharp
private async Task AddEntryFromDirectoryWizard(DirectoryWizardResult result)
{
    var snapshot = CaptureEntryMutationSnapshot();
    try
    {
        // 与 AddEntry 路径一致：构造 LaunchEntry → 加到 _entries → save
        var newEntry = new LaunchEntry
        {
            Id = EntryId.Generate(),
            Name = result.EntryName,
            Directory = result.Directory,
            Command = $"${result.CommandTemplateKey}",
            Selected = false,
            UpdatedAtUtc = CurrentTimestampUtc(),
            LaunchedAtUtc = string.Empty,
        };
        // 走现有 AddEntry mutation
        // (具体复用哪个方法待实施时确认；可能是 LauncherEntryMutationWorkflow.Add 之类)
    }
    catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
    {
        ErrorLog.Write(ex, "Discovery.DirectoryWizard");
        RestoreEntryMutationSnapshot(snapshot);
        StatusText = $"创建 entry 失败: {ExceptionMessageFormatter.ForUser(ex)}";
    }
}
```

## 关键 ADR

### ADR-1：用 RadioButton 而非 TabStrip 做 mode 切换

**决策**：两个 RadioButton + GroupName。

**为何**：项目现有 dialog 全是纯 code-behind，无 XAML；TabStrip 模板需要更多 styling 才能与现有 design tokens 一致。RadioButton 内置 styling 已经能用，且语义上 mode 切换就是单选。

**权衡**：未来若加第三个 mode，RadioButton 不如 TabStrip 易扩展。但 ponytail：现在只有 2 个 mode。

### ADR-2：directory 来源是 `_entries`，不扫文件系统

**决策**：只列已有 entry 的 directory，不递归扫磁盘。

**为何**：(1) 父任务 out of scope 明确排除全盘扫描；(2) 已有 entry 的 directory 已被 schema 校验过存在；(3) 用户要"快速创建 entry"，从已用过的 directory 挑比从磁盘扫更精准。

**权衡**：用户想加全新 directory（不在已有列表）时，仍需走主界面 Add Entry → 浏览按钮（子任务 4 已交付）。

### ADR-3：Name 默认填文件夹名，用户可改

**决策**：选中 directory 后自动填 `EntryName = 文件夹名`，TextBox 可编辑。

**为何**：减少输入；文件夹名通常就是项目名。用户偶尔想用更友好的名字（如"主仓库"），可改。

**权衡**：默认值可能不符合用户偏好，但 TextBox 在确认前可改，无 lock-in。

### ADR-4：一次只创建 1 个 entry

**决策**：目录模式单选 directory + 单选 command + 单个 entry。

**为何**：(1) 批量创建增加 UI 复杂度（多选 directory + 多选 command 笛卡尔积）；(2) 用户场景是"我看到这个 directory 想加个新命令（比如已有 codex，想加 claude）"，单次操作；(3) 批量需求出现时再加。

**权衡**：用户想一次加 3 个命令到同一 directory，要点 3 次。但这是边界场景。

### ADR-5：返回值由 owner 查 VM.Mode 决定

**决策**：dialog Close 后，owner 调 `_viewModel.Mode` 判断走哪个分支（不通过 Close 参数传结果类型）。

**为何**：类型安全。Close 返回 `bool?`（true=确认，false/null=取消），具体数据从 VM 取。

**权衡**：owner 必须持有 VM 引用（已经持有，因为注入了），无新耦合。

## 兼容性

### 现有 PATH 模式

完全保留：`ScanAsync` / `GetSelected` / Rows UI 不变。默认 mode 仍是 `PathScan`。

### 现有 dialog 调用方

`MainWindowViewModel.RequestDiscoverAgentsAsync` 调用方式不变（注入的 factory 仍接受 VM）；返回类型不变（`IReadOnlyList<DiscoveredAgent>`）；目录模式的副作用（新 entry）通过 owner 在 VM.Mode 判断后调用 `AddEntryFromDirectoryWizard` 实现。

### Schema

无变更。新 record 仅运行时使用，不进 JSON。

## 回滚

无 schema 变更，无新持久化字段。若实现出错：
- 删 `ExistingDirectoryRow` / `DirectoryWizardResult` records
- 还原 `DiscoverAgentsViewModel` 构造函数签名
- 还原 `DiscoverAgentsWindow` 中 mode 切换 panel
- 还原 `MainWindowViewModel.RequestDiscoverAgentsAsync`

UI 变更全在 code-behind，git revert 即可。

## 操作考量

- 用户在目录模式选了 directory 但没选 command → 确认按钮禁用（CanCreateFromDirectory=false）。
- 用户在目录模式切回 PATH 模式 → 选择状态保留（不重置 SelectedDirectory），但 PATH 模式的确认按钮只看 PATH 数据。
- 关闭 dialog 重开 → 状态全清（重新注入 VM）。
