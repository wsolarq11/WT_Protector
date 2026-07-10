namespace WTLauncher.Core;

/// <summary>
/// Result of probing one catalog entry against the local machine.
/// Immutable. <see cref="Version"/> is empty when detection failed
/// (timeout, non-zero exit, no parseable version substring) — failure
/// is not fatal; the agent is still offered for import with no version.
/// </summary>
public sealed record DiscoveredAgent
{
    public required AgentCatalogEntry Catalog { get; init; }

    /// <summary>Absolute path to the resolved binary on this machine.</summary>
    public required string ResolvedPath { get; init; }

    /// <summary>Parsed version substring, or empty on detection failure.</summary>
    public string Version { get; init; } = string.Empty;
}
