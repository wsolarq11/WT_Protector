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
        ├─ new DialogService()
        ├─ new EntryService(dialogService)
        ├─ new PresetService(dialogService)
        ├─ new MainWindowViewModel(...interfaces...)
        └─ new MainWindow(viewModel)
            └─ dialogService.Initialize(mainWindow)
```

**Source:** `src/avalonia-launcher/src/WTLauncher.App/App.axaml.cs`

---

## Scenario: Feature-service construction and ViewModel ownership

### 1. Scope / Trigger

Apply this contract whenever a workflow is extracted from `MainWindowViewModel` or a new feature service is added.

### 2. Signatures

```csharp
public MainWindowViewModel(
    string storePath,
    LauncherStoreFile storeFile,
    LauncherLaunchWorkflow launchWorkflow,
    IDialogService dialogService,
    IEntryService entryService,
    IPresetService presetService,
    Func<DateTimeOffset> utcNow);
```

### 3. Contracts

- `App.axaml.cs` is the production composition root and constructs concrete feature services.
- `MainWindowViewModel` receives interfaces and must not `new EntryService` or `new PresetService`.
- Feature services are stateless workflow owners. They do not own the observable aggregate and do not persist directly.
- ViewModel retains bindable state, commands, CQ-02 transaction application, status/receipt mapping and property notification order.
- Reuse an existing Core owner or injected workflow directly; do not add a service that only forwards one method.

### 4. Validation & Error Matrix

| Condition | Required behavior |
| --- | --- |
| Any injected service is null | Constructor throws `ArgumentNullException` |
| Service returns validation/cancel status | ViewModel publishes status without mutating/persisting |
| Service returns updated immutable state | ViewModel applies it through the mutation transaction |
| Store commit fails | ViewModel restores the complete snapshot and preserves the save error |
| Proposed extraction needs the whole ViewModel/callback bag | Keep it as orchestration; the seam is not stable |

### 5. Good/Base/Bad Cases

- Good: `PresetService` owns CRUD, eligibility, reference counting and selection-preview rules.
- Base: `LauncherEntryQuery.FilterAndSort` remains a direct one-line Core call; collection synchronization remains in ViewModel.
- Bad: adding `LayoutService.FilterAndSort(...) => LauncherEntryQuery.FilterAndSort(...)` with no new contract.

### 6. Tests Required

- Direct service tests cover success, cancel/validation and recoverable failure where applicable.
- `ArchitectureRules` R7–R10 prevent Entry/Preset services from referencing Views or `System.IO.File`.
- Existing ViewModel tests prove binding, persistence and rollback behavior remain compatible.

### 7. Wrong vs Correct

```csharp
// Wrong: hidden concrete construction inside the ViewModel.
_presetService = new PresetService(dialogService);

// Correct: composition root constructs; ViewModel validates the interface.
_presetService = presetService ?? throw new ArgumentNullException(nameof(presetService));
```

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

## Scenario: Declarative management window with modal editor routing

### 1. Scope / Trigger

Use this when a management Window has stable tabs, toolbars, list rows and empty states but still needs owner-relative modal editors.

### 2. Signatures

```csharp
public ObservableCollection<CommandRow> CommandRows { get; }
public CommandRow? SelectedCommandRow { get; set; }
public bool HasNoCommandRows => CommandRows.Count == 0;

public CommandManagementWindow(); // Avalonia runtime loader
public CommandManagementWindow(CommandManagementViewModel viewModel); // production owner
```

### 3. Contracts

- Stable layout and row rendering live in compiled XAML using `ItemsSource`, `SelectedItem` and typed `DataTemplate`.
- Collection mutations notify empty-state projections after replacement; code-behind never clears/rebuilds controls.
- Code-behind may open owner-relative modal editors and route returned values into existing ViewModel commands.
- The public parameterless constructor exists only so Avalonia can reach the XAML resource; production composition uses the typed constructor.

### 4. Validation & Error Matrix

| Event | Owner |
| --- | --- |
| tab switch, selection, delete command, visibility | ViewModel binding |
| list row rendering and selected border | XAML/DataTemplate |
| double-tap or toolbar opens modal editor | code-behind |
| validation, mutation, persistence, rollback | ViewModel/Core owner |

### 5. Good/Base/Bad Cases

- Good: `ListBox SelectedItem` binds two-way to the existing selected row and its template owns all row pixels.
- Base: a click handler reads the selected row only to open `ShowDialog<T>(this)`.
- Bad: `RenderRows()` clears a StackPanel and reconstructs every child after each collection change.

### 6. Tests Required

- ViewModel tests cover tab and empty/selection projections.
- XAML structure test proves typed templates, collection bindings, selection bindings and automation status.
- Structure test rejects retired render/build helpers and manual `PropertyChanged` subscriptions.
- Full publish and GUI smoke prove XAML resources and the public loader constructor resolve.

### 7. Wrong vs Correct

```csharp
// Wrong: the View mirrors observable state by rebuilding controls.
rows.CollectionChanged += (_, _) => RenderRows();

