using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

public abstract record DiscoveryDialogResult;

public sealed record DiscoveredAgentsSelection(IReadOnlyList<DiscoveredAgent> Agents)
    : DiscoveryDialogResult;

public sealed record ExistingDirectorySelection(EntryDraft Draft)
    : DiscoveryDialogResult;

public enum DiscoveryMode
{
    PathScan,
    ExistingDirectory,
}
