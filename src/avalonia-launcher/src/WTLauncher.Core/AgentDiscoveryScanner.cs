using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace WTLauncher.Core;

/// <summary>
/// Probes the local PATH for catalog agents and detects their versions.
/// Pure function: no static mutable state, no Avalonia/UI dependency.
/// Continues on per-agent failure rather than throwing; failures are
/// reported in <see cref="AgentDiscoveryResult.Failures"/> so the caller
/// can decide whether to surface them.
/// </summary>
public static class AgentDiscoveryScanner
{
    // Windows executable extensions searched in order. Empty string covers
    // the case where the binary already appears with an extension on PATH.
    private static readonly string[] WindowsExtensions = ["", ".exe", ".cmd", ".bat"];

    private static readonly TimeSpan VersionTimeout = TimeSpan.FromSeconds(2);

    // Generic semver-like pattern. Intentionally loose: "1.2.3", "v1.2.3",
    // "claude 1.0.14 (build 4f3a2b1)" all yield "1.0.14". Specific agents
    // that need bespoke parsing can be added later — ponytail: one regex
    // for all until proven insufficient.
    private static readonly Regex VersionPattern = new(@"\d+\.\d+(?:\.\d+)?", RegexOptions.Compiled);

    /// <summary>
    /// Scans the supplied PATH (or the process PATH env var when omitted)
    /// for every catalog entry, attempting to detect the installed version.
    /// Per-agent failures (missing binary, version-detect timeout, non-zero
    /// exit) are accumulated into <see cref="AgentDiscoveryResult.Failures"/>
    /// and do not abort the scan.
    /// </summary>
    public static AgentDiscoveryResult Scan(
        IEnumerable<AgentCatalogEntry> catalog,
        string? pathOverride = null)
    {
        ArgumentNullException.ThrowIfNull(catalog);

        var path = pathOverride ?? Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        var directories = path.Split(';', StringSplitOptions.RemoveEmptyEntries);

        var found = ImmutableArray.CreateBuilder<DiscoveredAgent>();
        var failures = ImmutableArray.CreateBuilder<AgentDiscoveryFailure>();

        foreach (var entry in catalog)
        {
            try
            {
                var resolved = TryResolveOnPath(entry.ExecName, directories);
                if (resolved is null)
                {
                    // Agent simply isn't installed — not a failure, just absent.
                    continue;
                }

                var version = TryDetectVersion(resolved, entry.VersionArgs);
                found.Add(new DiscoveredAgent
                {
                    Catalog = entry,
                    ResolvedPath = resolved,
                    Version = version ?? string.Empty,
                });
            }
            catch (IOException ex)
            {
                failures.Add(new AgentDiscoveryFailure(entry.ExecName, ex.Message));
            }
            catch (UnauthorizedAccessException ex)
            {
                failures.Add(new AgentDiscoveryFailure(entry.ExecName, ex.Message));
            }
            catch (Win32Exception ex)
            {
                failures.Add(new AgentDiscoveryFailure(entry.ExecName, $"进程启动失败: {ex.Message}"));
            }
            catch (InvalidOperationException ex)
            {
                failures.Add(new AgentDiscoveryFailure(entry.ExecName, ex.Message));
            }
        }

        return new AgentDiscoveryResult
        {
            Found = found.ToImmutable(),
            Failures = failures.ToImmutable(),
        };
    }

    /// <summary>
    /// Walks the supplied directories looking for the first matching
    /// executable. Returns the absolute resolved path or null when not found.
    /// </summary>
    internal static string? TryResolveOnPath(string execName, IEnumerable<string> directories)
    {
        foreach (var dir in directories)
        {
            if (string.IsNullOrWhiteSpace(dir))
                continue;

            foreach (var ext in WindowsExtensions)
            {
                string candidate;
                try
                {
                    candidate = Path.GetFullPath(Path.Combine(dir, execName + ext));
                }
                catch (ArgumentException)
                {
                    // Malformed PATH entry (invalid chars, empty segment) — skip
                    // silently rather than abort the whole scan.
                    continue;
                }
                catch (PathTooLongException)
                {
                    continue;
                }

                if (File.Exists(candidate))
                    return candidate;
            }
        }

        return null;
    }

    /// <summary>
    /// Spawns the binary with its <paramref name="versionArgs"/> and parses
    /// the first semver-like substring from the first line of stdout.
    /// Returns null on timeout, non-zero exit, or no parseable version.
    /// </summary>
    internal static string? TryDetectVersion(string resolvedPath, string versionArgs)
    {
        var psi = new ProcessStartInfo
        {
            FileName = resolvedPath,
            Arguments = versionArgs,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            // Most modern CLI agents emit UTF-8 even on Windows; setting this
            // explicitly avoids mojibake on version strings containing
            // non-ASCII characters. If a future agent emits the system codepage
            // we'll revisit per-agent encoding overrides — ponytail: one setting.
            StandardOutputEncoding = Encoding.UTF8,
        };

        using var process = new Process { StartInfo = psi };

        if (!process.Start())
            return null;

        // Read the first line synchronously — process lifetime is short
        // (typically <100ms for `--version`), and async stream reads would
        // complicate the timeout path without buying anything here.
        string? firstLine;
        try
        {
            firstLine = process.StandardOutput.ReadLine();
        }
        catch (IOException)
        {
            // Output stream closed prematurely — give up on version detection.
            try { process.Kill(entireProcessTree: true); } catch { /* narrow ignore */ }
            return null;
        }

        if (!process.WaitForExit((int)VersionTimeout.TotalMilliseconds))
        {
            try { process.Kill(entireProcessTree: true); } catch { /* narrow ignore */ }
            return null;
        }

        if (firstLine is null)
            return null;

        var match = VersionPattern.Match(firstLine);
        return match.Success ? match.Value : null;
    }
}
