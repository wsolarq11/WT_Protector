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

---

## Layer 0 — Pure Data & Contracts (Core)

`src/WTLauncher.Core/` — 26 files, zero framework dependencies.

| Responsibility | Key Files |
|---------------|-----------|
| Data records | `LaunchEntry.cs`, `LaunchPlan.cs`, `LauncherPreset.cs`, `LauncherSettings.cs` |
| Argument building | `LaunchArgumentBuilder.cs` — `wt.exe` quoting with Windows rules |
| JSON persistence | `LauncherJson.cs` — atomic temp-file + `.bak` rotation |
| Design tokens | `LauncherDesignTokens.cs` — colors, spacing, sizing (single source) |
| Config store | `LauncherConfigStore.cs` — load/validate/normalize pipeline |
| Pure rules | `LauncherEntryOrder.cs`, `LauncherEntryLayoutCalculator.cs`, `LauncherDragGhostLayoutCalculator.cs` |
| View mode enum | `LauncherEntryViewMode.cs` — Cards / Details / Names |

**Rule:** Core never references `Avalonia.*` or `CommunityToolkit.Mvvm`. If you're tempted to add one, the logic probably belongs in ViewModels.

---

## Layer 1 — Command-Local Mutations (ViewModels)

`src/WTLauncher.App/ViewModels/MainWindowViewModel.Entries.cs`

- Entry add/edit/delete command handlers own: **validate → mutate → save**.
- Shared rules stay in narrow helpers (`LauncherEntryDrafts`, `LauncherEntryReferences`).
- Don't create a second ViewModel workflow when there's only one caller.

---

## Layer 2 — Persistence + Rollback (ViewModels)

`src/WTLauncher.App/ViewModels/` — `LauncherEntryMutationPersistence.cs`, `LauncherEntryMutationSnapshot.cs`

- **Snapshot-before → mutate → save-all → restore-on-failure**.
- Coordinates `config.json` + `presets.json` + `settings.json` as an atomic unit.
- Rollback restores from deep-cloned snapshots (`entry with { }` pattern).

Key files:
- `LauncherEntryMutationSnapshot.cs` — captures entries + presets + custom order + selection
- `LauncherEntryMutationPersistence.cs` — save config → save presets → save settings → rollback on failure
- `LauncherConfigMutationPersistence.cs`, `LauncherPresetMutationPersistence.cs`, `LauncherSettingsMutationPersistence.cs`

---

## Layer 3 — ViewModel Orchestration

`src/WTLauncher.App/ViewModels/MainWindowViewModel.cs` (9 partial class files)

- The single writable owner of launcher UI state.
- Delegates reusable rules to Core helpers.
- Injects dialog confirmation `Func<>` delegates through the `MainWindow` constructor.

Partial file breakdown:
| File | Responsibility |
|------|---------------|
| `MainWindowViewModel.cs` | Constructor, core state, refresh |
| `MainWindowViewModel.Entries.cs` | Add/edit/delete/clone commands |
| `MainWindowViewModel.Launch.cs` | Single + batch launch workflows |
| `MainWindowViewModel.Selection.cs` | Checkbox selection + batch select/clear |
| `MainWindowViewModel.Layout.cs` | Card density, view modes, sizing, search/filter + sort |
| `MainWindowViewModel.Presets.cs` | Preset selection + application |
| `MainWindowViewModel.Presets.Persistence.cs` | Preset save/update/delete |
| `MainWindowViewModel.Presets.Helpers.cs` | Preset reference counting |
| `MainWindowViewModel.Drag.cs` | Drag reorder session management |
| `MainWindowViewModel.Reorder.cs` | Move up/down commands |

---

## Layer 4 — Views & Drag-Drop

`src/WTLauncher.App/Views/` — 15 files

- XAML with three view modes: Cards / Details / Names.
- Drag reorder: ghost factory (FLIP animation removed 2026-06-23).
- Dialog windows: confirmations, entry editor, preset editor.
- Focused view helpers for shared drag/hit-test behavior.

---

## Project File Layout

```
src/avalonia-launcher/
├── README.md                           # 5-layer architecture docs
├── run.ps1                             # Dev run script
├── verify.ps1                          # Mainline gate (restore → test → publish → smoke)
├── src/
│   ├── WTLauncher.Core/                # Layer 0: 26 files
│   └── WTLauncher.App/
│       ├── Program.cs                  # Entry point
│       ├── App.axaml / App.axaml.cs    # Design token injection
│       ├── ViewModels/                 # Layer 1-3: 34 files
│       └── Views/                      # Layer 4: 15 files
├── tests/WTLauncher.Tests/             # 77 xUnit test files
└── artifacts/                          # Build outputs (gitignored)
```

---

## Naming Conventions

- **Files:** PascalCase matching the primary type they contain.
- **Partial classes:** `MainWindowViewModel.<Area>.cs` — area name in PascalCase.
- **Tests:** `<TypeUnderTest>Tests.cs` or `<TypeUnderTest>Tests.<Area>Tests.cs` for split test files.
- **Core namespace:** `WTLauncher.Core`
- **App namespace:** `WTLauncher.App.ViewModels`, `WTLauncher.App.Views`
- **Test namespace:** `WTLauncher.Tests`

---

## Where to Put New Code

| What you're adding | Where it goes |
|-------------------|---------------|
| Pure data type with no UI deps | `WTLauncher.Core/` |
| Validation/normalization logic | `WTLauncher.Core/` (if pure) or ViewModels helper |
| A new user command | `MainWindowViewModel.<Area>.cs` partial |
| Entry-level ViewModel property | `LauncherEntryViewModel.cs` or `.Layout.cs` / `.Drag.cs` |
| A dialog window | `Views/` — one file per dialog |
| XAML-only styling | `App.axaml` resources or local `Styles` |
