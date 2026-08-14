using System.Text.RegularExpressions;

using WTLauncher.Tests;
namespace WTLauncher.Tests.UiContract;

[Trait("Category", TestCategories.UiContract)]
public sealed partial class VisualTokenContractTests
{
    [GeneratedRegex(@"(?<![0-9A-Fa-f])#(?:[0-9A-Fa-f]{8}|[0-9A-Fa-f]{6})(?![0-9A-Fa-f])")]
    private static partial Regex RawHexPattern();

    [Fact]
    public void AppSource_HasNoRawHexColors()
    {
        var sourceFiles = Directory.EnumerateFiles(TestRepositoryPaths.AppSourceRoot, "*", SearchOption.AllDirectories)
            .Where(path => path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
                || path.EndsWith(".axaml", StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.Split(Path.DirectorySeparatorChar).Contains("bin", StringComparer.OrdinalIgnoreCase)
                && !path.Split(Path.DirectorySeparatorChar).Contains("obj", StringComparer.OrdinalIgnoreCase));

        var violations = sourceFiles
            .SelectMany(path => RawHexPattern().Matches(File.ReadAllText(path)).Select(match => $"{path}: {match.Value}"))
            .ToArray();

        Assert.Empty(violations);
    }

    [Fact]
    public void SemanticTokens_AreInjectedAndConsumedByProductionViews()
    {
        var tokens = File.ReadAllText(TestRepositoryPaths.CoreSource("LauncherDesignTokens.cs"));
        var app = File.ReadAllText(TestRepositoryPaths.AppCodeBehind);
        var discoverAgents = File.ReadAllText(TestRepositoryPaths.View("DiscoverAgentsWindow.axaml.cs"));
        var drag = File.ReadAllText(TestRepositoryPaths.MainWindowDragCodeBehind);
        var xaml = Directory.EnumerateFiles(TestRepositoryPaths.AppSourceRoot, "*.axaml", SearchOption.AllDirectories)
            .Select(File.ReadAllText)
            .ToArray();

        foreach (var token in new[] { "DragGhostShadowColor", "FontSizeSmall", "FontSizeBody", "FontSizeEmphasis" })
            Assert.Contains(token, tokens, StringComparison.Ordinal);

        Assert.Contains("LauncherDesignTokens.DragGhostShadowColor", drag, StringComparison.Ordinal);
        Assert.Contains("LauncherDesignTokens.BadgeInfoForegroundColor", discoverAgents, StringComparison.Ordinal);
        Assert.Contains("Resources[\"NoBoxShadow\"]", app, StringComparison.Ordinal);

        foreach (var resource in new[] { "FontSizeSmall", "FontSizeBody", "FontSizeEmphasis" })
        {
            Assert.Contains($"Resources[\"{resource}\"] = LauncherDesignTokens.{resource};", app, StringComparison.Ordinal);
            Assert.Contains(xaml, content => content.Contains($"{{StaticResource {resource}}}", StringComparison.Ordinal));
        }

        Assert.Contains(xaml, content => content.Contains("{StaticResource NoBoxShadow}", StringComparison.Ordinal));
        Assert.DoesNotContain(xaml, content => Regex.IsMatch(content, "FontSize[^\\r\\n]*(?:\"12\"|\"14\"|\"16\")"));
    }
}
