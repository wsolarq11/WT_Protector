using WTLauncher.Core;

using WTLauncher.Tests;
namespace WTLauncher.Tests.CoreUnit;

[Trait("Category", TestCategories.CoreUnit)]
public sealed class AgentCatalogTests
{
    [Fact]
    public void Entries_AreNonEmpty()
    {
        Assert.NotEmpty(AgentCatalog.Entries);
    }

    [Fact]
    public void Entries_HaveUniqueExecNames()
    {
        var execNames = AgentCatalog.Entries.Select(e => e.ExecName).ToArray();
        var distinct = execNames.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        Assert.Equal(execNames.Length, distinct.Length);
    }

    [Fact]
    public void Entries_AllHaveRequiredFields()
    {
        foreach (var entry in AgentCatalog.Entries)
        {
            Assert.False(string.IsNullOrWhiteSpace(entry.ExecName), $"{entry} missing ExecName");
            Assert.False(string.IsNullOrWhiteSpace(entry.DisplayName), $"{entry.ExecName} missing DisplayName");
            Assert.False(string.IsNullOrWhiteSpace(entry.Vendor), $"{entry.ExecName} missing Vendor");
            Assert.False(string.IsNullOrWhiteSpace(entry.VersionArgs), $"{entry.ExecName} missing VersionArgs");
            Assert.False(string.IsNullOrWhiteSpace(entry.HomePageUrl), $"{entry.ExecName} missing HomePageUrl");
            Assert.True(Enum.IsDefined(entry.Category), $"{entry.ExecName} has invalid Category");
        }
    }

    [Fact]
    public void Entries_IncludeCoreAgents()
    {
        var execNames = AgentCatalog.Entries.Select(e => e.ExecName).ToHashSet(StringComparer.OrdinalIgnoreCase);
        // ponytail: minimum coverage — these are the agents documented in PRD.
        Assert.Contains("claude", execNames);
        Assert.Contains("codex", execNames);
        Assert.Contains("aider", execNames);
    }

    [Fact]
    public void Entries_HaveUniqueHomePageUrls()
    {
        var urls = AgentCatalog.Entries
            .Select(e => e.HomePageUrl)
            .Where(u => !string.IsNullOrWhiteSpace(u))
            .ToArray();
        var distinct = urls.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        Assert.Equal(urls.Length, distinct.Length);
    }

    [Fact]
    public void Entries_AllHaveValidBrandColorHex()
    {
        // BrandColorHex must be #RRGGBB; the UI layer parses it via Color.Parse.
        Assert.NotEmpty(AgentCatalog.Entries);
        foreach (var entry in AgentCatalog.Entries)
        {
            Assert.Matches("^#[0-9A-Fa-f]{6}$", entry.BrandColorHex);
        }
    }

    [Fact]
    public void Entries_HaveDistinctBrandColorHex()
    {
        // ponytail: pairwise distinct so the badge is a real differentiator.
        var hexes = AgentCatalog.Entries.Select(e => e.BrandColorHex).ToArray();
        var distinct = hexes.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        Assert.Equal(hexes.Length, distinct.Length);
    }
}
