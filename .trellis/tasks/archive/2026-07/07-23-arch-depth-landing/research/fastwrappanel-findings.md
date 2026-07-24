# Research: FastWrapPanel Custom Layout Panel Analysis

- **Query**: Ultrareview: FastWrapPanel custom layout panel deep code review
- **Scope**: Internal code analysis
- **Date**: 2026-07-23

## Findings

### Files Analyzed

| File Path | Description |
|---|---|
| `src/avalonia-launcher/src/WTLauncher.App/Views/FastWrapPanel.cs` | Custom `Panel` subclass implementing left-to-right wrap layout |
| `src/avalonia-launcher/tests/WTLauncher.Tests/FastWrapPanelTests.cs` | 6 test methods covering core wrap behavior and edge cases |
| `src/avalonia-launcher/src/WTLauncher.App/Views/LauncherEntriesView.axaml` | XAML using FastWrapPanel in Cards view (line 74) and Names view (line 408) |

### Files Referencing FastWrapPanel

| File | Context |
|---|---|
| `src/avalonia-launcher/src/WTLauncher.App/Views/LauncherEntriesView.axaml:74` | Cards view: `ItemSpacing="{x:Static core:LauncherDesignTokens.SpacingSmall}"` (8px) |
| `src/avalonia-launcher/src/WTLauncher.App/Views/LauncherEntriesView.axaml:408` | Names view: `ItemSpacing="{x:Static core:LauncherDesignTokens.NameTileGap}"` (6px), `LineSpacing="{x:Static core:LauncherDesignTokens.NameTileRowGap}"` (4px) |
| `src/avalonia-launcher/tests/WTLauncher.Tests/FastWrapPanelTests.cs` | 6 test methods |
| `tools/verify.ps1:591` | Whitelisted file (ignored by style gate) |
| `.trellis/spec/views/directory-structure.md:15` | Listed as custom panel for name-wall layout |

### Design Tokens Used as Spacing Values

| Token | Value | Used In |
|---|---|---|
| `LauncherDesignTokens.SpacingSmall` | 8 | Cards view FastWrapPanel |
| `LauncherDesignTokens.NameTileGap` | 6 | Names view FastWrapPanel (ItemSpacing) |
| `LauncherDesignTokens.NameTileRowGap` | 4 | Names view FastWrapPanel (LineSpacing) |

---

## Custom Panel Implementation

### 1. MeasureOverride / ArrangeOverride Correctness (Confidence: 90)

**MeasureOverride** (FastWrapPanel.cs:38-81):
- Children are measured with `child.Measure(new Size(width, measureHeight))` where `width` is the panel's available width (or 900 fallback) and `measureHeight` is either the available height or PositiveInfinity (line 58-59). This constrains child width to the panel bounds, preventing overflow at row start.
- The wrap condition at line 65 (`rowWidth + childWidth > width && rowWidth > 0`) has a `rowWidth > 0` guard that ensures a single child wider than the panel still gets placed on a fresh row rather than being swallowed.
- Row height is tracked as the maximum child height in the row (lines 74, 113).
- After wrapping, the trailing spacing is subtracted from each row's total width for the final panel width calculation (line 68: `rowWidth - spacing`, line 78: `rowWidth > 0 ? rowWidth - spacing : 0`).

**ArrangeOverride** (FastWrapPanel.cs:83-117):
- Uses `child.DesiredSize` from the Measure pass, avoiding re-measurement (the key optimization vs. `WrapPanel`).
- Clamps child width to `containerWidth` via `Math.Min(childW, containerWidth)` (line 101) as defense-in-depth.
- Arrange positions match Measure wrap logic exactly -- both use the same condition (`x + childWidth > containerWidth && x > 0`) and the same x-accumulation pattern.

**Measure / Arrange width divergence** (Minor, Confidence: 90):
- In MeasureOverride, children are measured with `availableSize.Width` (line 59) but the panel's own width can report less than `availableSize.Width` (line 80: `Math.Min(totalWidth, width)`). This means the panel may report a narrower desired size than what children were measured against. In practice, Avalonia passes `finalSize >= DesiredSize` to ArrangeOverride, so containerWidth in ArrangeOverride should be >= the measure width. The `Math.Min` clamp on line 101 would be a no-op in normal operation but serves as defense-in-depth.

### 2. Infinite Constraint Handling (Confidence: 95)

