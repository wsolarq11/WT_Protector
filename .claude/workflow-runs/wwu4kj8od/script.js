export const meta = {
  name: 'single-file-architecture',
  description: 'Consolidate 3 config files into 1, eliminate journal/snapshot/multi-commit',
  phases: [
    { title: 'Model', detail: 'Create LauncherStoreFile record, LauncherStore load/save' },
    { title: 'Rewrite', detail: 'Update ViewModel, Program.cs, delete old files' },
    { title: 'Migrate', detail: 'Update tests, migrate config files' },
    { title: 'Verify', detail: 'Build + full test suite' },
  ],
}

// Phase 1: Create the new store model and load/save
const modelResult = await agent(`Create the single-file store architecture for WT_Protector.

BASE: D:/<PROJECT_ROOT>/WT_Protector/src/avalonia-launcher/src/WTLauncher.Core/

STEP 1: Create LauncherStoreFile.cs — a record wrapping all three sections:
- SchemaVersion (int, default 2)
- Settings (LauncherSettings)
- Entries (ImmutableList<LaunchEntry>)
- Presets (ImmutableList<LauncherPreset>)
- DefaultPresetId (string, default "")

Use [JsonPropertyName] attributes for camelCase JSON. Use [JsonRequired] where appropriate.

STEP 2: Create LauncherStore.cs — static class with:
- static LauncherStoreFile Load(string storePath) — single call replacing 3 separate Load calls
- static void Save(LauncherStoreFile store, string storePath, bool backup = true) — single atomic save
- static string ResolveStorePath(string? explicitPath, string? currentDir, string? appBaseDir) — replaces ResolveConfigPath
- Internal: normalization/validation of entries, presets, and settings during load (port existing logic from LauncherConfigStore, LauncherPresetStore, LauncherSettingsStore)

STEP 3: Read ALL existing store files to port validation logic:
- LauncherConfigStore.cs, LauncherPresetStore.cs, LauncherSettingsStore.cs
- LauncherSettingsCopies.cs (already deleted — inline the SchemaVersion=1 pattern)
- LauncherPresetFiles.cs (for preset validation)
- LauncherEntryIds.cs, LauncherPresetIds.cs

Create both new files. Run dotnet build. If it fails, fix and retry. Report what you created.`, { phase: 'Model', label: 'create:store-model' })

// Phase 2: Rewrite callers after model is ready
const rewriteResult = await agent(`Update all callers to use the new single-file LauncherStore.

After the new LauncherStore.cs and LauncherStoreFile.cs are created in the Core layer, update:

1. Program.cs (D:/<PROJECT_ROOT>/WT_Protector/src/avalonia-launcher/src/WTLauncher.App/Program.cs):
   - Replace ResolveConfigPath + ResolveSettingsPath with ResolveStorePath
   - Replace 3 separate Load calls with 1 LauncherStore.Load
   - Pass LauncherStoreFile to MainWindowViewModel constructor instead of 3 paths

2. MainWindowViewModel.cs (ALL partials):
   - Replace _configPath, _presetPath, _settingsPath with single _storePath
   - Replace 3 separate Save paths with single LauncherStore.Save(storeFile, _storePath, true)
   - Remove LauncherEntryMutationPersistence usage
   - Simplify DiscardDragReorderPreview, SaveEntryCollectionChanges, SavePresetMutationAndApply

3. Also clean up:
   - Remove LauncherPresetStore.ResolvePresetPath (presets now in same file as config)
   - LauncherSettingsStore.TrySaveWindowBounds now calls LauncherStore.Save directly

IMPORTANT: This is a large refactoring. Read EVERY caller file first. Make minimal changes to wire up the new store. Keep ViewModel behavior identical — only change the persistence layer.

Run dotnet build. Fix errors. Report what changed.`, { phase: 'Rewrite', label: 'rewrite:callers' })

// Phase 3: Update tests and migrate config files  
const migrateResult = await agent(`Update tests and config files for single-file architecture.

1. Update config files at D:/<PROJECT_ROOT>/WT_Protector/config/:
   - Read config.json, settings.json, presets.json
   - Combine into launcher.json (single file, schemaVersion:2)
   - The old files can stay as reference

2. Update test fixtures:
   - ALL tests that create TempDirectory with config/settings/presets paths — change to single storePath
   - Test fixtures that call LauncherConfigStore.Load → LauncherStore.Load
   - Test fixtures that call LauncherSettingsStore.Load → same LauncherStore.Load (from .Settings property)
   - Test fixtures that call LauncherPresetStore.Load → same LauncherStore.Load (from .Presets property)
   - Remove tests that test deleted files (CommitJournal tests, MutationPersistence tests, etc.)

3. Remove deleted files from csproj: Check WTLauncher.Core.csproj and WTLauncher.Tests.csproj for any <Compile Remove> or <Compile Include> of deleted files.

Run dotnet build and dotnet test. Fix ALL failures. Report test count and any remaining issues.`, { phase: 'Migrate', label: 'migrate:tests-config' })

phase('Verify')
const final = await agent(
  `Final verification: cd D:/<PROJECT_ROOT>/WT_Protector/src/avalonia-launcher && dotnet build 2>&1 | tail -5 && dotnet test --no-build 2>&1 | tail -5. Report exact pass/fail/skip counts. If tests fail, report ALL details — DO NOT SKIP ANY FAILURE.`,
  { phase: 'Verify', label: 'verify:final' }
)

return { model: modelResult?.slice(0, 300), rewrite: rewriteResult?.slice(0, 300), migrate: migrateResult?.slice(0, 300), final: final?.slice(0, 500) }
