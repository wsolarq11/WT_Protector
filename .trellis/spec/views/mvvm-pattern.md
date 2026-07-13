# MVVM Pattern — Views Layer

> View ↔ ViewModel contract, dialog injection, and design token flow.

---

## Architecture

```
App.axaml.cs (composition root)
    │
    ├─ InjectDesignTokens() ─── LauncherDesignTokens → XAML Resources
    │
    └─ OnFrameworkInitializationCompleted()
        ├─ LauncherStore.ResolveConfigPath()
        ├─ LauncherStore.Load()
        ├─ new LauncherLaunchWorkflow()
        ├─ new MainWindowViewModel(config, settings, workflow, utcNow)
        └─ new MainWindow(viewModel)
            └─ Injects dialog Func<> delegates
```

**Source:** `src/avalonia-launcher/src/WTLauncher.App/App.axaml.cs`

---

## Dialog Injection Pattern

ViewModels never create Windows directly. The `MainWindow` constructor injects `Func<>` delegates that wrap dialog `ShowDialog<>` calls:

```csharp
// In MainWindow constructor:
viewModel.ConfirmLaunchAsync = confirmation =>
    new ConfirmLaunchWindow(confirmation).ShowDialog<bool>(this);

viewModel.ConfirmBatchLaunchAsync = confirmation =>
    new ConfirmBatchLaunchWindow(confirmation).ShowDialog<bool>(this);

viewModel.RequestEntryDraftAsync = entry =>
    new EntryDetailsWindow(entry, ...).ShowDialog<EntryDraft?>(this);
```

**Why:**
1. ViewModels stay testable — tests inject mock `Func<>` that return canned results.
2. View layer owns the window lifecycle — no `Window` references leak into ViewModels.
3. One composition point — all dialog wiring visible in one place.

**Source:** `MainWindow.axaml.cs` lines 25-30.

---

## Design Token Flow

```
LauncherDesignTokens (Core, const)
    │
    ▼
App.axaml.cs InjectDesignTokens()
    │  SolidColorBrush(Color.Parse(token))
    │  new CornerRadius(token)
    │  new Thickness(token)
    ▼
Application.Resources (XAML runtime dictionary)
    │
    ▼
MainWindow.axaml bindings: {StaticResource PrimaryBrush}
```

**Rule:** Never hardcode a color/size value in XAML. Always reference a resource key defined by `LauncherDesignTokens`.

**Source:** `App.axaml.cs` lines 24-69.

---

## ViewModel ↔ View Binding

### One-way: ViewModel → View

```xml
<TextBlock Text="{Binding StatusText}" />
<Button Command="{Binding LaunchCommand}" />
```

### Two-way: View ↔ ViewModel

```xml
<TextBox Text="{Binding FilterText, Mode=TwoWay}" />
```

### Selection Pattern

`LauncherEntryViewModel.IsSelected` setter calls `_owner.UpdateSelection()`, which immediately persists:

```csharp
public bool IsSelected
{
    get => _isSelected;
    set
    {
        if (SetProperty(ref _isSelected, value))
            _owner.UpdateSelection(this, value);  // → save config
    }
}
```

**Source:** `LauncherEntryViewModel.cs` lines 33-41.

---

## ObservableObject Pattern

All ViewModels extend `ObservableObject` from CommunityToolkit.Mvvm:

```csharp
public sealed partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _statusText = "...";
    // Generates: public string StatusText { get; set; }
    //           + PropertyChanged notification
}
```

Commands use `[RelayCommand]`:

```csharp
[RelayCommand]
private Task LaunchAsync() => _owner.LaunchAsync(this);
// Generates: public IAsyncRelayCommand LaunchCommand { get; }
```

**Rule:** CommunityToolkit.Mvvm is the ONLY third-party dependency allowed in ViewModels.

---

## AutoCompleteBox for Search

**Decision:** 搜索框使用 `AutoCompleteBox` 而非 `TextBox`。AutoCompleteBox 内置前缀匹配过滤、键盘导航（↓↑/Enter/Escape）、下拉建议列表、和 MinimumPopulateDelay 防抖。

**关键属性配置：**

```xml
<AutoCompleteBox x:Name="SearchTextBox"
                 Text="{Binding FilterText, Mode=TwoWay}"
                 ItemsSource="{Binding SearchHistoryItems}"
                 MinimumPrefixLength="1"
                 MinimumPopulateDelay="150"
                 IsTextCompletionEnabled="False"/>
```

