using System.Collections.Immutable;

namespace WTLauncher.Core;

/// <summary>
/// Aggregated scan result. Scanner continues on per-agent failure
/// rather than throwing; failures are reported here so the caller
/// (dialog) can decide whether to surface them to the user.
/// </summary>
public sealed record AgentDiscoveryResult
{
    public required ImmutableArray<DiscoveredAgent> Found { get; init; }

    public required ImmutableArray<AgentDiscoveryFailure> Failures { get; init; }

    public static AgentDiscoveryResult Empty { get; } = new()
    {
        Found = ImmutableArray<DiscoveredAgent>.Empty,
        Failures = ImmutableArray<AgentDiscoveryFailure>.Empty,
    };
}

/// <summary>One agent that the scanner could not probe. Reason is human-readable.</summary>
public sealed record AgentDiscoveryFailure(string ExecName, string Reason);
