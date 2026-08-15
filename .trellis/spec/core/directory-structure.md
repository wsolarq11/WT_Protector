# Directory Structure — Core Layer

> How the project is organized across its 5 logical layers.

---

## 5-Layer Architecture

The codebase is organized in five layers. **Each layer depends only on the layers below it.**

```
Layer 0 ─ WTLauncher.Core/           纯数据/规则（零Avalonia依赖）
Layer 1 ─ ViewModels (Commands)      命令本地 mutation（add/edit/delete 流程）
Layer 2 ─ ViewModels (Persistence)   Snapshot-then-save-then-rollback
Layer 3 ─ ViewModels (Orchestration) 单 MainWindowViewModel 主控
Layer 4 ─ Views/                     XAML + code-behind + drag-drop
```

**Reference:** `src/avalonia-launcher/README.md` — 5-layer model description + key invariants.
**Structure facts** (file counts, partials, test files, gate checks) are single-sourced in
`AGENTS.md` 结构基线 and machine-enforced by gate check `structure-baseline-consistency`
— do not hardcode drifting counts in this spec; consult the baseline table instead.

---

## Layer 0 — Pure Data & Contracts (Core)

`src/WTLauncher.Core/` — 53 C# files, zero framework dependencies.

| Responsibility | Key Files |
|---------------|-----------|
| Data records | `LaunchEntry.cs`, `LaunchPlan.cs`, `LauncherPreset.cs`, `LauncherSettings.cs`, `LauncherStoreFile.cs` |
| Argument building | `LaunchArgumentBuilder.cs` — `wt.exe` quoting with Windows rules |
| JSON persistence | `LauncherJson.cs` — atomic temp-file + `File.Replace` (AOT-safe source-gen `LauncherJsonContext`) |
| Declared settings defaults | `LauncherSettingsDefaults.cs` — load-time normalization for missing optional keys |
| Design tokens | `LauncherDesignTokens.cs` — colors, spacing, sizing (single source) |
| Config store | `LauncherStore.cs` — load/validate + legacy migration |
| Pure rules | `LauncherEntryOrder.cs`, `LauncherEntryLayoutCalculator.cs`, `LauncherDragGhostLayoutCalculator.cs`, `LauncherEntryQuery.cs`, `LauncherQuickMatch.cs`, `LauncherEntryGrouper.cs` |
| Directory/agent discovery | `DirectoryPath.cs`, `AgentDiscoveryScanner.cs`, `AgentCatalog.cs`, `AgentCatalogEntry.cs`, `AgentBrandMatcher.cs` |
| Run history / updates / hotkey | `LauncherRunHistory.cs`, `LauncherUpdateManifest.cs`, `LauncherHotkey.cs` |
| View mode enum | `LauncherEntryViewMode.cs` — Cards / Details / Names |

**Rule:** Core never references `Avalonia.*` or `CommunityToolkit.Mvvm`. If you're tempted to add one, the logic probably belongs in ViewModels.

---

## Layer 1 — Command-Local Mutations (ViewModels)

`src/WTLauncher.App/ViewModels/MainWindowViewModel*.cs` — command handlers own **validate → mutate → save**; shared rules stay in narrow helpers (`LauncherEntryDrafts`, `LauncherEntryReferences`, `Features/Entries/EntryListViewModel`). Don't create a second ViewModel workflow when there's only one caller.

---

## Layer 2 — Persistence + Rollback (ViewModels + Infrastructure)

`src/WTLauncher.App/Infrastructure/` — `StoreGrain` is the single state container (all mutations through `Dispatch`/`DispatchErrorOr`, analyzer-enforced as the only write path); `FileStorePersistence` (implements `IStorePersistence`) with instance `SemaphoreSlim(1,1)` on `Save` (defense-in-depth); `FileLauncherJsonIO` (implements `ILauncherJsonIO`) holds the JSON IO. **Rollback = save-first-then-commit:** `Dispatch` saves the reducer result to disk *before* applying it to memory, so a failed save leaves memory unchanged and surfaces the error (snapshot classes were removed 2026-07-21). The store is a single file (`config/launcher.json`) owning entries + presets + settings + defaultPresetId as one atomic unit.

---

## Layer 3 — ViewModel Orchestration

`src/WTLauncher.App/ViewModels/MainWindowViewModel*.cs` — **partial class: 1 main + 15 partials (16 files, ~2100 lines total)**:

