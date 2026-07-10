namespace WTLauncher.Core;

/// <summary>
/// Pure function over <see cref="AgentCatalog"/>: given a launch entry's
/// command string, determine whether it invokes a catalog-known agent and
/// return the matched catalog row. Used by the UI layer to surface per-agent
/// brand badges. Zero state, zero UI dependencies.
/// </summary>
public static class AgentBrandMatcher
{
    /// <summary>
    /// Extracts the first whitespace-delimited token of <paramref name="command"/>,
    /// strips directory path and Windows extension, and matches against catalog
    /// <see cref="AgentCatalogEntry.ExecName"/> values (ordinal ignore-case).
    /// </summary>
    /// <returns>True when a catalog entry matches; false for null/empty/unknown.</returns>
    public static bool TryMatch(string? command, out AgentCatalogEntry entry)
    {
        entry = default!;
        if (string.IsNullOrWhiteSpace(command)) return false;

        var tokens = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length == 0) return false;
        var token = tokens[0];
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
