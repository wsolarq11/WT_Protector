namespace WTLauncher.Core;

/// <summary>
/// One row in the static catalog of known AI agent CLIs. Immutable.
/// The catalog is the single source of truth for "which agents does the
/// launcher know how to discover." Adding an agent is a one-line edit
/// in <see cref="AgentCatalog"/>.
/// </summary>
public sealed record AgentCatalogEntry
{
    /// <summary>Binary name searched on PATH (without extension on Windows).</summary>
    public required string ExecName { get; init; }

    /// <summary>Human-readable label shown in the recommendation dialog.</summary>
    public required string DisplayName { get; init; }

    /// <summary>Vendor or community owner.</summary>
    public required string Vendor { get; init; }

    /// <summary>Arguments passed to detect the installed version.</summary>
    public required string VersionArgs { get; init; }

    /// <summary>Optional pre-filled args when generating a launch entry.
    /// Empty by default in v2.1 — user sets per-entry.</summary>
    public string RecommendedArgs { get; init; } = string.Empty;

    /// <summary>Coarse category. v2.1 ships TerminalNative only.</summary>
    public required AgentCategory Category { get; init; }

    /// <summary>Optional homepage URL for the "learn more" link in the dialog.</summary>
    public string HomePageUrl { get; init; } = string.Empty;

    /// <summary>Approximate brand color as #RRGGBB for the card badge. Not a
    /// vendor-official asset; a visually distinct signal per agent. Parsed by
    /// the UI layer for badge background.</summary>
    public required string BrandColorHex { get; init; }
}
