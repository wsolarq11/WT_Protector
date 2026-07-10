# Per-agent Branding — Icons + Theme Colors

## Goal

Give each catalog-known agent CLI a **brand color** and a **2-letter glyph** rendered as a small badge on the launcher card. The badge visually distinguishes "this is the Claude entry" from "this is the Codex entry" at a glance, without importing copyrighted logo assets.

This is the v2 follow-up the user selected after the v2 discovery wave landed. It is **visual polish on the existing discovery substrate**, not new functionality.

## Background

The discovery task (`07-10-agent-cli-discovery`) shipped a static `AgentCatalog` with 8 entries (claude / codex / cursor / antigravity / gemini / opencode / aider / qwen-code). Today every launch card looks identical — only the command text differentiates them. Per-agent branding adds a fast visual signal.

## Requirements

### R1 — Core: extend the catalog data model

- `AgentCatalogEntry` gains a required `BrandColorHex` field (string, `#RRGGBB` format).
- `AgentCatalog` populates `BrandColorHex` for all 8 entries with approximate brand colors (not official assets; visually distinct hexes with sufficient white-text contrast).
- New `AgentBrandMatcher` static class in Core: `TryMatch(string command, out AgentCatalogEntry entry)` extracts the first whitespace-delimited token of `command`, strips directory path and Windows extension, and matches against catalog `ExecName` values (ordinal ignore-case). Returns `false` for null/empty/unknown commands.

### R2 — App: surface brand on the entry ViewModel

- `LauncherEntryViewModel` gains two read-only computed properties:
  - `BrandColorHex` — the matched catalog entry's `BrandColorHex`, or empty string when unmatched.
  - `BrandGlyph` — the 2-letter uppercase prefix derived from the matched `ExecName` (`"claude"` → `"CL"`), or empty string when unmatched.
- Both are recomputed and notified in `RefreshDisplay()` so editing the `Command` re-matches live.
- Matching is cached per call (no global state); the matcher is a pure function over the catalog.

### R3 — Views: render the brand badge

- Card header (Cards view): add a small rounded square (`Border`) before the entry Name, sized `LauncherDesignTokens.BrandBadgeSize`, corner-radius `BrandBadgeCornerRadius`, background bound to `BrandColorHex`, containing a white `TextBlock` with `BrandGlyph`. `IsVisible` when `BrandColorHex` is non-empty.
- Discovery dialog: prepend the same brand swatch to each agent row's `DisplayName` for visual consistency between "what the scanner found" and "what the card shows after add".
- Details and Names views: **out of scope for v1** (keeps the diff small; card view is the primary surface).

### R4 — Design tokens: shared rendering constants

- `LauncherDesignTokens` gains:
  - `BrandBadgeSize = 22`
  - `BrandBadgeCornerRadius = 6`
  - `BrandBadgeForegroundColor = "White"`

### R5 — Tests

- `AgentBrandMatcherTests`: matches `codex`, `CODEX`, `codex.exe`, `C:\path\codex.cmd`, `codex --flag`; returns false for `""`, `"   "`, `"unknownbin"`, null.
- `AgentCatalogTests` extended: every entry has a `BrandColorHex` matching `^#[0-9A-Fa-f]{6}$`.

## Acceptance Criteria

- [ ] `AgentCatalogEntry.BrandColorHex` exists and is populated for all 8 entries.
- [ ] `AgentBrandMatcher.TryMatch` is a pure Core function, tested.
- [ ] `LauncherEntryViewModel.BrandColorHex` / `BrandGlyph` are computed, cached, and refreshed on edit.
- [ ] Cards view shows the badge before the Name; hidden when no catalog match.
- [ ] Discovery dialog shows the same badge per agent row.
- [ ] No hardcoded colors in XAML — badge uses `{Binding BrandColorHex}` for background and `LauncherDesignTokens.BrandBadgeForegroundColor` via `{StaticResource}` for text.
- [ ] `tools/verify.ps1` 19-check gate stays green (count may rise to 20 if a new check is added, but existing 19 must not break).
- [ ] No new NuGet package added.
- [ ] `dotnet test` passes (new tests included; existing 483 stay green).
- [ ] No bare `catch (Exception)` introduced.
- [ ] 5-layer architecture holds (ArchUnitNET R1-R6 green) — `AgentBrandMatcher` is pure Core, zero UI deps.

## Out of Scope

- **Real SVG/PNG agent logos** — copyright + asset pipeline overhead. Initials-on-color gives 80% of the value at 5% of the cost. Add real logos as a separate future task if the glyph version feels insufficient.
- **User-overridable brand colors** — the catalog is static; users can already add custom entries that simply won't get a brand badge.
- **Details / Names view badges** — card view is the primary surface; other views can inherit later if wanted.
- **Brand-color theming beyond the badge** (e.g., tinting the whole card border) — visually noisy, deferred.
- **Per-agent icons in the context menu / dialogs** — those surfaces are action-oriented, not identity-oriented.

## Notes

- Brand colors are **approximations**, not vendor-pulled assets. The hex list lives in one place (`AgentCatalog`) and ships with a comment noting they're not official. Users who care about exact brand fidelity can edit the catalog locally.
- The 2-letter glyph is derived from `ExecName` (first 2 chars, uppercased) — no new field needed. This means `claude`/`codex`/`cursor` resolve to `CL`/`CO`/`CU`, which are distinguishable when paired with their distinct brand colors.
- Ponytail: one matcher function in Core, two computed properties on the VM, one Border in XAML. No plugin machinery, no resource dictionary, no converter class.