- **Width infinity/NaN**: Falls back to 900px (FastWrapPanel.cs:47). Documented as conservative default for primary monitor width minus window chrome.
- **Height infinity**: Used as-is; children are measured with `PositiveInfinity` height (line 58), allowing them to report their natural height.
- **ArrangeOverride**: Falls back `finalSize.Width` to 900 via `Math.Max(1, ...)` (line 86), preventing zero-width degenerate layout.
- **NegativeInfinity**: Both `double.IsInfinity()` and `double.IsNaN()` return `false` for `NegativeInfinity` (confirmed by test `Measure_HandlesNegativeInfinityWidthWithoutCrash` at FastWrapPanelTests.cs:63). The negative value propagates to children's `Measure(new Size(negative, ...))`. Avalonia's `Layoutable.Measure` does not validate for negative infinity, so this produces garbage sizes rather than a crash. This is a known gap; the test documents it as defense-in-thought rather than a fix.

### 3. Line Wrapping Logic (Confidence: 95)

- Standard left-to-right, top-to-bottom wrap. Each child is placed after the previous one with `ItemSpacing` gap.
- When `rowWidth + childWidth > availableWidth` (and at least one child is already placed in the row), the current child wraps to the next line.
- Row height is determined by the tallest child in each row -- correct for mixed-height content.
- MeasureOverride and ArrangeOverride use identical wrap conditions, ensuring consistency.

### 4. Stretch / Alignment (Confidence: 85)

- Children are arranged at their exact `DesiredSize` (clamped for width). No stretch behavior -- all items are top-left aligned within their row cell.
- This matches `WrapPanel` behavior in Avalonia/WPF, where items are also placed at DesiredSize.
- No `VerticalContentAlignment` or `HorizontalContentAlignment` properties are exposed. If children use vertical `Stretch` alignment, FastWrapPanel's explicit Arrange rect will override it.

### 5. Last Line Alignment Edge Cases (Confidence: 95)

- The last line is placed correctly starting from x=0 (line 107) with normal spacing.
- Trailing spacing on the last line is correctly excluded from the panel's computed width (line 78).
- If the last line has fewer items, they start at x=0 with standard spacing -- no special centering or justification, which is correct for a minimal wrap panel.

### 6. Very Small / Large Available Sizes (Confidence: 90)

- **Very narrow** (smaller than any child): `rowWidth > 0` guard in the wrap condition ensures a child wider than the panel is still placed on a new line (line 65). The child will overflow the panel horizontally. ArrangeOverride clamps child width with `Math.Min(childW, containerWidth)` (line 101), which may clip content but prevents layout crashes.
- **Zero width**: `Math.Max(1, ...)` guard on line 86 prevents zero-width container in ArrangeOverride; no equivalent guard in MeasureOverride (line 47 handles NaN/Infinity but not zero). A zero-value `availableSize.Width` would pass through, making `width = 0` and placing all children at x=0 on the same row.
- **Very large**: Items distribute naturally across available width with standard wrapping. No overflow or performance issues beyond O(n).

### 7. Empty Panel (Confidence: 100)

- MeasureOverride: `foreach` body is skipped, `totalWidth = 0`, `totalHeight = 0`. Returns `Size(0, 0)`.
- ArrangeOverride: `foreach` body is skipped. Returns `finalSize` unchanged.
- No crashes. Confirmed by test `Arrange_DoesNotCrashOnEmptyPanel` (FastWrapPanelTests.cs:72).

### 8. Single Child Edge Cases (Confidence: 95)

- MeasureOverride: `rowWidth = childWidth + spacing`, `rowHeight = childHeight`. Returns `Size(childWidth, childHeight)` -- the trailing spacing is correctly subtracted from the reported width (line 78).
- ArrangeOverride: child is arranged at `(0, 0, childWidth, childHeight)`.
- Behavior is correct for both narrower-than-panel and wider-than-panel children.

---

## Performance (Confidence: 100 overall)

### 1. Layout Pass Complexity (Confidence: 100)

- **O(n)** for both MeasureOverride and ArrangeOverride, where n = number of children.
- Single pass over `Children` with no nested loops, no sorting, no secondary passes.
- Simpler than `WrapPanel` which may re-measure children during Arrange.

### 2. Allocations Per Layout Pass (Confidence: 100)

- No heap allocations per pass. All state is stored in local `double` variables on the stack.
- No LINQ, no closures, no dictionary/list allocations.
- The static constructor lambdas (lines 22-23) are one-time allocations.

### 3. Visibility Changes (Confidence: 85)

- The panel does not explicitly check `child.IsVisible` before measuring.
- In Avalonia 12, `Layoutable.Measure()` on a non-visible child sets `DesiredSize = (0, 0)` internally, so invisible children effectively disappear from layout.
- A child with `IsVisible = true` but actual rendered `IsVisible = false` (via ancestor visibility) is already excluded from the layout pass by Avalonia's own recursion, so FastWrapPanel would never see it.
- No explicit `IsVisible` filtering means that if a child somehow returns a non-zero DesiredSize while being invisible, it would still occupy space. This is consistent with how most Avalonia panels behave and is not a practical concern given the ItemsControl usage pattern.

