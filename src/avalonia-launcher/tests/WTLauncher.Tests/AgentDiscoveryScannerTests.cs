using WTLauncher.Core;

namespace WTLauncher.Tests;

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
        var result = AgentDiscoveryScanner.Scan(AgentCatalog.Entries, pathOverride: "");
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

        var result = AgentDiscoveryScanner.Scan(new[] { catalogEntry }, pathOverride: temp.Path);
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
            pathOverride: "");
        Assert.Empty(result.Found);
        Assert.Empty(result.Failures);
    }
}
