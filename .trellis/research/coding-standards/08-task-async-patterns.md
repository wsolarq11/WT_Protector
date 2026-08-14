# Research: Task / Async Patterns

- **Query**: Check all .cs files for async/await correctness
- **Scope**: Internal
- **Date**: 2026-07-26

## Findings

### Task.Result / Task.Wait() (Sync-over-Async)

A search for `.Result` across the entire src/ directory returned **no matches**. Zero occurrences of the sync-over-async `.Result` anti-pattern.

A search for `.Wait()` found two matches in `FileLauncherJsonIO.cs`, but these are `SemaphoreSlim.Wait()` calls (not `Task.Wait()`):
- `FileLauncherJsonIO.cs:111` — `_saveLock.Wait();`
- `FileLauncherJsonIO.cs:182` — `_saveLock.Wait();`

`SemaphoreSlim.Wait()` is a legitimate synchronous synchronization primitive, not a `Task` anti-pattern.

In the tests directory, `ErrorLogTests.cs:111, 121` have `ready.Wait()` — these are `ManualResetEvent.Wait()`, also fine.

**No violations found.**

### ConfigureAwait Usage

Per .editorconfig, CA2007 (ConfigureAwait) is explicitly disabled (`dotnet_diagnostic.CA2007.severity = none`) because the project uses Avalonia's UI synchronization context. ConfigureAwait(true) is used intentionally at UI-thread call sites.

**ConfigureAwait(true)** — used consistently on awaits that must return to the UI thread:
- `PresetService.cs:50` — `.ConfigureAwait(true)`
- `EntryListViewModel.cs:127` — `.ConfigureAwait(true)`
- `EntryService.cs:27` — `.ConfigureAwait(true)`
- `LaunchOrchestrator.cs:101` — `.ConfigureAwait(true)`
- `MainWindowViewModel.Presets.cs:55` — `.ConfigureAwait(true)`
- etc.

**No ConfigureAwait(false) in the source code** — this is consistent with the Avalonia UI constraint. Some code paths (e.g., `ErrorOrExtensions.TryAsync`) do not use ConfigureAwait, which is acceptable since they precede UI operations and have no explicit context requirement.

**No violations found** — ConfigureAwait usage is consistent and matches project conventions.

### async void (Allowed Only for Event Handlers)

Per standard C# practice, `async void` is only acceptable for event handlers. The codebase correctly limits `async void` to UI event handler methods:

**Event handlers (acceptable):**
- `CommandManagementWindow.axaml.cs:29` — `private async void OnRenameCommandClicked(object? sender, RoutedEventArgs e)`
- `CommandManagementWindow.axaml.cs:42` — `private async void OnAddTemplateClicked(object? sender, RoutedEventArgs e)`
- `CommandManagementWindow.axaml.cs:51` — `private async void OnUpdateTemplateKeyClicked(object? sender, RoutedEventArgs e)`
- `CommandManagementWindow.axaml.cs:64` — `private async void OnUpdateTemplateValueClicked(object? sender, RoutedEventArgs e)`
- `CommandManagementWindow.axaml.cs:77` — `private async void OnCommandRowDoubleTapped(object? sender, TappedEventArgs e)`
- `CommandManagementWindow.axaml.cs:93` — `private async void OnTemplateRowDoubleTapped(object? sender, TappedEventArgs e)`
- `DirectoryEditDialog.axaml.cs:36` — `private async void OnBrowseClicked(object? sender, RoutedEventArgs e)`
- `DiscoverAgentsWindow.axaml.cs:95` — `private async void OnScanClicked(object? sender, RoutedEventArgs e)`
- `DirectoryManagementWindow.axaml.cs:29` — `private async void OnRenameClicked(object? sender, RoutedEventArgs e)`
- `DirectoryManagementWindow.axaml.cs:42` — `private async void OnMergeClicked(object? sender, RoutedEventArgs e)`
- `DirectoryManagementWindow.axaml.cs:55` — `private async void OnRowDoubleTapped(object? sender, TappedEventArgs e)`

All 11 instances are `private` event handler methods with sender/EventArgs signatures — the accepted pattern for `async void`.

**No violations of the async void rule found** (no non-event-handler async void methods).

### Async Method Naming

A search confirms that async methods do NOT use the `Async` suffix convention consistently:
- `ScanAsync()` (DiscoverAgentsViewModel.cs:94) — has `Async` suffix
- `LaunchAsync()` (LauncherEntryViewModel.cs:181) — has `Async` suffix
- `EditAsync()` (LauncherEntryViewModel.cs:184) — has `Async` suffix
- `AddEntryAsync()` (EntryListViewModel.cs:119) — has `Async` suffix
- `CloneEntryAsync()` (EntryListViewModel.cs:144) — has `Async` suffix

However, Event handlers like `OnRenameCommandClicked` (not suffixed with Async) are acceptable as they are event handlers and not intended to be awaited.

**No violations found** — async methods consistently use the `Async` suffix where appropriate.

## Summary

| Category | Violations |
|---|---|
| `.Result` (sync-over-async) | 0 violations |
| `Task.Wait()` (sync-over-async) | 0 violations |
| ConfigureAwait consistency | 0 violations |
| async void on non-event-handlers | 0 violations |
| Async method naming suffix | 0 violations |
