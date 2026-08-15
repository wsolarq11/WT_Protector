# Dialogs — Views Layer

> Confirmation windows, entry editor, preset editor — the dialog contract.

---

## Wiring Contract Update (2026-08-15)

- `IDialogService` must not expose `Func<...>` or `Action<...>` bridge parameters.
- Command/directory management dialogs receive `ICommandEditApplier` / `IDirectoryEditApplier`; the dialog ViewModel applies edits through the port and never persists itself.
- `EntryDetailsWindow` and `PresetDetailsWindow` receive `IEntryDraftValidator`; code-behind must not call Core normalization directly.
- Views construct windows only through `DialogService`; business validation and file IO stay behind feature ports.

## Dialog Inventory

| Dialog | File | Return Type | Purpose |
|--------|------|-------------|---------|
| Confirm Launch | `ConfirmLaunchWindow.cs` | `bool` | Single launch "are you sure?" |
| Confirm Batch Launch | `ConfirmBatchLaunchWindow.cs` | `bool` | Batch launch confirmation |
| Confirm Entry Delete | `ConfirmEntryDeleteWindow.cs` | `bool` | Delete entry confirmation |
| Confirm Preset Delete | `ConfirmPresetDeleteWindow.cs` | `bool` | Delete preset confirmation |
| Entry Details | `EntryDetailsWindow.cs` | `EntryDraft?` | Add / edit entry |
| Preset Details | `PresetDetailsWindow.cs` | `string?` | Save / update preset |
| Directory Management | `DirectoryManagementWindow.cs` | `null` | Browse/merge directories |
| Directory Edit | `DirectoryEditDialog.cs` | `string?` | Edit directory path |
| Command Edit | `CommandEditDialog.cs` | `string?` or `(string, string)` | Edit command or template |
| Discover Agents | `DiscoverAgentsWindow.cs` | `DiscoveredAgent[]?` | Auto-discover CLI agents |
| Command Management | `CommandManagementWindow.axaml` | `null` | Manage commands/templates |

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

---

## Dialog Consistency Tokens (V03)

Three tokens unify title font size, outer padding, and footer spacing across all programmatic dialogs:

| Token | Value | Consumed By |
|-------|-------|-------------|
| `DialogTitleFontSize` | 20 | `LauncherDialogText.Title()`, `CommandManagementWindow.axaml` via `{StaticResource DialogTitleFontSize}` |
| `DialogOuterPadding` | 24 | All programmatic dialogs (Border.Padding or Grid.Margin), `CommandManagementWindow.axaml` via `{StaticResource DialogOuterPadding}` |
| `DialogFooterTopMargin` | 14 | `LauncherDialogButtons.Footer()`, `CommandManagementWindow.axaml` via `{StaticResource DialogFooterMargin}` (Margin: `0,14,0,0`) |
| `DialogFooterSpacing` | 10 | `LauncherDialogButtons.Footer()` horizontal button spacing |

**Rules:**
- `LauncherDialogText.Title()` must use `LauncherDesignTokens.DialogTitleFontSize` — no per-dialog FontSize override.
- All dialog outer containers must use `LauncherDesignTokens.DialogOuterPadding` — no per-dialog literal (20/22/24).
- `LauncherDialogButtons.Footer()` must receive `LauncherDesignTokens.DialogFooterTopMargin` unless the parent Grid's `RowSpacing=14` already provides spacing (then use `0` to avoid double spacing).
- `CommandManagementWindow.axaml` must consume `{StaticResource DialogOuterPadding}`, `{StaticResource DialogTitleFontSize}`, and `{StaticResource DialogFooterMargin}` — no XAML literals.

**Source:** `LauncherDesignTokens.cs`

---

## Destructive Dialog Semantics (V03)

Dangerous delete/remove dialogs must bias user input toward safety:

- Cancel button: `IsDefault=true`, `IsCancel=true`, receives initial focus in `Opened`.
- Delete/destructive button: `IsDefault=false`, styled with `LauncherDialogButtons.Destructive()`.

```csharp
var cancel = LauncherDialogButtons.Cancel(isDefault: true);
cancel.Click += (_, _) => Close(false);
var delete = LauncherDialogButtons.Destructive("删除启动按钮", isDefault: false);
delete.Click += (_, _) => Close(true);
Opened += (_, _) => cancel.Focus();
```

**Affected dialogs:** `ConfirmEntryDeleteWindow`, `ConfirmPresetDeleteWindow`.

Safe/affirmative dialogs (launch, save, confirm) continue using Primary as default with initial focus on the primary action.

---

## Design Token Usage in Dialogs

```csharp
public const double DialogMinWidth = 420;
public const double DialogFooterSpacing = 10;
public const double DialogTitleFontSize = 20;
public const double DialogOuterPadding = 24;
public const double DialogFooterTopMargin = 14;
```

**Source:** `LauncherDesignTokens.cs`

---

## Forbidden Patterns

- ❌ **Dialog calls `LauncherStore.Save()` directly** — return data, let ViewModel persist.
- ❌ **Dialog modifies `MainWindowViewModel` state** — return result, let ViewModel handle.
- ❌ **`ShowDialog<object>` with casting** — always use the correct typed return value.
- ❌ **Dialog with no null-return for cancel** — canceled editing must return `null`, not a default object.