**规则：**
1. `ItemsSource` 绑定到 ViewModel 的 `IReadOnlyList<string>` 属性——AutoCompleteBox 内置 `StartsWith`（大小写不敏感）过滤器匹配当前输入。
2. `IsTextCompletionEnabled="False"` ——不禁用内联自动补全，用户输入不被自动替换。
3. `MinimumPopulateDelay="150"` ——150ms 去抖，防止每次按键都刷新下拉。
4. **不在 XAML 中设置 `FilterMode`** ——默认 `StartsWith` 已是大小写不敏感。

**与 TextBox 的差异：**
- AutoCompleteBox **没有 `SelectAll()` 方法**。Ctrl+F 聚焦搜索框时只做 `Focus()`，不选全文本。
- `IsDropDownOpen` 属性可区分"下拉打开（用户浏览建议）"和"下拉关闭（用户输入文本）"。

**Source:** `MainWindow.axaml` lines 329-336, `MainWindowViewModel.Layout.cs` `SearchHistoryItems`.

---

## Commit-on-Enter Pattern

搜索历史写入使用"commit"语义——只在 Enter 键时写入，不在每次按键时写入：

```
Code-behind OnWindowKeyDown:
  Enter + Source is SearchTextBox + !SearchTextBox.IsDropDownOpen
    → viewModel.RecordSearchTerm()
      → 去重（已有词条移到最前）
      → 上限 20 条
      → snapshot/rollback 持久化
```

**为什么不在 `OnFilterTextChanged` 里写搜索历史？** `FilterText` 每次按键都触发——LastFilterText 持久化是有意设计（保存/恢复搜索框文本）。但搜索历史应该是用户"确认"的搜索词（按 Enter 的动作），不是每次按键。

**Source:** `MainWindow.axaml.cs` lines 96-104, `MainWindowViewModel.Layout.cs` `RecordSearchTerm()`.

---

## Scenario: Persisted ViewModel mutation transaction

### 1. Scope / Trigger

Use this contract when a ViewModel mutation changes in-memory state that must be restored if the unified launcher store cannot commit. It applies to settings, entries, presets, and selection; a save-only operation without a real snapshot must not claim rollback semantics.

### 2. Signatures

```csharp
internal static string? LauncherMutationTransaction.Commit<TSnapshot>(
    TSnapshot snapshot,
    Func<string?> persist,
    Action<TSnapshot> restore);
```

### 3. Contracts

- `snapshot` is captured before the mutation and contains every observable state value owned by that mutation family.
- `persist` routes through `MainWindowViewModel.SaveStoreFile(BuildStoreFile())`; `LauncherStore` remains the only disk writer.
- `restore` performs in-memory restoration plus required derived-state refreshes. It must not write to disk.
- The returned `string?` is the original persistence failure: `null` means committed; non-null means restored.

### 4. Validation & Error Matrix

| Condition | Required result |
| --- | --- |
| `persist()` returns `null` | Keep mutated state; never call `restore` |
| `persist()` returns an error | Call `restore(snapshot)` exactly once; return the same error |
| `persist()` throws a recoverable storage exception | `SaveStoreFile` converts it to the existing persistence status before `Commit` sees it |
| `restore` exposes stale derived state | Contract violation; include selection/filter/count refresh in the restore callback |

### 5. Good/Base/Bad Cases

- Good: entry rollback restores entries, settings, presets, selection, filter projection, counts, and layout presentation.
- Base: settings rollback restores the immutable `LauncherSettings` snapshot.
- Bad: wrapping command-management save in `Commit` without first capturing its edited entries/settings would advertise rollback that cannot happen.

### 6. Tests Required

- `LauncherMutationTransactionTests`: success does not restore; failure restores once and returns the same message.
- Every migrated mutation family keeps a save-failure regression asserting its complete observable state equals the pre-mutation snapshot.
- Full repository verification must still prove the single-store invariant, publish, and GUI smoke.

### 7. Wrong vs Correct

```csharp
// Wrong: every caller privately owns the failure branch.
var failure = SaveStoreFile(BuildStoreFile());
if (failure is not null)
    Restore(snapshot);

// Correct: one control-flow owner; domain restoration stays with the caller.
var failure = LauncherMutationTransaction.Commit(
    snapshot,
    () => SaveStoreFile(BuildStoreFile()),
    Restore);
```

Do not replace this narrow helper with a repository, Unit of Work, or event bus: the application commits one local JSON aggregate and has no multi-resource transaction.

---

## Forbidden Patterns

- ❌ **ViewModel creates a `new Window()`** — dialogs always injected via `Func<>`.
- ❌ **XAML contains hardcoded colors/sizes** — use `{StaticResource}` from design tokens.
- ❌ **Code-behind contains business `if/else`** — delegate to ViewModel.
- ❌ **View references Core types as data contracts** — Views only bind to ViewModel properties.
- ❌ **在 SearchTextBox 上调用 `SelectAll()`** — AutoCompleteBox 无此方法。仅用 `Focus()`。
