using WTLauncher.Core;

using WTLauncher.Tests;
namespace WTLauncher.Tests.ViewModelBehavior;

[Trait("Category", TestCategories.ViewModelBehavior)]
public sealed class AgentDiscoveryScannerTests
{
    [Fact]
    public void TryResolveOnPath_ReturnsNull_WhenNoDirectories()
    {
        var resolved = AgentDiscoveryScanner.TryResolveOnPath("claude", Array.Empty<string>());
        Assert.Null(resolved);
    }

    [Fact]
    public void TryResolveOnPath_ReturnsNull_WhenBinaryAbsent()
    {
        using var temp = new TempDirectory();
        var resolved = AgentDiscoveryScanner.TryResolveOnPath("nonexistent-cli-xyz", new[] { temp.Path });
        Assert.Null(resolved);
    }

    [Fact]
    public void TryResolveOnPath_FindsExe_WhenPresentWithoutExtension()
    {
        using var temp = new TempDirectory();
        var execPath = Path.Combine(temp.Path, "fake-agent.exe");
        File.WriteAllText(execPath, "fake");
        // Skip directory enumeration that triggers AV — just probe directly.
        var resolved = AgentDiscoveryScanner.TryResolveOnPath("fake-agent", new[] { temp.Path });
        Assert.NotNull(resolved);
        Assert.Equal(execPath, resolved);
    }

    [Fact]
    public void TryResolveOnPath_FindsCmd_WhenOnlyCmdExtensionPresent()
    {
        using var temp = new TempDirectory();
        var cmdPath = Path.Combine(temp.Path, "fake-agent.cmd");
        File.WriteAllText(cmdPath, "@echo off");
        var resolved = AgentDiscoveryScanner.TryResolveOnPath("fake-agent", new[] { temp.Path });
        Assert.NotNull(resolved);
        Assert.Equal(cmdPath, resolved);
    }

    [Fact]
    public void TryResolveOnPath_SkipsMalformedPathEntries_WithoutThrowing()
    {
        // ponytail: malformed PATH segment (illegal chars) must not abort the whole scan.
        var malformed = new[] { "Z::|||invalid|||" };
        var resolved = AgentDiscoveryScanner.TryResolveOnPath("claude", malformed);
        Assert.Null(resolved);
    }

    [Fact]
    public async Task Scan_WithEmptyPathOverride_ReturnsEmptyResult()
    {
        var result = AgentDiscoveryScanner.Scan(AgentCatalog.Entries, pathOverride: "", ct: TestContext.Current.CancellationToken);
        Assert.Empty(result.Found);
        Assert.Empty(result.Failures);
        await Task.CompletedTask;
    }

    [Fact]
    public void Scan_WithFakeExeOnPath_FindsAgentAndDetectsVersion()
    {
        // ponytail: uses a real .cmd shim that echoes a fake semver. Validates
        // the end-to-end scan → resolve → version-detect pipeline.
        using var temp = new TempDirectory();
        var cmdPath = Path.Combine(temp.Path, "fake-claude.cmd");
        File.WriteAllText(cmdPath, "@echo claude 1.0.14 (build 4f3a2b1)\r\n@exit 0\r\n");

        var catalogEntry = new AgentCatalogEntry
        {
            ExecName = "fake-claude",
            DisplayName = "Fake Claude",
            Vendor = "Test",
            VersionArgs = "--version",
            Category = AgentCategory.TerminalNative,
            BrandColorHex = "#D97757",
        };

        var result = AgentDiscoveryScanner.Scan(new[] { catalogEntry }, pathOverride: temp.Path, ct: TestContext.Current.CancellationToken);
        var found = Assert.Single(result.Found);
        Assert.Equal("1.0.14", found.Version);
        Assert.EndsWith("fake-claude.cmd", found.ResolvedPath);
        Assert.Empty(result.Failures);
    }

    [Fact]
    public void Scan_AgentNotInstalled_SkipsSilentlyWithoutFailure()
    {
        // ponytail: absent agent is not a failure — just absent. Failures are
        // reserved for I/O / process errors during probing.
        var result = AgentDiscoveryScanner.Scan(
            new[] { AgentCatalog.Entries.First(e => e.ExecName == "claude") },
            pathOverride: "",
            ct: TestContext.Current.CancellationToken);
        Assert.Empty(result.Found);
        Assert.Empty(result.Failures);
    }

    [Fact]
    public void Scan_ParallelSpawn_FindsAllConcurrentEntries()
    {
        // ponytail: 并行化的真正价值是验证 ConcurrentBag 累积无丢失/无串扰。
        // timing 断言已删除 — 并行性是 Parallel.ForEachAsync 的库保证,
        // timing 对 AV 扫描 + CI 负载敏感, 本质 flaky。
        // fast + slow (ping ≈1s) 同时 spawn: 若串行总耗 ≈ 2*s; 并行 ≈ max(s)。
        // 仍保留 slow fixture 以暴露任何"等待 slow 时 fast 被丢"的累积 bug。
        using var temp = new TempDirectory();

        var fastPath = Path.Combine(temp.Path, "fast.cmd");
        File.WriteAllText(fastPath, "@echo fast 1.0.0\r\n@exit 0\r\n");
        var slowPath = Path.Combine(temp.Path, "slow.cmd");
        File.WriteAllText(slowPath, "@ping -n 2 127.0.0.1 > nul\r\n@echo slow 2.0.0\r\n@exit 0\r\n");

        var catalog = new[]
        {
            new AgentCatalogEntry
            {
                ExecName = "fast",
                DisplayName = "Fast",
                Vendor = "Test",
                VersionArgs = "--version",
                Category = AgentCategory.TerminalNative,
                BrandColorHex = "#000000",
            },
            new AgentCatalogEntry
            {
                ExecName = "slow",
                DisplayName = "Slow",
                Vendor = "Test",
                VersionArgs = "--version",
                Category = AgentCategory.TerminalNative,
                BrandColorHex = "#000000",
            },
        };

        var result = AgentDiscoveryScanner.Scan(catalog, pathOverride: temp.Path, ct: TestContext.Current.CancellationToken);

        Assert.Equal(2, result.Found.Length);
        Assert.Empty(result.Failures);
        Assert.Contains(result.Found, f => f.Version == "1.0.0");
        Assert.Contains(result.Found, f => f.Version == "2.0.0");
    }

    [Fact]
    public void TryDetectVersion_ReturnsNull_WithinTimeout_WhenCliHangsWithoutStdout()
    {
        // ponytail: pre-2026-07-10 bug — ReadLine() had no timeout, hung forever
        // when a cli started but never wrote stdout (slow .cmd shim, stdin-waiter,
        // stderr-only emitter). Now WaitForExit(timeout) kills it.
        using var temp = new TempDirectory();
        var hangPath = Path.Combine(temp.Path, "hang.cmd");
        // @ping -n 30 127.0.0.1 > nul blocks ~30s with no stdout output.
        File.WriteAllText(hangPath, "@ping -n 30 127.0.0.1 > nul\r\n@exit 0\r\n");

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var version = AgentDiscoveryScanner.TryDetectVersion(hangPath, "--version");
        sw.Stop();

        Assert.Null(version);
        // VersionTimeout = 2s. Allow +1.5s for cmd.exe spawn + kill cleanup.
        Assert.True(sw.ElapsedMilliseconds < 3500,
            $"Hang detection took {sw.ElapsedMilliseconds}ms, expected < 3500ms");
    }
}
