# Design — Per-agent Branding

## Layer assignments (follows the 5-layer invariant)

| Change | Layer | File | Why |
|---|---|---|---|
| `AgentCatalogEntry.BrandColorHex` field | 0 Core | `AgentCatalogEntry.cs` | Pure data record; no UI deps |
| `AgentCatalog` brand color population | 0 Core | `AgentCatalog.cs` | Static data, one source |
| `AgentBrandMatcher.TryMatch` | 0 Core | `AgentBrandMatcher.cs` (new) | Pure function over catalog; tests in isolation |
| `LauncherDesignTokens` badge constants | 0 Core | `LauncherDesignTokens.cs` | Single source for sizing/color constants |
| `LauncherEntryViewModel.BrandColorHex` / `BrandGlyph` | 1-3 App | `LauncherEntryViewModel.cs` | Computed projection of `Command`; cached; notified in `RefreshDisplay` |
| Card badge `Border` + `TextBlock` | 4 View | `MainWindow.axaml` | Pure XAML binding, no code-behind |
| Discovery dialog brand swatch | 4 View | `DiscoverAgentsWindow.cs` | Visual consistency with card badge |
| `AgentBrandMatcherTests`, `AgentCatalogTests` | Tests | `WTLauncher.Tests` | xUnit `[Theory]` for matcher; extend existing catalog tests |

## Contracts

### `AgentCatalogEntry.BrandColorHex`

```csharp
/// <summary>Approximate brand color as #RRGGBB. Not an official vendor asset;
/// visually distinct per agent. Parsed by the UI layer for badge background.</summary>
public required string BrandColorHex { get; init; }
```

Format contract: `^#[0-9A-Fa-f]{6}$`. Enforced by unit test, not by runtime regex (catalog is static and trusted).

### `AgentBrandMatcher.TryMatch`

```csharp
public static bool TryMatch(string? command, out AgentCatalogEntry entry)
```

- Null/empty/whitespace `command` → returns `false`, `entry` = default (null-coalesced via `out` contract; use `default!` to satisfy nullable annotations).
- Extracts first whitespace-delimited token: `command.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()`.
- Strips directory path: `Path.GetFileNameWithoutExtension(token)` — this also strips `.exe`/`.cmd`/`.bat`.
- Matches against catalog `ExecName` using `StringComparison.OrdinalIgnoreCase`.
- Returns first match; if none, returns `false`.

**Why a static method, not an instance**: the catalog is static data, the matcher is a pure function. No state, no DI ceremony. Ponytail.

### `LauncherEntryViewModel.BrandColorHex` / `BrandGlyph`

```csharp
public string BrandColorHex { get; private set; } = string.Empty;
public string BrandGlyph { get; private set; } = string.Empty;

private void RefreshBrand()
{
    if (AgentBrandMatcher.TryMatch(_entry.Command, out var catalogEntry))
    {
        BrandColorHex = catalogEntry.BrandColorHex;
        BrandGlyph = catalogEntry.ExecName.Length >= 2
            ? catalogEntry.ExecName.Substring(0, 2).ToUpperInvariant()
            : catalogEntry.ExecName.ToUpperInvariant();
    }
    else
    {
        BrandColorHex = string.Empty;
        BrandGlyph = string.Empty;
    }
}
```

Called from the constructor (initial compute) and from `RefreshDisplay()` (re-compute on edit).

`OnPropertyChanged(nameof(BrandColorHex))` and `OnPropertyChanged(nameof(BrandGlyph))` raised inside `RefreshDisplay` alongside the existing `Command` notification.

### Card badge XAML

Inserted into `entry-header` Grid, before the `CheckBox` (or after, depending on visual balance — design call during implementation):

```xml
<Border Classes="brand-badge"
        Width="{StaticResource BrandBadgeSize}"
        Height="{StaticResource BrandBadgeSize}"
        CornerRadius="{StaticResource BrandBadgeCornerRadius}"
        Background="{Binding BrandColorHex}"
        IsVisible="{Binding BrandColorHex, Converter={x:Static ObjectConverters.IsNotNullOrEmpty}}"
        VerticalAlignment="Center">
    <TextBlock Classes="brand-badge-glyph"
               Foreground="{StaticResource BrandBadgeForegroundBrush}"
               FontSize="11"
               FontWeight="Bold"
               HorizontalAlignment="Center"
               VerticalAlignment="Center"
               Text="{Binding BrandGlyph}"/>
</Border>
```

