# Research: Modifier Order

- **Query**: Check all .cs files for correct modifier ordering (per .editorconfig)
- **Scope**: Internal
- **Date**: 2026-07-26

## Findings

### Expected Modifier Order

Per .editorconfig (csharp_preferred_modifier_order):
`public, private, protected, internal, file, static, extern, new, virtual, abstract, sealed, override, readonly, unsafe, required, volatile, async`

### Static Readonly Fields

All static readonly fields in the codebase use the correct order `private static readonly` or `public static readonly`:

**Correct examples (sampled):**
- `private static readonly DiagnosticDescriptor Rule` (DispatchMustBeOnlyWritePathAnalyzer.cs:14)
- `private static readonly string LogDirectory` (ErrorLog.cs:21)
- `private static readonly string[] WindowsExtensions` (AgentDiscoveryScanner.cs:23)
- `public static readonly StringComparer Comparer` (EntryId.cs:19)
- `private static readonly IBrush TitleBrush` (LauncherDialogText.cs:34)
- `public static readonly StyledProperty<double> ItemSpacingProperty` (FastWrapPanel.cs:14)

A grep for `readonly static` across all source files returned **no matches** — every instance uses the correct `static readonly` ordering.

**No violations found.**

### Public/Private/Static/Readonly Sequence

All field and method declarations follow the correct modifier sequence. **No violations found.**

### Async on Methods

The async modifier correctly follows other modifiers:
- `private async void OnRenameCommandClicked` (CommandManagementWindow.axaml.cs:29) — correct: `private` then `async` then `void`
- `internal async Task ScanAsync()` (DiscoverAgentsViewModel.cs:94) — correct: `internal` then `async` then `Task`
- etc.

**No violations found.**

## Summary

| Category | Violations |
|---|---|
| `readonly static` ordering | 0 violations |
| `async` positioning | 0 violations |
| General modifier sequence | 0 violations |