---

## Avalonia-Specific (Confidence: 90)

### 1. AffectsMeasure / AffectsArrange Usage (Confidence: 80)

- The panel uses `AddClassHandler` in the static constructor (lines 22-23) to call `InvalidateMeasure()` when `ItemSpacingProperty` or `LineSpacingProperty` changes.
- This is functionally equivalent to the idiomatic `AffectsMeasure<FastWrapPanel>(ItemSpacingProperty, LineSpacingProperty)` pattern.
- The manual handler approach is slightly more verbose and non-idiomatic, but produces identical behavior. No functional issue.
- Note: `AffectsArrange` is not used. Since spacing changes affect both measure and arrange, `InvalidateMeasure` is sufficient (measure invalidation cascades to arrange).

### 2. Proper Override Signatures (Confidence: 100)

- `MeasureOverride(Size availableSize) -> Size` -- correct for Avalonia 12.
- `ArrangeOverride(Size finalSize) -> Size` -- correct for Avalonia 12.
- Both are `protected override` in a class deriving from `Panel`.

### 3. Outdated API Usage (Confidence: 100)

- No outdated APIs detected.
- Uses `AvaloniaProperty.Register<FastWrapPanel, double>` (Avalonia 11+ style, correct for 12).
- Uses `AddClassHandler<FastWrapPanel>` (Avalonia 11+ style, correct for 12).
- Uses `Layoutable.DesiredSize` (correct property name, unchanged in 12).
- Uses `InvalidateMeasure()` (correct method, unchanged in 12).

---

## Test Coverage (Confidence: 70)

### Tests Present

| Test | File:Line | What It Covers |
|---|---|---|
| `MeasureArrange_ChildrenWithinDesiredSize_AfterWrap` | FastWrapPanelTests.cs:12 | 10 children, 3 per row wrap, verifies bounds containment and exact dimensions |
| `Measure_ExactFit_DoesNotWrap` | FastWrapPanelTests.cs:37 | Boundary case: item pair exactly fills width (96+8+96=200), must NOT wrap between them |
| `Measure_HandlesPositiveInfinityWidthWithoutCrash` | FastWrapPanelTests.cs:52 | Infinite width fallback to 900px, verifies finite DesiredSize |
| `Measure_HandlesNegativeInfinityWidthWithoutCrash` | FastWrapPanelTests.cs:61 | Negative infinity (bypasses NaN/Infinity guards), no-crash contract |
| `Arrange_DoesNotCrashOnEmptyPanel` | FastWrapPanelTests.cs:72 | Empty children list, both Measure and Arrange |
| `Measure_NaNIsHandledByAvaloniaBaseClass` | FastWrapPanelTests.cs:79 | NaN throws at Avalonia base before panel code runs |

### Testing Gaps

| Gap | Severity | Detail |
|---|---|---|
| Mixed-height children | Medium | No test with children of varying heights in the same row. Row height = max(child heights) is untested. |
| Single child wider than container | Low | No test for the "child wider than panel width" edge case. |
| Arrange consistency vs. Measure | Medium | No test that validates Arrange positions match Measure calculations (e.g., that x,y position equals expected row/column position). |
| Zero/negative spacing values | Low | No test for `ItemSpacing = 0`, negative, or very large values. |
| Collapsed/hidden children | Medium | No test with a child whose `IsVisible = false`. |
| Single child | Low | No explicit test for the single-child edge case. |
| Large-N performance | Low | No test with many children (e.g., 1000) for performance regression detection. |
| Exact fit at end of row | Low | No test where the last item exactly fills remaining space on the row. |
| Very narrow container | Low | No test where container is narrower than every child. |

---

## Related Spec Documents

- `.trellis/spec/views/directory-structure.md:15` -- Lists FastWrapPanel.cs as part of the views directory
- `.trellis/tasks/archive/2026-06/06-25-ultrareview-full-clearance-3/implement.md:94` -- Documents R3.22: 900px fallback was documented as intentional (not a bug)

## Caveats / Not Found

- No spec document specifically covers FastWrapPanel design decisions or invariants.
- The 900px fallback width was previously flagged as a review finding (R3.22) and resolved as "documented as intentional" but the document justifying it lives in an archived task rather than in a spec file.
- No test asserts that `ArrangeOverride` produces positions consistent with `MeasureOverride`'s row calculations.
- The panel only works with uniform-width items in practice, despite the doc comment saying "uniform item sizes" -- children can have different heights but the row height is determined by the tallest, which is correct behavior.
