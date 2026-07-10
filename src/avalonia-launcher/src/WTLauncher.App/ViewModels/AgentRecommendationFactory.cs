using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

/// <summary>
/// Maps a <see cref="DiscoveredAgent"/> into an <see cref="EntryDraft"/>
/// ready for the existing add-entry pipeline. Lives in ViewModels (not Core)
/// because <see cref="EntryDraft"/> is a ViewModel-layer type and Core cannot
/// depend on it. The mapping itself is pure — no static state, no I/O.
/// </summary>
public static class AgentRecommendationFactory
{
    /// <summary>
    /// Builds a draft from a discovered agent. The caller passes the default
    /// directory (typically the user profile); the user can edit it in the
    /// standard entry editor after creation.
    /// </summary>
    public static EntryDraft ToDraft(DiscoveredAgent agent, string defaultDirectory)
    {
        ArgumentNullException.ThrowIfNull(agent);

        return new EntryDraft(
            Id: GenerateEntryId(agent),
            Name: agent.Catalog.DisplayName,
            Directory: string.IsNullOrWhiteSpace(defaultDirectory)
                ? agent.Catalog.ExecName  // Will fail validation; caller surfaces error
                : defaultDirectory,
            Command: BuildCommand(agent),
            Selected: false);
    }

    private static string GenerateEntryId(DiscoveredAgent agent)
    {
        // Stable id derived from exec name — survives restarts, makes the
        // dedup check ("already added") robust across launcher runs.
        return agent.Catalog.ExecName.Replace("-", "_");
    }

    private static string BuildCommand(DiscoveredAgent agent)
    {
        // v2.1 ships empty RecommendedArgs for all catalog entries — user sets
        // per-entry after import. When a future agent needs pre-filled args
        // (e.g. claude --dangerously-bypass-approvals-and-sandbox), this is
        // the single switch point.
        return string.IsNullOrEmpty(agent.Catalog.RecommendedArgs)
            ? agent.Catalog.ExecName
            : $"{agent.Catalog.ExecName} {agent.Catalog.RecommendedArgs}";
    }
}
