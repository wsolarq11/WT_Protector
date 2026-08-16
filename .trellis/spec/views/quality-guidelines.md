# Quality Guidelines — Views Layer

> Code standards, forbidden patterns, and review checklist for `WTLauncher.App/Views/`.

---

## View Code-Behind Boundaries

Code-behind is allowed to handle:
- **Dialog injection** (wiring `Func<>` delegates in constructor)
- **Keyboard event routing** (`OnWindowKeyDown`, Ctrl+F, Escape)
- **Drag-drop event wiring** (DragOver, Drop handlers)
- **Window lifecycle** (SizeChanged, Closing, Opened)
- **Context menu opening** (right-click → `ContextMenu.Open()`)

Code-behind must NOT:
- **Make business decisions** (e.g., "should this entry show confirmation?")
- **Directly mutate ViewModel collections** (no `viewModel.Entries.Add()`)
- **Call persistence methods** (no `LauncherStore.Save()`)
- **Contain conditional logic about data validity**

---

## XAML Standards

### Resource References

```xml
<!-- GOOD: StaticResource from design tokens -->
<Border Background="{StaticResource PrimaryBrush}"
        CornerRadius="{StaticResource CardBorderRadius}" />

<!-- BAD: Hardcoded value -->
<Border Background="#2563EB" CornerRadius="13" />
```

`WTLauncher.App` production `.cs` and `.axaml` files must contain no `#RRGGBB` or `#AARRGGBB` literals. Reuse an existing token first; add a Core token only for a distinct, stable semantic role. Framework-only values such as an empty shadow are created once in `App.InjectDesignTokens()`:

```csharp
Resources["NoBoxShadow"] = BoxShadows.Parse("0 0 0 0 Transparent");
```

```xml
<Setter Property="BoxShadow" Value="{StaticResource NoBoxShadow}"/>
```

The root `scripts/verify.ts` gate reports `app-raw-hex-zero` and rejects a temporary raw-hex fixture, so an allowlist must not be introduced to bypass the ownership rule.

### Class-based Visual States

```xml
<!-- GOOD: Classes driven by ViewModel bindings -->
<Border Classes.drag-insert="{Binding IsDragInsertBefore}" />
<Border Classes.compact="{Binding IsCompactLayout}" />
```

**Rule:** Visual states use CSS-like classes, not inline `Setter` blocks. Class names are defined in `MainWindowEntryClasses` or similar static containers.

### Three View Modes

Cards, Details, Names — only ONE is visible at a time:

```csharp
// MainWindowEntriesRootSelection.Select()
Control Select(bool isDetailedList, bool isNamesView, Control details, Control names, Control cards)
```

**Source:** `MainWindow.axaml.cs` — `MainWindowEntriesRootSelection`

### Main-window action hierarchy

Keep high-frequency actions directly visible and place maintenance actions in labeled overflow menus:

- Direct: launch selected, add entry, discover, preset apply/preview/launch.
- Overflow: select/clear/refresh, command/address management, preset save/update/delete.

An overflow trigger is a standard text `Button` with a `ContextMenu`, an automation name, and one shared view-only click handler. Menu items bind the existing ViewModel commands; do not create proxy commands or duplicate business logic in code-behind.

```csharp
if (sender is Control { ContextMenu: { } menu } control)
{
    menu.Open(control);
    e.Handled = true;
}
```

---

## Performance Rules

### In-App Reorder: Use Pointer Capture, Not OLE DragDrop

OLE `DragDrop.DoDragDropAsync` starts a nested Win32 message loop that prioritises
input messages over render callbacks, causing drag stutter regardless of how
lightweight the visual tree is. For in-app card/item reorder:

```csharp
// ❌ OLE drag-drop — nested message loop kills render scheduling
await DragDrop.DoDragDropAsync(e, data, DragDropEffects.Move);

// ✅ Pointer capture — fires on Avalonia's normal render loop
pointer.Capture(this);
// PointerMoved → update ghost + insert line
// PointerReleased / PointerCaptureLost → commit
```

**Source:** `MainWindow.Drag.cs` P6 — full pointer-capture architecture with
`ForceCleanup()` pattern for guaranteed pointer release on any exit path.

### Drag Hot Path: Zero Layout, Zero Visual-Tree Walk

Every element moved during drag must use `RenderTransform`, never
`Canvas.SetLeft/SetTop` or property bindings that trigger `InvalidateMeasure`:

```csharp
// ❌ Triggers InvalidateArrange on parent → layout cascade
Canvas.SetLeft(ghost, x);

// ✅ Purely visual — compositor-only operation
ghost.RenderTransform = new TranslateTransform(x, y);
```

```xml
<!-- ❌ AffectsMeasure → InvalidateMeasure propagates to panel -->
<Border IsVisible="{Binding IsDragInsertBefore}" />

<!-- ✅ Render property — zero layout impact -->
<Border Opacity="{Binding IsDragInsertOpacityBefore}" />
```

### Performance Diagnosis: Eliminate Before Optimising

