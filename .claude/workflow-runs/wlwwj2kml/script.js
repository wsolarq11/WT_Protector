const r1 = agent("fix medium core", {
  description: "Fix MEDIUM findings in Core layer",
  subagent_type: "general-purpose",
  prompt: `You have full authority to edit files. Fix these ultrareview MEDIUM findings in WTLauncher.Core project. Project root: D:\\<PROJECT_ROOT>\\WT_Protector\\

1. LauncherEntryText.cs: RequireNonEmpty has wrong error message "Entry name is required" when used for preset names. Change it to accept a string? fieldName param with default "Entry name". Use the fieldName in the error message.

2. LauncherConfigStore.cs line 100: defaultValue not normalized. The Load method passes Array.Empty<LaunchEntry>() as defaultValue to LoadWithRecovery. Fix: pass NormalizeEntries(Array.Empty<LaunchEntry>().ToImmutableList()) or similar pre-validated default.

3. LauncherEntryVisibility.cs line 44: Redundant .Trim() after Normalize already trims. Remove the redundant Trim(), keeping only Normalize.

4. CommitJournal.cs: JournalEntry record has no [JsonPropertyName] attributes. Add [JsonPropertyName("temp")] and [JsonPropertyName("target")] to the record properties.

5. LauncherEntryVisibility.cs lines 229-236: RequireText duplicates RequireNonEmpty logic. Remove RequireNonEmpty and use RequireText with a fieldName parameter everywhere.

Read each file completely, find the exact code, apply fixes with Edit tool. Be precise with old_string matching.`,
  run_in_background: true
});

const r2 = agent("fix medium app vm", {
  description: "Fix MEDIUM findings in ViewModels",
  subagent_type: "general-purpose",
  prompt: `You have full authority to edit files. Fix these ultrareview MEDIUM findings in WTLauncher.App/ViewModels. Project root: D:\\<PROJECT_ROOT>\\WT_Protector\\

1. LauncherEntryViewModel.cs: MoveUpCommand and MoveDownCommand methods (around line 156-164) have no try-catch for exceptions from _owner.MoveEntryUp/MoveEntryDown. The synchronous [RelayCommand] methods don't have automatic exception handling. Add try-catch with ExceptionFilters.IsRecoverable(ex) pattern.

2. EntryDetailsWindow.cs around line 168: Raw InvalidDataException ex.Message is displayed in error UI instead of using ExceptionMessageFormatter.ForUser(ex). Change to use ForUser(ex).

3. MainWindowViewModel.cs: Find the RollbackFailed method in LauncherPersistenceStatus class. The 3-parameter overload (with restoreErrors) is never called. If confirmed dead, delete it.

4. LauncherEntryDrafts.cs: EntryDraft positional record has no null guards on string fields. Add string.Empty defaults to the record properties.

Read each file completely, find the exact code, apply fixes with Edit tool. Be precise with old_string matching.`,
  run_in_background: true
});

const r3 = agent("fix medium views tests", {
  description: "Fix MEDIUM+LOW findings in Views and Tests",
  subagent_type: "general-purpose",
  prompt: `You have full authority to edit files. Fix these ultrareview MEDIUM+LOW findings. Project root: D:\\<PROJECT_ROOT>\\WT_Protector\\

1. MainWindowDragGhostFactory.cs: No NaN guard on sourceWidth/sourceHeight passed to Calculate. Add guard: var sw = double.IsFinite(sourceWidth) ? sourceWidth : 0; same for sourceHeight. Line ~25.

2. LauncherEntryLayoutCalculator.cs: The column-count loop iterates count times unbounded. Add a cap: use count = Math.Min(count, 10000) before the loop. Line ~53.

3. MainWindow.axaml: Add AutomationProperties.Name to empty-state TextBlocks. Look for x:Name="EmptyStateCardsText", x:Name="EmptyStateDetailsText", x:Name="EmptyStateNamesText". Add AutomationProperties.Name="无可见按钮" or similar.

4. PresetDetailsWindow.cs lines ~73-90: The Submit() method validates name + duplicate names directly. Add a ponytail comment acknowledging this is intentional duplicate of ViewModel validation (UI-feedback-before-save pattern).

Read each file completely, find the exact code, apply fixes with Edit tool. Be precise with old_string matching.`,
  run_in_background: true
});

return { r1, r2, r3 };
