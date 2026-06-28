export const meta = {
  name: 'complete-100-percent',
  description: 'Fix every remaining finding to reach 100% completion',
  phases: [
    { title: 'Schema', detail: 'Add schema versions, mutex, remove dead code' },
    { title: 'Tests', detail: 'Fix all remaining test assertions' },
    { title: 'Cosmetic', detail: 'Fix XAML duplication, naming, cleanup' },
    { title: 'Verify', detail: 'Build and test everything' },
  ],
}

// ITEM 1: Add named mutex for multi-instance prevention
const mutexFix = await agent(`Add a named mutex to prevent multiple launcher instances from corrupting config files in D:/<PROJECT_ROOT>/WT_Protector/src/avalonia-launcher/src/WTLauncher.App/Program.cs.

Currently there is no file locking or mutex — two instances writing to the same config can interleave their save operations (last-writer-wins). This is documented as a limitation in LauncherJson.cs.

THE FIX: Add a named Mutex at startup (before config loading). Use "Global\\WTLauncher_ConfigMutex" as the name. If another instance holds the mutex, show a message box and exit gracefully. Acquire the mutex before any config operations (wrap in try/finally for release). This is an optional named mutex — if acquisition fails (e.g., permissions), log a warning but continue (best-effort).

Read Program.cs completely. Apply the minimal fix. Run dotnet build and dotnet test. Report what you changed.`, { phase: 'Schema', label: 'fix:mutex' })

// ITEM 2: Add schemaVersion to config.json with backward compat
const schemaFix = await agent(`Add schemaVersion support to config.json in D:/<PROJECT_ROOT>/WT_Protector/src/avalonia-launcher/src/WTLauncher.Core/LauncherConfigStore.cs.

THE BUG: settings.json and presets.json both have schemaVersion fields with version gating (reject old/new). config.json has NO schemaVersion at all, making future migrations impossible without silent data loss.

THE FIX: Add a wrapper record "LauncherConfigFile" with a SchemaVersion field and an Entries list. Update Load/Save to use this wrapper. SchemaVersion defaults to 1. Add validation: reject schemaVersion > current (newer app version). Accept schemaVersion <= current (forward compat for migration). Write schemaVersion 1 on save. Add a migration comment block showing the pattern for future version bumps.

Read LauncherConfigStore.cs completely. Apply changes. Run dotnet build and dotnet test — this will break some tests that create raw JSON arrays, update them to use the wrapper format. Report what you changed and test results.`, { phase: 'Schema', label: 'fix:schema-version' })

// ITEM 3: Remove dead code — MainWindowEntriesRootSelection.Select
const deadCodeFix = await agent(`Remove dead code in D:/<PROJECT_ROOT>/WT_Protector/src/avalonia-launcher/src/WTLauncher.App/Views/MainWindow.axaml.cs.

The public static method "MainWindowEntriesRootSelection.Select" around line 375-393 is defined but never called from any production code. It has dedicated unit tests in MainWindowEntriesRootSelectionTests.cs.

THE FIX: If the method is truly unused:
1. Mark it with [Obsolete] or remove it
2. Remove or update the tests that reference it
Read MainWindow.axaml.cs around the method, check if it's called anywhere (grep for "Select" and "MainWindowEntriesRootSelection"). If truly dead, remove it and update tests. Report what you found.`, { phase: 'Schema', label: 'fix:dead-code' })

// ITEM 4: Fix all remaining exception.Message assertions in tests
const allMsgFix = await agent(`Fix ALL remaining exception.Message format assertions in the WT_Protector test suite. The .NET ArgumentException.Message format can change between runtime versions, making these tests brittle.

Search for ALL Assert.Equal with exception.Message using pattern: grep for "Assert.Equal.*exception.Message" in D:/<PROJECT_ROOT>/WT_Protector/src/avalonia-launcher/tests/

For each match:
- If it tests an ArgumentException (the format "Entry id is required. (Parameter 'Id')" is .NET version dependent):
  Replace with Assert.Contains + Assert.Equal(exception.ParamName)
- If it tests an app-defined exception (InvalidDataException, InvalidOperationException with the app's own message):
  Keep as-is — the app controls the message format.

Fix ALL ArgumentException.Message tests. Run dotnet test to verify. Report count of files changed.`, { phase: 'Tests', label: 'fix:all-exception-msg' })

// ITEM 5: Fix empty-state text duplication in XAML
const xamlFix = await agent(`Fix duplicated empty-state text in D:/<PROJECT_ROOT>/WT_Protector/src/avalonia-launcher/src/WTLauncher.App/Views/MainWindow.axaml.

THE ISSUE: The string "没有匹配的启动项" appears 3 times (lines ~586, ~759, ~863) in the three view mode containers (Cards, Details, Names).

THE FIX: Extract this string to a resource key and reference it. Or bind to a ViewModel property. The simplest approach: add a static string property (e.g., "EmptyStateText") to the ViewModel that returns the localized text, and bind all three TextBlocks to "{Binding EmptyStateText}".

Read MainWindow.axaml to find the exact locations. Add a property to MainWindowViewModel.cs. Run dotnet build. Report changes.`, { phase: 'Cosmetic', label: 'fix:xaml-dupe' })

// ITEM 6: Fix body token semantic mismatch in LauncherDialogText
const tokenFix = await agent(`Fix the semantic token mismatch in D:/<PROJECT_ROOT>/WT_Protector/src/avalonia-launcher/src/WTLauncher.App/Views/LauncherDialogText.cs.

THE BUG: BodyBrush is initialized from LauncherDesignTokens.ControlBoxLabelColor, but "ControlBoxLabel" semantically refers to control-box header labels (search/sort/view/preset boxes), not dialog body text. If someone changes the control box label color for better readability, dialog body text color changes unintentionally.

THE FIX: Either:
A) Rename the token usage to a more appropriate design token (e.g., TextMutedColor for muted body text), or
B) Add a comment explaining this is intentional reuse.

Read LauncherDialogText.cs. Make the minimal change. Report.`, { phase: 'Cosmetic', label: 'fix:body-token' })

phase('Verify')
const final = await agent(
  `Final verification: cd D:/<PROJECT_ROOT>/WT_Protector/src/avalonia-launcher && dotnet build 2>&1 | tail -5 && dotnet test --no-build 2>&1 | tail -5. Report exact pass/fail/skip counts. If failures, report details.`,
  { phase: 'Verify', label: 'verify:100percent' }
)

return { mutex: mutexFix?.slice(0, 500), schema: schemaFix?.slice(0, 500), deadCode: deadCodeFix?.slice(0, 300), msgFix: allMsgFix?.slice(0, 300), xaml: xamlFix?.slice(0, 300), token: tokenFix?.slice(0, 300), final: final?.slice(0, 500) }
