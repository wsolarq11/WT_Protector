# Implement — Per-agent Branding

Ordered checklist. Each step is independently testable. Stop and fix if any step breaks the gate.

## Phase 1 — Core data model (Layer 0)

- [ ] **1.1** `AgentCatalogEntry.cs`: add `public required string BrandColorHex { get; init; }` with XML doc comment noting "approximate, not vendor-official".
- [ ] **1.2** `AgentCatalog.cs`: populate `BrandColorHex` for all 8 entries per the design's color table. Add a top-of-file comment: "Brand colors are approximate visual signals, not vendor-pulled assets."
- [ ] **1.3** `LauncherDesignTokens.cs`: add three constants — `BrandBadgeSize = 22`, `BrandBadgeCornerRadius = 6`, `BrandBadgeForegroundColor = "White"`.
- [ ] **1.4** Create `AgentBrandMatcher.cs` in `WTLauncher.Core`:
  ```csharp
  public static class AgentBrandMatcher
  {
      public static bool TryMatch(string? command, out AgentCatalogEntry entry)
      {
          entry = default!;
          if (string.IsNullOrWhiteSpace(command)) return false;
          var token = command.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
          if (string.IsNullOrEmpty(token)) return false;
          var name = Path.GetFileNameWithoutExtension(token);
          if (string.IsNullOrEmpty(name)) return false;
          foreach (var candidate in AgentCatalog.Entries)
          {
              if (candidate.ExecName.Equals(name, StringComparison.OrdinalIgnoreCase))
              {
                  entry = candidate;
                  return true;
              }
          }
          return false;
      }
  }
  ```
- [ ] **1.5** Build Core: `dotnet build src/avalonia-launcher/src/WTLauncher.Core` — must compile clean with `-warnaserror`.

## Phase 2 — Core tests

- [ ] **2.1** Create `AgentBrandMatcherTests.cs`:
  - `[Theory]` match cases: `"codex"`, `"CODEX"`, `"codex.exe"`, `"codex.cmd"`, `"C:\\path\\codex.cmd"`, `"codex --flag --etc"`, `"  claude  "` → each asserts `TryMatch` returns `true` and `entry.ExecName` is the lowercased expected.
  - `[Theory]` no-match cases: `""`, `"   "`, `"unknownbin"`, `"cod"` (prefix-only), `null` → each asserts `false`.
- [ ] **2.2** Extend `AgentCatalogTests.cs`:
  - `[Fact]` every entry has `BrandColorHex` matching `^#[0-9A-Fa-f]{6}$`.
  - `[Fact]` all `BrandColorHex` values are pairwise distinct.
- [ ] **2.3** Run tests: `dotnet test src/avalonia-launcher/WTLauncher.Avalonia.sln -c Release --filter "FullyQualifiedName~AgentBrandMatcher|AgentCatalog"` — all green.

## Phase 3 — App ViewModel (Layer 1-3)

- [ ] **3.1** `LauncherEntryViewModel.cs`: add two private fields + public read-only properties:
  ```csharp
  private string _brandColorHex = string.Empty;
  private string _brandGlyph = string.Empty;
  public string BrandColorHex => _brandColorHex;
  public string BrandGlyph => _brandGlyph;
  ```
- [ ] **3.2** Add `RefreshBrand()` private method per design contract. Call it from the constructor (after `_entry` assignment) and from `RefreshDisplay()` (before the layout refresh).
- [ ] **3.3** In `RefreshDisplay()`, add `OnPropertyChanged(nameof(BrandColorHex))` and `OnPropertyChanged(nameof(BrandGlyph))`.
- [ ] **3.4** Build App: `dotnet build src/avalonia-launcher/src/WTLauncher.App` — must compile clean.

## Phase 4 — Views (Layer 4)

- [ ] **4.1** `App.axaml` (or the brush/resource declaration file): add `<x:Double x:Key="BrandBadgeSize">22</x:Double>`, `<x:Double x:Key="BrandBadgeCornerRadius">6</x:Double>`, and a `<SolidColorBrush x:Key="BrandBadgeForegroundBrush">` bound to a `<Color x:Key="BrandBadgeForegroundColor">` sourced from the token.
- [ ] **4.2** `MainWindow.axaml` card header: change `ColumnDefinitions="Auto,*"` to `"Auto,Auto,*"`. Insert the brand-badge `Border` (per design XAML) at `Grid.Column="1"`. Shift the title `TextBlock` to `Grid.Column="2"`. Keep the `CheckBox` at column 0.
- [ ] **4.3** `DiscoverAgentsWindow.cs`: prepend a brand swatch (`Border` with `Background = catalogEntry.BrandColorHex`, child `TextBlock` with glyph) to each agent row. This is code-behind since the dialog is built procedurally.
- [ ] **4.4** Confirm no hardcoded color hexes in the new XAML (only `{Binding BrandColorHex}` and `{StaticResource BrandBadgeForegroundBrush}`). Grep `#` in the diff to verify.

## Phase 5 — Gate

- [ ] **5.1** Run Avalonia mainline verify: `powershell -NoProfile -ExecutionPolicy Bypass -File .\src\avalonia-launcher\verify.ps1` — must PASS (restore + test + publish + GUI smoke).
- [ ] **5.2** Run root gate: `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\verify.ps1` — must PASS with all 19 existing checks + new test count.
- [ ] **5.3** Manual: launch the app, confirm Claude/Codex/etc. entries show distinct badges. Open the discovery dialog, confirm swatches appear. (Not gated; sanity check.)

## Phase 6 — Docs

- [ ] **6.1** Update `README.md` "Current implementation status" — add a bullet: "- per-agent brand badges on cards (color + 2-letter glyph) for catalog-known agents".
- [ ] **6.2** Bump the feature audit: add feature #23 "Per-agent brand badges" to `docs/feature-reality-north-star-audit.md` with `Verdict level: narrow-north-star`. Update the gate's count expectations (22 → 23). Update README/AGENTS check counts if a new gate check was added (likely not — no new check needed for this task).
- [ ] **6.3** Update `AGENTS.md` repo map or feature count references if applicable.

## Phase 7 — Finish

- [ ] **7.1** Spawn verification agent with the PRD acceptance criteria list.
- [ ] **7.2** On PASS: commit as `feat(branding): per-agent brand badges on cards`.
- [ ] **7.3** Archive task: `task.py archive 07-10-per-agent-branding`.

## Validation commands

```powershell
# Per-phase quick checks
dotnet build src/avalonia-launcher/src/WTLauncher.Core -warnaserror
dotnet test src/avalonia-launcher/WTLauncher.Avalonia.sln -c Release --filter "FullyQualifiedName~AgentBrandMatcher|AgentCatalog"
dotnet test src/avalonia-launcher/WTLauncher.Avalonia.sln -c Release

# Full gate
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\verify.ps1
```

## Rollback point

If any phase fails beyond repair: `git checkout -- .` reverts all working-tree changes (nothing committed until Phase 7). No migration, no persisted state to undo.
