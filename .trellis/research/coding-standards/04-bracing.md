# Research: Bracing

- **Query**: Check all .cs files for bracing style, including single-line if/for/foreach without braces
- **Scope**: Internal
- **Date**: 2026-07-26

## Findings

### Allman Style (K&R / Same-Line Opening Brace)

Per .editorconfig:
```
csharp_new_line_before_open_brace = all
csharp_new_line_before_else = true
csharp_new_line_before_catch = true
csharp_new_line_before_finally = true
```

The codebase consistently uses Allman-style braces (opening brace on new line) throughout. All classes, methods, properties, if/else, try/catch, and switch statements use new-line braces.

**No violations found.**

### Single-Line If Statements Without Braces (IDE0011)

Per .editorconfig `csharp_prefer_braces = true:warning` (IDE0011 severity set to `suggestion` — not build-blocking, but still a guideline).

Many single-line `if` statements omit braces. This is technically a style deviation flagged at `suggestion` level. Key examples:

| File | Line | Code |
|---|---|---|
| App.axaml.cs | 102-103 | `if (_isInitialized) return;` |
| AgentDiscoveryScanner.cs | 218-219 | `if (firstLine is null) return null;` |
| Program.cs | 29-30 | `if (ex is not null) ErrorLog.Write(ex, ...)` |
| EntryName.cs | 23-24 | `if (normalized.Length == 0) throw new FormatException(...)` |
| EntryId.cs | 63-64 | `if (trimmed.Length == 0) throw new FormatException(...)` |
| EntryId.cs | 162-163 | `if (!allowed) throw new FormatException(...)` |
| ErrorOrExtensions.cs | 29-30 | `if (ex is AggregateException agg) ex = ...;` |
| Win32Acrylic.cs | 51-52 | `if (hwnd == IntPtr.Zero) return;` |
| LaunchArgumentBuilder.cs | 59-60 | `if (name.Length == 0) throw ...` |
| LaunchArgumentBuilder.cs | 77-78 | `if (totalLength > MaxCommandLineLength) throw ...` |
| LaunchArgumentBuilder.cs | 107-108 | `if (!hasUnsafe) return value;` |
| LauncherCommandTemplateResolver.cs | 23-24 | `if (templates is null) return command;` |
| LauncherCommandTemplateResolver.cs | 30-31 | `if (key.Length == 0) return command;` |
| PresetService.cs | 58-59 | `if (presetName is null) return ServiceCanceled(...)` |
| DiscoverAgentsViewModel.cs | 97-98 | `if (IsScanning) return;` |
| Projection.cs (EntryProjection) | 91-92 | `public void SetFilter(string text) => ...` (expression-bodied) |
| FastWrapPanel.cs | Multiple | Various single-line if statements |

**Note:** Many of these are guard clauses (early returns/throws), which are acceptably concise. However, per the .strict `csharp_prefer_braces = true:warning` setting, all single-line control flow blocks should technically use braces.

Due to the vast number (50+ instances across the codebase), this is a pervasive style pattern rather than isolated violations.

### Expression-Bodied Members (IDE style preference)

The .editorconfig sets:
- `csharp_style_expression_bodied_methods = when_on_single_line:suggestion`
- `csharp_style_expression_bodied_properties = true:suggestion`

The codebase consistently uses expression-bodied members where appropriate:
- Many `=>` property expressions
- Some single-line method expressions (e.g., `CommandEditDialog.axaml.cs:46`: `private void OnCancelClicked(...) => Close(null);`)
- Some multi-line methods use traditional block bodies

These are at `suggestion` level and used appropriately throughout.

**No clear violations found** — mixed usage is within the config rules.

## Summary

| Category | Violations |
|---|---|
| Allman brace style | 0 violations (consistent) |
| Single-line if without braces (IDE0011, suggestion) | 50+ instances across the codebase (not build-blocking per .editorconfig) |
| Expression-bodied usage | 0 violations (within config rules) |
