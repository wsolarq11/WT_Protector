# Research: Null Checking Patterns

- **Query**: Check all .cs files for consistent null checking patterns
- **Scope**: Internal
- **Date**: 2026-07-26

## Findings

### Pattern 1: ArgumentNullException.ThrowIfNull (Preferred)

The codebase predominantly uses the modern `ArgumentNullException.ThrowIfNull()` pattern. Sampled call sites:
- `AgentDiscoveryScanner.cs:60` — `ArgumentNullException.ThrowIfNull(catalog);`
- `EntryId.cs:104`, `EntryId.cs:123` — `ArgumentNullException.ThrowIfNull(ids);`
- `LaunchArgumentBuilder.cs:47` — `ArgumentNullException.ThrowIfNull(entry);`
- `ErrorLog.cs:45` — `ArgumentNullException.ThrowIfNull(exception);`
- `LauncherEntryViewModel.cs:17-18` — `ArgumentNullException.ThrowIfNull(entry); ArgumentNullException.ThrowIfNull(owner);`
- Many more (100+ call sites)

The codebase also uses `ArgumentException.ThrowIfNullOrWhiteSpace` where appropriate:
- `FileLauncherJsonIO.cs:28` — `ArgumentException.ThrowIfNullOrWhiteSpace(documentName);`
- `LauncherStatusContent.cs:7` — `ArgumentException.ThrowIfNullOrWhiteSpace(text);`
- `LauncherStatusText.cs:304` — `ArgumentException.ThrowIfNullOrWhiteSpace(itemLabel);`

This is the consistent pattern used throughout the project.

**Pattern consistency: HIGH**

### Pattern 2: Manual Null Checks with throw

In `EntryListViewModel.cs:73-76`, the constructor uses a manual pattern:
```csharp
_entryService = entryService ?? throw new ArgumentNullException(nameof(entryService));
```
This is an older pattern but is functionally equivalent to `ArgumentNullException.ThrowIfNull`. It was used in 4 consecutive lines in the constructor. This is inconsistent with the rest of the codebase.

**V: EntryListViewModel.cs lines 73-76**
```csharp
_entryService = entryService ?? throw new ArgumentNullException(nameof(entryService));
_utcNow = utcNow ?? throw new ArgumentNullException(nameof(utcNow));
_storeGrain = storeGrain ?? throw new ArgumentNullException(nameof(storeGrain));
_cardLayout = cardLayout ?? throw new ArgumentNullException(nameof(cardLayout));
```
These 4 lines should use `ArgumentNullException.ThrowIfNull()` for consistency with the rest of the codebase.

### Pattern 3: is null checks

The codebase uses `is null` pattern matching for conditional null checks (not argument validation):
- `if (firstLine is null) return null;` (AgentDiscoveryScanner.cs:218)
- `if (presetName is null) return ServiceCanceled(...)` (PresetService.cs:58)
- `if (configPath is not null)` (LauncherStartupArguments.cs:23)

This is consistent with modern C# patterns and is used correctly. **No violations found.**

### Redundant Null Checks

**No redundant null checks found.** After validated paths (e.g., after `ArgumentNullException.ThrowIfNull`), subsequent code does not re-check for null.

## Summary

| Category | Violations |
|---|---|
| ArgumentNullException.ThrowIfNull pattern | 0 violations (primary pattern) |
| Manual `?? throw` instead of ThrowIfNull | 1 violation site: EntryListViewModel.cs:73-76 (4 lines) |
| `is null` pattern matching | 0 violations |
| Redundant null checks | 0 violations |
