# Dialogs — Views Layer

> Confirmation windows, entry editor, preset editor — the dialog contract.

---

## Dialog Inventory

| Dialog | File | Return Type | Purpose |
|--------|------|-------------|---------|
| Confirm Launch | `ConfirmLaunchWindow.cs` | `bool` | Single launch "are you sure?" |
| Confirm Batch Launch | `ConfirmBatchLaunchWindow.cs` | `bool` | Batch launch confirmation |
| Confirm Entry Delete | `ConfirmEntryDeleteWindow.cs` | `bool` | Delete entry confirmation |
| Confirm Preset Delete | `ConfirmPresetDeleteWindow.cs` | `bool` | Delete preset confirmation |
| Entry Details | `EntryDetailsWindow.cs` | `EntryDraft?` | Add / edit entry |
| Preset Details | `PresetDetailsWindow.cs` | `string?` | Save / update preset |

---

## Dialog Pattern

Every dialog follows the same pattern:

### 1. Constructor takes a typed parameter object

```csharp
// Confirmation dialogs take a "target" describing what to confirm
new ConfirmEntryDeleteWindow(LauncherEntryDeleteTarget target)

// Editor dialogs take a "draft" for editing (null = create new)
new EntryDetailsWindow(LaunchEntry? entry, int referenceCount, ...)
```

### 2. Returns meaningful result via `ShowDialog<T>`

```csharp
// bool: user clicked confirm / cancel
var accepted = await new ConfirmLaunchWindow(plan).ShowDialog<bool>(owner);

// EntryDraft?: null means canceled
var draft = await new EntryDetailsWindow(entry, ...).ShowDialog<EntryDraft?>(owner);
```

### 3. Never calls business logic directly

Dialogs only **collect user input** and return it. The ViewModel decides what to do with it.

---

## Shared Dialog Helpers

**Source:** `LauncherDialogButtons.cs`, `LauncherDialogText.cs`

Common button layouts and text patterns shared across dialogs:

```csharp
// LauncherDialogButtons.cs
public static class LauncherDialogButtons
{
    // Standard OK/Cancel button row
    // Shared button styling
}

// LauncherDialogText.cs
public static class LauncherDialogText
{
    // Shared label text patterns
}
```

**Rule:** If two dialogs have similar button layouts or text, extract to these helpers. Don't duplicate dialog chrome.

---

## Dialog Injection in MainWindow

**Source:** `MainWindow.axaml.cs` lines 25-30

```csharp
public MainWindow(MainWindowViewModel viewModel) : this()
{
    DataContext = viewModel;
    viewModel.ConfirmLaunchAsync = confirmation =>
        new ConfirmLaunchWindow(confirmation).ShowDialog<bool>(this);
    viewModel.RequestEntryDraftAsync = entry =>
        new EntryDetailsWindow(entry, ...).ShowDialog<EntryDraft?>(this);
    // ... all other dialogs
}
```

**Why here:** The `MainWindow` knows about Avalonia's `Window.ShowDialog<>()`. The ViewModel only knows about `Func<>`. This is the seam.

---

## Design Token Usage in Dialogs

Dialog dimensions use `LauncherDesignTokens`:

```csharp
public const double DialogMinWidth = 420;
public const double DialogFooterSpacing = 10;
```

**Source:** `LauncherDesignTokens.cs`

---

## Forbidden Patterns

- ❌ **Dialog calls `LauncherStore.Save()` directly** — return data, let ViewModel persist.
- ❌ **Dialog modifies `MainWindowViewModel` state** — return result, let ViewModel handle.
- ❌ **`ShowDialog<object>` with casting** — always use the correct typed return value.
- ❌ **Dialog with no null-return for cancel** — canceled editing must return `null`, not a default object.