// Correct: XAML observes the collection directly.
ItemsSource="{Binding CommandRows}"
SelectedItem="{Binding SelectedCommandRow, Mode=TwoWay}"
```

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

## Scenario: Authoritative status text with derived presentation tone

### 1. Scope / Trigger

Use this when MainWindow needs semantic success/warning/error emphasis without adding toast state or duplicating status content.

### 2. Signatures

```csharp
public string StatusText { get; set; }
public LauncherStatusTone StatusTone => LauncherStatusPresentation.Classify(StatusText);
public string StatusIndicator => LauncherStatusPresentation.IndicatorFor(StatusTone);
```

### 3. Contracts

- `StatusText` remains the only writable authority; tone, indicator and boolean XAML projections are read-only derivatives.
- Classification precedence is error, warning, success, neutral so partial-success text containing “跳过” is not painted as pure success.
- The status panel retains text and adds a glyph; color is never the only cue.
- No timer, queue, history, persistence field or background task is permitted.

### 4. Validation & Error Matrix

| Text class | Tone |
| --- | --- |
| save/load/dialog/launch failure, invalid or missing target | Error |
| canceled, skipped or partial outcome | Warning |
| saved, added, updated, deleted, launched or completed | Success |
| ready, loaded, filtering, selection or in-progress text | Neutral |

### 5. Good/Base/Bad Cases

- Good: `StatusText = "存储保存失败..."` synchronously notifies `StatusTone`, indicator and all boolean projections.
- Base: “已加载 20 个按钮” stays neutral because it is ambient state, not an operation receipt.
- Bad: a separately writable `StatusTone` field that can drift from `StatusText`.

### 6. Tests Required

- Classifier tests cover every tone and precedence overlap.
- ViewModel test asserts dependent `PropertyChanged` notifications.
- MainWindow structural test proves the authoritative text binding, semantic classes and non-color glyph remain present.
- GUI smoke proves compiled XAML resources resolve.

### 7. Wrong vs Correct

```csharp
// Wrong: second writable truth.
StatusText = message;
StatusTone = LauncherStatusTone.Error;

// Correct: presentation is always derived from the authority.
StatusText = message;
public LauncherStatusTone StatusTone => LauncherStatusPresentation.Classify(StatusText);
```

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
      → LauncherSearchHistory.Record()（Normalize + 去重 + 最新置顶）
      → LauncherSearchHistory.MaximumTerms（上限 20 条）
      → snapshot/rollback 持久化
```

**为什么不在 `OnFilterTextChanged` 里写搜索历史？** `FilterText` 每次按键都触发——LastFilterText 持久化是有意设计（保存/恢复搜索框文本）。但搜索历史应该是用户"确认"的搜索词（按 Enter 的动作），不是每次按键。

**Source:** `MainWindow.axaml.cs` `OnWindowKeyDown`, `LauncherSearchHistory.cs`, `MainWindowViewModel.Layout.cs` `RecordSearchTerm()`.

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

## Scenario: Typed feature-service outcomes

### 1. Scope / Trigger

Use this contract when an App feature service operation can succeed, be canceled by the user, or fail recoverably and the ViewModel must branch on that outcome. Raw dialog adapters may retain nullable/bool window semantics; translate them at the feature-service boundary.

### 2. Signatures

```csharp
public abstract record ServiceResult<T>;
public sealed record ServiceSuccess<T> : ServiceResult<T>;
public sealed record ServiceCanceled<T> : ServiceResult<T>;
public sealed record ServiceFailure<T> : ServiceResult<T>;

Task<ServiceResult<LaunchEntry>> IEntryService.AddEntryAsync(...);
Task<ServiceResult<string>> IPresetService.RequestNewPresetAsync(...);
ServiceResult<PresetMutation> IPresetService.AddPreset(...);
```

### 3. Contracts

- `ServiceSuccess<T>.Value` is non-null and carries the complete data required for the caller's success path.
- `ServiceCanceled<T>.Status` and `ServiceFailure<T>.Status` are non-empty existing user-facing status strings.
- Dialog `null`/`false` becomes Canceled; recoverable exceptions and validation rejection become Failure; valid data becomes Success.
- ViewModels match all three branches and use an unreachable default. Persistence, snapshot, mutation, save, and rollback remain outside the service.
- Operation-specific success shapes use immutable sealed payload records instead of anonymous tuples.

### 4. Validation & Error Matrix

