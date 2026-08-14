# Research: String Comparison

- **Query**: Check all .cs files for correct StringComparison usage
- **Scope**: Internal
- **Date**: 2026-07-26

## Findings

### StringComparison.Ordinal / OrdinalIgnoreCase Usage

Most string comparison operations throughout the codebase correctly use `StringComparison.Ordinal` or `StringComparison.OrdinalIgnoreCase`.

**Correct usages (sampled):**
- `string.Equals(left, right, StringComparison.Ordinal)` (EntryId.cs:84)
- `string.Equals(e.Command, key, StringComparison.OrdinalIgnoreCase)` (CommandUsageAnalyzer.cs:68)
- `string.Equals(p.Name, normalizedName, StringComparison.OrdinalIgnoreCase)` (LauncherPresetWorkflow.cs:48)
- `.Contains(t, StringComparison.OrdinalIgnoreCase)` (EntryProjection.cs:44-46)
- `StringComparer.OrdinalIgnoreCase` on HashSets throughout

### V: Direct String Comparison with `==` Operator (Culture-Sensitive)

Two instances in `MainWindow.axaml.cs` use the `==` operator for string comparison against string literals without explicit `StringComparison`:

**V: MainWindow.axaml.cs line 105**
```csharp
if (w.WindowState == "Maximized")
    WindowState = WindowState.Maximized;
```
Per CA1307, `string.operator==(string, string)` uses `StringComparison.Ordinal` since .NET 5+, so this is actually Ordinal by default. However, not specifying `StringComparison.Ordinal` explicitly makes it harder to audit for culture-sensitive patterns.

**V: MainWindow.axaml.cs line 131**
```csharp
if (mode == "acrylic")
```
Same concern — while `==` is Ordinal in .NET 5+, the lack of explicit `StringComparison.Ordinal` creates inconsistency with the rest of the codebase.

### StringComparison.InvariantCulture

A search for `StringComparison.InvariantCulture` across the entire src/ and tests/ directories returned **no matches**. Good — invariant culture comparisons are avoided.

### All .Contains() Calls on Collections

String `.Contains()` calls on collections (`HashSet<string>`) correctly rely on the set's comparer (e.g., `new HashSet<string>(StringComparer.OrdinalIgnoreCase)`). Key examples:
- `_existingPresetNames.Contains(name)` — HashSet with `OrdinalIgnoreCase` (PresetDetailsWindow.axaml.cs:54)
- `EntryId.WindowsReservedNames.Contains(normalizedId)` — HashSet with `OrdinalIgnoreCase` (LauncherEntryDrafts.cs:99, EntryId.cs:68)
- `knownSet.Contains(id)` — HashSet (LauncherEntryOrder.cs:19)
- `_alreadyAddedExecNames.Contains(discovered.Catalog.ExecName)` — HashSet with `OrdinalIgnoreCase` (DiscoverAgentsViewModel.cs:120)

**No violations found** on collection Contains calls.

### .ToLowerInvariant() / .ToUpperInvariant() Usage

All `ToLowerInvariant()` and `ToUpperInvariant()` calls are intentional and correctly used:
- `ToLowerInvariant()` for normalization (LauncherCardsGroupKeys.cs:18, PresetId.cs:40)
- `ToUpperInvariant()` for brand glyph display (DiscoverAgentsViewModel.cs:219-220, LauncherEntryViewModel.cs:51-52)

These are invariant-culture operations, which are acceptable for display and normalization purposes.

**No violations found.**

## Summary

| Category | Violations |
|---|---|
| StringComparison explicitly specified | 0 violations (consistent) |
| Culture-sensitive `==` on strings | 2 instances (MainWindow.axaml.cs:105, 131) — functionally Ordinal in .NET 5+, but inconsistent style |
| InvariantCulture comparisons | 0 violations |
| Collection .Contains() patterns | 0 violations |
| ToLower/ToUpper culture | 0 violations |
