# Research: Naming Conventions

- **Query**: Check all .cs files for naming convention violations
- **Scope**: Internal
- **Date**: 2026-07-26

## Findings

### Public Methods PascalCase

All public methods in src/ follow PascalCase correctly. No violations found.

### Private Fields Should Be _camelCase

Per .editorconfig (dotnet_naming_rule.private_fields_underscore), private fields must use `_camelCase` prefix.

All private fields throughout the codebase correctly use the `_camelCase` pattern:
- `_lock`, `_state`, `_disposables`, `_disposed`, `_hasCached`, `_saveLock`, etc.

**No violations found.**

### Private const Fields Must Be PascalCase

Per .editorconfig (dotnet_naming_rule.const_fields_pascal), `private const` fields must use PascalCase.

All 13 private const fields found use PascalCase correctly:
- `WcaAccentPolicy` (Win32Acrylic.cs:39)
- `MonospaceFontFamily` (DiscoverAgentsWindow.axaml.cs:25)
- `SuppressTransitionsClass` (MainWindow.Drag.cs:23)
- `InsertThresholdDefault` (MainWindow.Drag.cs:375)
- `InsertThresholdHysteresisLow` (MainWindow.Drag.cs:376)
- `InsertThresholdHysteresisHigh` (MainWindow.Drag.cs:377)
- `FallbackAvailableWidth` (LauncherEntryLayoutCalculator.cs:33)
- `FallbackAvailableHeight` (LauncherEntryLayoutCalculator.cs:34)
- `NameTileColumnHysteresisPx` (LauncherEntryLayoutCalculator.cs:37)
- `RepositoryLineEnding` (FileLauncherJsonIO.cs:15)
- `DefaultMaxBackups` (FileLauncherJsonIO.cs:16)
- `MaxLogFileSize` (ErrorLog.cs:17)
- `OldLogSuffix` (ErrorLog.cs:19)

**No violations found.**

### Interfaces Must Start With I

Per .editorconfig (dotnet_naming_rule.interface_should_begin_with_i), all interfaces must have the `I` prefix.

Interfaces found in the codebase:
- `IDialogService` (IDialogService.cs)
- `IEntryService` (IEntryService.cs)
- `IEntryListViewModel` (IEntryListViewModel.cs)
- `IPresetService` (IPresetService.cs)
- `ILauncherJsonIO` (ILauncherJsonIO.cs)
- `IStorePersistence` (IStorePersistence.cs)
- `IEntryCardHost` (IEntryCardHost.cs)

All correctly start with `I`. **No violations found.**

### Parameters Must Be camelCase

Per .editorconfig (dotnet_naming_rule.parameters_camel, severity: suggestion), parameters should be camelCase.

All method parameters throughout the codebase use camelCase correctly. **No violations found.**

### Private Methods PascalCase

C# convention is for private methods to be PascalCase (not `_camelCase` which is for fields). All private methods in the codebase use PascalCase, including event handlers:
- `InjectDesignTokens()` (App.axaml.cs:31)
- `ApplyBackgroundMode(string mode)` (MainWindow.axaml.cs:129)
- `RefreshBrand()` (LauncherEntryViewModel.cs:45)
- etc.

**No violations found.** The PascalCase naming of private methods is correct per C# convention.

## Summary

**No naming convention violations found.** The codebase consistently follows the .editorconfig naming rules.