When a component is slow, **first hide it entirely** (`IsVisible=false`) and
measure the performance delta. If hiding 450+ visual elements makes zero
difference, the bottleneck is not in rendering — look at message loops, I/O,
or GC. Do not optimise code you haven't proven is the bottleneck.

**Source:** `pitfalls.md` §2.6 — full methodology with session evidence.

### Card Layout Calculation

Card sizing is cheap integer math in Core, not measure/arrange passes:

```csharp
// LauncherEntryLayoutCalculator computes rows/columns in O(n)
// Sizes are delegated to CardLayoutProvider — single source of truth,
// no per-VM mutable state, no memoization needed (calculation is trivially fast).
```

**Key invariants:**
- `CardWidth`/`CardHeight`/`NameTileWidth`/`NameTileHeight` are **read-only properties** on `LauncherEntryViewModel` that delegate to `CardLayoutProvider`
- `CardLayoutProvider` is owned by `LayoutManager` and exposed via `IEntryCardHost.CardLayout`
- `CardLayoutProvider.Recalculate(availableSize, count)` is the only mutation path — called from `ApplyFilter()` and `UpdateCardsAvailableSize()`
- Never store ephemeral View-derived dimensions as `[ObservableProperty]` with defaults on ViewModels — this creates staleness risk (see `LauncherEntryLayoutPresentation.cs` removal in git history)

---

## Testing Views

View-logic tests live in `WTLauncher.Tests/` and test **extracted pure functions**, not Avalonia controls:

| Test file | What it tests |
|-----------|--------------|
| `LauncherDragGhostLayoutCalculatorTests.cs` | Ghost element layout math |
| `MainWindowDragInsertDecisionTests.cs` | Drop target resolution + insert position logic |
| `MainWindowEntryCardResolverTests.cs` | Visual tree card detection |
| `MainWindowEntryCardContextMenuTests.cs` | XAML class/name contracts |
| `MainWindowEntryRowDoubleTapTests.cs` | Double-tap launch decision |
| `MainWindowViewModelTests.ModesTests.cs` | View mode (cards/details/names) switching contracts |

**Pattern:** Extract the decision logic into a static method, test that. The XAML binding/visual behavior is verified by the `verify.ps1` GUI smoke step.

---

---

## View State Preservation

When switching between Cards/Details/Names, Avalonia rebuilds the visual tree. Scroll position perishes unless explicitly preserved.

### Pattern: ViewModel-backed scroll state (F13b, 2026-06-23)

```csharp
// ViewModel owns scroll state (survives template switch)
private readonly Dictionary<LauncherEntryViewMode, double> _scrollOffsets = new();
public void SaveScrollOffset(LauncherEntryViewMode mode, double offset)
    => _scrollOffsets[mode] = offset;
public double GetScrollOffset(LauncherEntryViewMode mode)
    => _scrollOffsets.TryGetValue(mode, out var v) ? v : 0;
```

**Contract**: Scroll state in ViewModel. Save on mode change, restore on layout updated. Never cache scroll offset in View code-behind fields — they become garbage on template switch.

## Preflight Failure Confirmation (E8, 2026-06-23)

```csharp
// OLD (removed): skip confirmation when FailedCount > 0
// if (preflight.Report.FailedCount == 0 && !await Confirm...)

// NEW: always confirm when preflight has failures
if (!RequireConfirmationForLaunches && preflight.Report.FailedCount == 0)
    return true;  // skip only when clean preflight + toggle off
```

`StartBatch` no longer unconditionally skips valid items when preflight partially fails. Users decide via the confirmation dialog.

## Code Review Checklist

- [ ] No business logic in code-behind.
- [ ] No hardcoded colors/sizes in XAML — all from `{StaticResource}`.
- [ ] New dialog follows the `typed param → ShowDialog<T>` pattern.
- [ ] New drag visual uses `drag-target` class, not duplicated tokens.
- [ ] DragOver handler does not trigger full list refresh.
- [ ] Keyboard alternatives exist for drag-only operations.
- [ ] Dialog layout reuses `LauncherDialogButtons`/`LauncherDialogText` where applicable.
- [ ] **XAML TimeSpan**: All `Duration`/`MinimumPopulateDelay` values use explicit `h:m:s.ms` format, never bare integers. `"150"` = 150 days in `TimeSpan.Parse`, not 150ms. Use `"0:0:0.150"`.
- [ ] **NaN/Infinity guard**: All `MeasureOverride`/`ArrangeOverride` must guard `availableSize.Width`/`.Height` with `double.IsFinite` and `double.IsNaN` before using in calculations or passing to child `.Measure()`.
- [ ] **Dialog default button**: Destructive confirmation dialogs (delete) must make Cancel the `isDefault: true` button. Only safe/affirmative dialogs (launch, save) may make the primary action the default.
- [ ] **Exception filter unwrap**: Any `while` loop that unwraps exception wrappers (TypeInitializationException, AggregateException) must have a max-iteration guard (e.g., `for (var i = 0; i < 16 && ...)` ) to prevent infinite loops from null-InnerException cycles.
