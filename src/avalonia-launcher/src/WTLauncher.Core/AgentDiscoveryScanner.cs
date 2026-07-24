using System.Collections.Concurrent;
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
    // ponytail: PATHEXT 顺序。npm 全局装的 CLI 同时有无扩展名 Unix shell 脚本
    // (claude) 和 Windows .cmd shim (claude.cmd)。原 ["", ".exe", ".cmd", ".bat"]
    // 把 "" 放首位会先匹中 Unix 脚本 → Windows 上 Process.Start 无法执行 → 扫不到。
    // 只接受 Windows 扩展名,遵循 Windows 默认 PATHEXT=.COM;.EXE;.BAT;.CMD 顺序。
    private static readonly string[] WindowsExtensions = [".exe", ".cmd", ".bat"];

    private static readonly TimeSpan VersionTimeout = TimeSpan.FromSeconds(2);

    // Generic semver-like pattern. Intentionally loose: "1.2.3", "v1.2.3",
    // "claude 1.0.14 (build 4f3a2b1)" all yield "1.0.14". Specific agents
    // that need bespoke parsing can be added later — ponytail: one regex
    // for all until proven insufficient.
    private static readonly Regex VersionPattern = new(@"\d+\.\d+(?:\.\d+)?", RegexOptions.Compiled);

    /// <summary>
    /// ponytail: 合并 Machine + User PATH, 不依赖 Process PATH 快照。
    /// Windows 上 explorer.exe 启动子进程(app 双击启动)时, PATH 是登录时
    /// 缓存的快照 — 装新 CLI(npm install -g claude) 后注册表 User PATH 更新,
    /// 但已运行的 explorer.exe 及其子进程仍持旧快照, 扫不到新装的 CLI。
    /// 读 Machine + User 注册表实时值绕过这个经典 Windows 问题。
    /// (cc-switch commit f8c1f17 "在 Windows 上禁用 env check" 的根因。)
    /// </summary>
    private static string BuildEffectivePath()
    {
        var machine = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Machine) ?? "";
        var user = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User) ?? "";
        return string.IsNullOrEmpty(user) ? machine : $"{machine};{user}";
    }

    /// <summary>
    /// Scans the supplied PATH (or the process PATH env var when omitted)
    /// for every catalog entry, attempting to detect the installed version.
    /// Per-agent failures (missing binary, version-detect timeout, non-zero
    /// exit) are accumulated into <see cref="AgentDiscoveryResult.Failures"/>
    /// and do not abort the scan.
    /// </summary>
    public static AgentDiscoveryResult Scan(
        IEnumerable<AgentCatalogEntry> catalog,
        string? pathOverride = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(catalog);

        var path = pathOverride ?? BuildEffectivePath();
        var directories = path
            .Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        // ponytail: Parallel.ForEachAsync cuts total scan time from Σ(per-cli)
        // to max(per-cli). MaxDegreeOfParallelism=4 caps concurrent process
        // spawns; catalog has 8 entries today, 4-way parallelism is OS-trivial.
        // ConcurrentBag is lock-free thread-safe for the parallel writes.
        var found = new ConcurrentBag<DiscoveredAgent>();
        var failures = new ConcurrentBag<AgentDiscoveryFailure>();

        var entries = catalog as IReadOnlyList<AgentCatalogEntry> ?? catalog.ToList();

        // CancellationToken is passed through to Parallel.ForEachAsync so that
        // if the caller cancels mid-scan, in-flight agent probes are allowed to
        // complete but no new ones start. Full propagation from ViewModel (e.g.
        // linking to page navigation CTS) is a future enhancement.
        Parallel.ForEachAsync(
            entries,
            new ParallelOptions { MaxDegreeOfParallelism = 4, CancellationToken = ct },
            (entry, _) =>
            {
                try
                {
                    var resolved = TryResolveOnPath(entry.ExecName, directories);
                    if (resolved is null)
                    {
                        // Agent simply isn't installed — not a failure, just absent.
                        return ValueTask.CompletedTask;
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

                return ValueTask.CompletedTask;
            }).GetAwaiter().GetResult();

        return new AgentDiscoveryResult
        {
            Found = found.ToImmutableArray(),
            Failures = failures.ToImmutableArray(),
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

        // Wait for exit first with the version timeout. This guarantees the
        // ReadLine below never blocks indefinitely on a cli that starts but
        // doesn't write stdout (slow .cmd shim, stdin-waiter, stderr-only
        // emitter). Process exit flushes buffered stdout so ReadLine returns
        // immediately. ponytail: pre-2026-07-10 code called ReadLine before
        // WaitForExit, which had no timeout on the read and could hang the
        // whole scan if any cli failed to emit a first line.
        if (!process.WaitForExit((int)VersionTimeout.TotalMilliseconds))
        {
            try { process.Kill(entireProcessTree: true); } catch (InvalidOperationException) { /* process already exited */ }
            return null;
        }

        string? firstLine;
        try
        {
            firstLine = process.StandardOutput.ReadLine();
        }
        catch (IOException)
        {
            // Output stream closed prematurely — give up on version detection.
            return null;
        }

        if (firstLine is null)
            return null;

        var match = VersionPattern.Match(firstLine);
        return match.Success ? match.Value : null;
    }
}