| File | Lines | Responsibility |
|------|------:|----------------|
| `MainWindowViewModel.cs` | 437 | Constructor, core state, refresh, save helpers |
| `MainWindowViewModel.Batch.cs` | 166 | Selection ops + batch launch |
| `MainWindowViewModel.CommandManagement.cs` | 172 | Command add/edit/delete flows |
| `MainWindowViewModel.DirectoryManagement.cs` | 100 | Directory management commands |
| `MainWindowViewModel.Discovery.cs` | 108 | Agent discovery orchestration |
| `MainWindowViewModel.Drag.cs` | 143 | Drag reorder session |
| `MainWindowViewModel.Entries.cs` | 41 | Entry command helpers (thin — logic moved to `Features/Entries/`) |
| `MainWindowViewModel.Grouping.cs` | 101 | Grouping / section logic |
| `MainWindowViewModel.History.cs` | 19 | Run-history projection |
| `MainWindowViewModel.IEntryCardHost.cs` | 29 | Card-host interface |
| `MainWindowViewModel.IEntryListViewModel.cs` | 15 | List-VM interface |
| `MainWindowViewModel.Layout.cs` | 200 | Filter / sort / view-mode / layout |
| `MainWindowViewModel.Migration.cs` | 100 | Legacy config migration |
| `MainWindowViewModel.Presets.cs` | 385 | Preset CRUD + apply / preview / launch |
| `MainWindowViewModel.Reorder.cs` | 72 | Keyboard MoveUp / MoveDown |
| `MainWindowViewModel.Update.cs` | 11 | Update-check wiring |

- The single writable owner of launcher UI state; delegates reusable rules to Core helpers; injects dialog `Func<>` delegates through the `MainWindow` constructor.
- **Decomposition direction:** further splits are composition — separate collaborator classes (`LaunchOrchestrator`, `PresetManager`, `AppStateMachine`, `Features/Entries/EntryListViewModel`, `RunHistoryViewModel`), NOT more partials.

---

## Layer 4 — Views & Drag-Drop

`src/WTLauncher.App/Views/` — 44 files (24 C# + 20 AXAML). XAML with three view modes (Cards / Details / Names), drag ghost + insert indicators, code-behind windowing only. Full file list in [Views / Directory Structure](../views/directory-structure.md).

---

## Project File Layout

```
src/avalonia-launcher/
├── README.md                           # 5-layer architecture docs
├── run.ps1                             # Dev run script
├── verify.ps1                          # Mainline gate (restore → test → publish → smoke)
├── src/
│   ├── WTLauncher.Core/                # Layer 0: 53 C# files
│   ├── WTLauncher.Analyzers/           # Roslyn analyzers: 7 C# files
│   └── WTLauncher.App/
│       ├── Program.cs                  # Entry point
│       ├── App.axaml / App.axaml.cs    # Design token injection
│       ├── ViewModels/                 # Layer 1-3: 56 C# files (MainWindowViewModel + 15 partials + collaborators)
│       ├── Features/                   # 16 C# files (Entries/Presets/Status/Updates/Dialogs/ConfigExchange)
│       ├── Infrastructure/             # 9 C# files (persistence IO + IStorePersistence)
│       ├── Platform/                   # 3 C# files (platform wiring)
│       └── Views/                      # Layer 4: 44 files (24 C# + 20 AXAML)
├── tests/WTLauncher.Tests/             # 122 xUnit C# files, 8 layers, 739 [Fact]/[Theory] (gate runs 822)
└── artifacts/                          # Build outputs (gitignored)
```

---

## Naming Conventions

- **Files:** PascalCase matching the primary type they contain.
- **Partial classes:** `MainWindowViewModel.<Area>.cs` — area name in PascalCase.
- **Tests:** `<TypeUnderTest>Tests.cs` or `<TypeUnderTest>Tests.<Area>Tests.cs` for split test files.
- **Core namespace:** `WTLauncher.Core`
- **App namespace:** `WTLauncher.App.ViewModels`, `WTLauncher.App.Views`, `WTLauncher.App.Features`
- **Test namespace:** `WTLauncher.Tests`

---

## Where to Put New Code

| What you're adding | Where it goes |
|-------------------|---------------|
| Pure data type with no UI deps | `WTLauncher.Core/` |
| Validation/normalization logic | `WTLauncher.Core/` (if pure) or ViewModels helper |
| A new user command | `MainWindowViewModel.cs` (or a `MainWindowViewModel.<Area>.cs` partial) |
| A feature slice with its own VM + projection | `WTLauncher.App/Features/<Feature>/` |
| Entry-level ViewModel property | `LauncherEntryViewModel.cs` or `.Layout.cs` / `.Drag.cs` |
| A dialog window | `Views/` — one file per dialog (+ `.axaml`) |
| XAML-only styling | `App.axaml` resources or local `Styles` |
