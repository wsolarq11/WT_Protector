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
- **Call persistence methods** (no `LauncherConfigStore.Save()`)
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

---

## Performance Rules

### DragOver

```csharp
// NEVER do this in DragOver:
viewModel.Refresh();  // O(n) rebuild of entire list

// DO this:
viewModel.UpdateDragCue(hitIndex, direction);  // O(1) property update
```

**Source:** `MainWindow.Drag.cs` — DragOver updates only cue state, never rebuilds.

### Card Layout Calculation

Card sizing is cheap integer math in Core, not measure/arrange passes:

```csharp
// LauncherEntryLayoutCalculator computes rows/columns in O(n)
// Results cached until available size changes
```

---

## Testing Views

View-logic tests live in `WTLauncher.Tests/` and test **extracted pure functions**, not Avalonia controls:

| Test file | What it tests |
|-----------|--------------|
| `MainWindowDragGhostFactoryTests.cs` | Ghost element creation |
| `MainWindowDragDropDecisionTests.cs` | Drop target resolution |
| `MainWindowDragInsertDecisionTests.cs` | Insert position logic |
| `MainWindowEntryCardResolverTests.cs` | Visual tree card detection |
| `MainWindowEntryCardBoundsCaptureTests.cs` | Bounding box capture |
| `MainWindowXamlContractTests.cs` | XAML class/name contracts |
| `MainWindowEntriesRootSelectionTests.cs` | View mode root selection |
| `MainWindowReorderFlipAnimationTests.cs` | FLIP animation logic |
| `MainWindowReorderAnimationSchedulerTests.cs` | Animation scheduling |
| `MainWindowEntryRowDoubleTapTests.cs` | Double-tap launch decision |

**Pattern:** Extract the decision logic into a static method, test that. The XAML binding/visual behavior is verified by the `verify.ps1` GUI smoke step.

---

## Code Review Checklist

- [ ] No business logic in code-behind.
- [ ] No hardcoded colors/sizes in XAML — all from `{StaticResource}`.
- [ ] New dialog follows the `typed param → ShowDialog<T>` pattern.
- [ ] New drag visual uses `Border.drag-insert` class, not duplicated tokens.
- [ ] DragOver handler does not trigger full list refresh.
- [ ] Keyboard alternatives exist for drag-only operations.
- [ ] Dialog layout reuses `LauncherDialogButtons`/`LauncherDialogText` where applicable.
