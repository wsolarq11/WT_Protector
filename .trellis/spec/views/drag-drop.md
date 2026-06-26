# Drag & Drop — Views Layer

> Ghost factory, insert indicators, and drag session management. FLIP animation removed (2026-06-23 ultrareview B1).

---

## Drag Architecture

```
User starts drag on card
    │
    ▼
MainWindow.Drag.cs (event handlers)
    │  DragOver → update insert indicator
    │  Drop → commit reorder
    ▼
MainWindowDragGhostFactory.cs
    │  Creates semi-transparent ghost card overlay
    ▼
MainWindowDragDropDecision.cs
    │  Pure logic: where does the drop go?
```

---

## Ghost Factory

**Source:** `src/avalonia-launcher/src/WTLauncher.App/Views/MainWindowDragGhostFactory.cs`

Creates a visual clone of the dragged card as an overlay with:
- Reduced opacity (60-80%)
- Same dimensions as the source card
- Offset from pointer by a fixed delta
- **All colors from `LauncherDesignTokens`** (not hardcoded brushes)

**Test coverage:** `MainWindowDragGhostFactoryTests.cs`

---

## Insert Indicator

**Source:** `MainWindow.axaml` + `MainWindow.Drag.cs`

The insert indicator is a visual line/border that appears between cards during DragOver. Its state is managed by `DragCueState` on each `LauncherEntryViewModel`.

### Token Ownership (from DESIGN.md)

- Visual token: `drag-target` class — single source for color, radius, opacity, hit-test.
- Three views (Cards/Details/Names) only differ in **direction, size, positioning** — never redefine colors.
- `IsDragInsertBefore` / `IsDragInsertAfter` are **read-only projections** of `DragCueState`.

---

## DragReorderSession

**Source:** `src/avalonia-launcher/src/WTLauncher.App/ViewModels/LauncherDragReorderSession.cs`

```csharp
public sealed class LauncherDragReorderSession
{
    // Single owner of ALL drag state
    // snapshot, source, target, last preview
    // NO loose fields scattered across ViewModel
}
```

**Rule:** All drag state lives in ONE `DragReorderSession?` field. If you find yourself adding a `_dragSource` or `_dragTarget` field to the ViewModel, stop — it already exists in the session.

---

## Keyboard Reorder (Non-Drag Alternative)

**Source:** `MainWindowViewModel.Reorder.cs`

Entries support `MoveUp()` / `MoveDown()` commands with `CanMoveUp()` / `CanMoveDown()` guards. These are the keyboard-accessible alternatives to drag reorder.

---

## Drag Ghost Layout Calculator

**Source:** `src/avalonia-launcher/src/WTLauncher.Core/LauncherDragGhostLayoutCalculator.cs`

Pure Core-layer calculation of ghost dimensions and pointer offset. Lives in Core because it's testable without any UI framework.

**Test coverage:** `LauncherDragGhostLayoutCalculatorTests.cs`

---

## Forbidden Patterns

- ❌ **Drag state scattered across multiple ViewModel fields** — use `DragReorderSession`.
- ❌ **Insert indicator colors duplicated per view mode** — use `drag-target` class.
- ❌ **Ghost opacity/size hardcoded in code-behind** — use `LauncherDragGhostLayoutCalculator`.
- ❌ **Ghost colors hardcoded** — use `LauncherDesignTokens` (fixed 2026-06-23 ultrareview Q7).
- ❌ **Drag as the ONLY way to reorder** — always keep MoveUp/MoveDown as keyboard fallback.
- ❌ **`_dragCardCache` fallback pattern** — removed 2026-06-23 ultrareview B2. Visual hit-test (`GetVisualAt`) is the correct fallback.