The `IsVisible` binding uses `ObjectConverters.IsNotNullOrEmpty` so the badge hides for unmatched entries (empty `BrandColorHex`).

`ColumnDefinitions` on `entry-header` adjusts from `"Auto,*"` to `"Auto,Auto,*"` to seat the badge.

### Design token wiring

`App.axaml` (or wherever brushes are declared) gains:

```xml
<x:Double x:Key="BrandBadgeSize">22</x:Double>
<x:Double x:Key="BrandBadgeCornerRadius">6</x:Double>
<SolidColorBrush x:Key="BrandBadgeForegroundBrush" Color="{StaticResource BrandBadgeForegroundColor}"/>
```

With `BrandBadgeForegroundColor` constant added to `LauncherDesignTokens` as `Color` resource.

Actually — simpler. The existing `LauncherDesignTokens` are `const string` hexes surfaced via `<Color x:Key="...">` in AXAML. The badge size is a `const double`. I'll follow the same pattern. Details during implementation; the contract here is "constants live in `LauncherDesignTokens`, surfaced as `StaticResource` in AXAML".

## Brand color choices (approximate, not official)

| ExecName | Hex | Rationale |
|---|---|---|
| claude | `#D97757` | Anthropic terracotta/clay tone |
| codex | `#10A37F` | OpenAI green family |
| cursor | `#5B5BD6` | Indigo (distinct from OpenAI green) |
| antigravity | `#1A73E8` | Google blue |
| gemini | `#8B5CF6` | Purple (Gemini gradient leans purple; distinct from Google blue) |
| opencode | `#EA580C` | Orange (community, distinct) |
| aider | `#CA8A04` | Amber/gold (community, distinct) |
| qwen-code | `#6D28D9` | Deep purple (Qwen brand leans purple; distinct from Gemini) |

All chosen for (a) pairwise visual distinctness and (b) sufficient contrast with white text (foreground). None are vendor-official; documented as approximate in the catalog comment.

## Risks & mitigations

| Risk | Mitigation |
|---|---|
| Brand hex typo (e.g., 5-digit) | Unit test regex on every catalog entry |
| Badge crowding the card header on narrow widths | Badge is 22×22 fixed; `entry-header` is `Auto,Auto,*` so Name column flexes; tested by GUI smoke |
| Catalog match false-positive (user has a binary named `claude` that isn't Claude Code) | Acceptable — the match is on PATH-discoverable exec name, which is exactly what the catalog keys on. False positives are cosmetic (wrong color), not functional |
| Edit command → badge stale | `RefreshDisplay()` already fires on edit; add the two new properties to its notification list |
| `Path.GetFileNameWithoutExtension` strips `.cmd` but a token like `./codex` on Linux paths | `Path.GetFileNameWithoutExtension` handles both `/` and `\` on Windows; for cross-platform safety the matcher also handles `/` via `Split` on path separators. For now Windows-only paths are the reality — keep it simple |

## Compatibility / rollback

- **Schema**: no change to `launcher.json` — brand data is catalog-side only, not per-entry. No migration.
- **Persistence**: zero impact. Brand is computed at runtime from `Command`.
- **Rollback**: revert the commit; catalog returns to 8 brandless entries. No state to clean up.

## Test plan

1. `AgentBrandMatcherTests` — `[Theory]` over the matrix above.
2. `AgentCatalogTests` — extend with `[Fact]` asserting every entry's `BrandColorHex` matches `^#[0-9A-Fa-f]{6}$` and that all 8 hexes are pairwise distinct.
3. Existing 483 tests stay green (no behavioral change to launch/persistence).
4. GUI smoke (hidden window startup) covers the card rendering at the gate level.
5. Manual visual spot-check: run the app, confirm Claude/Codex/etc. entries show distinct colored badges. Not gated, but documented.