| Condition | Required result |
| --- | --- |
| Dialog closes or rejects confirmation | `ServiceCanceled<T>` with the existing cancellation status |
| Dialog throws a recoverable exception | Log it and return `ServiceFailure<T>` with sanitized status |
| Draft/name/domain validation rejects input | `ServiceFailure<T>`; do not mutate caller state |
| Domain workflow returns the expected payload | `ServiceSuccess<T>` with a non-null immutable payload |
| Store save fails after success | ViewModel transaction restores its snapshot; service result is not rewritten |

### 5. Good/Base/Bad Cases

- Good: entry deletion success carries the snapshotted delete target and optional focused entry ID.
- Base: preset name success carries the normalized string; cancel and failure carry only status.
- Bad: `(LaunchEntry? Entry, string? Status)` permits both fields to be null or non-null and forces callers to infer the state.

### 6. Tests Required

- Result constructors reject null success payloads and empty cancel/failure statuses.
- Each service family directly tests success, user cancellation, validation/recoverable failure, and operation-specific payload fields.
- ViewModel regressions continue to assert status text, focus cleanup, preset fallback, persistence failure, and complete rollback.

### 7. Wrong vs Correct

```csharp
// Wrong: four representable states, only two intended.
var (entry, status) = await service.AddEntryAsync(...);
if (entry is not null) { ... }
else if (status is not null) StatusText = status;

// Correct: every outcome is named and handled explicitly.
switch (await service.AddEntryAsync(...))
{
    case ServiceSuccess<LaunchEntry> success: Add(success.Value); break;
    case ServiceCanceled<LaunchEntry> canceled: StatusText = canceled.Status; break;
    case ServiceFailure<LaunchEntry> failure: StatusText = failure.Status; break;
    default: throw new UnreachableException();
}
```

Do not grow this into a global exception taxonomy or replace direct Core contracts that do not require caller branching.

## Scenario: Main-window XAML module with narrow owner contract

### 1. Scope / Trigger

Use this when a stable MainWindow region is large enough to own its XAML but window-level keyboard, scroll, context-menu or drag orchestration must remain in MainWindow.

### 2. Signatures

~~~csharp
public sealed partial class LauncherControlBar : UserControl
{
    public AutoCompleteBox SearchBox { get; }
    public ComboBox DisplayModeBox { get; }
}

public sealed partial class LauncherEntriesView : UserControl
{
    public event EventHandler<RoutedEventArgs>? EntryMoreClicked;
    public event EventHandler<TappedEventArgs>? EntryRowDoubleTapped;
    public event EventHandler<PointerPressedEventArgs>? EntryDragPointerPressed;
}
~~~

### 3. Contracts

- The module uses MainWindowViewModel as its compiled x:DataType and inherits the same DataContext; it never mirrors ViewModel state.
- Public control access is read-only and limited to controls required by proven window behavior.
- Child code-behind forwards original sender/event args only. MainWindow retains launch, context-menu, drag eligibility and scroll decisions.
- Cross-module lookup by string FindControl is forbidden.

### 4. Validation & Error Matrix

| Condition | Required behavior |
| --- | --- |
| Avalonia loads the module | Public parameterless constructor calls InitializeComponent() |
| Window needs focus/selection/scroll control | Use the module's typed read-only accessor |
| XAML raises more/double-tap/pointer input | Forward the original event to the subscribed MainWindow handler |
| Module would need copied state or a callback bag | Keep the orchestration in MainWindow; the proposed seam is too broad |

### 5. Good/Base/Bad Cases

- Good: EntriesView.EntryDragPointerPressed += OnEntryDragPointerPressed.
- Base: the status panel stays in MainWindow because it is small and window-authoritative.
- Bad: MainWindow.FindControl by child name, or a child ViewModel that mirrors selection.

### 6. Tests Required

- Structure test proves module composition, typed DataContext, named-control ownership and event forwarding.
- Structure test rejects migrated templates and named controls remaining in MainWindow.
- Release tests plus publish and GUI smoke prove compiled resources and loader constructors resolve.

### 7. Wrong vs Correct

~~~csharp
// Wrong: fragile visual-tree reach-through.
var cards = this.FindControl<ItemsControl>("CardsEntriesItemsControl");

// Correct: explicit narrow contract.
private ItemsControl CardsEntriesItemsControl => EntriesView.CardsItems;
~~~

---

## Forbidden Patterns

- ❌ **ViewModel creates a `new Window()`** — dialogs always injected via `Func<>`.
- ❌ **XAML contains hardcoded colors/sizes** — use `{StaticResource}` from design tokens.
- ❌ **Code-behind contains business `if/else`** — delegate to ViewModel.
- ❌ **View references Core types as data contracts** — Views only bind to ViewModel properties.
- ❌ **在 SearchTextBox 上调用 `SelectAll()`** — AutoCompleteBox 无此方法。仅用 `Focus()`。
