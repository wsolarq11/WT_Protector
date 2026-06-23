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
        ├─ LauncherConfigStore.ResolveConfigPath()
        ├─ LauncherSettingsStore.Load()
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

## Forbidden Patterns

- ❌ **ViewModel creates a `new Window()`** — dialogs always injected via `Func<>`.
- ❌ **XAML contains hardcoded colors/sizes** — use `{StaticResource}` from design tokens.
- ❌ **Code-behind contains business `if/else`** — delegate to ViewModel.
- ❌ **View references Core types as data contracts** — Views only bind to ViewModel properties.
