using System.Text.Json;
using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed class RepositoryConfigContractTests
{
    [Fact]
    public void RepositoryRootConfig_LoadsAndBuildsDryRunPlans()
    {
        var root = FindRepositoryRoot();
        var configPath = Path.Combine(root, "config", "config.json");

        AssertEveryEntryDeclaresPersistentSelection(configPath);
        var entries = LauncherConfigStore.Load(configPath);

        Assert.NotEmpty(entries);
        foreach (var entry in entries)
        {
            var plan = LaunchArgumentBuilder.Build(entry, validateDirectory: false);
            Assert.Equal("wt.exe", plan.Executable);
            Assert.StartsWith("-d ", plan.Arguments, StringComparison.Ordinal);
            Assert.Contains(" pwsh -NoExit -Command ", plan.Arguments, StringComparison.Ordinal);
            if (LaunchArgumentBuilder.IsDangerousCommand(entry.Command))
                Assert.True(plan.RequiresConfirmation);
        }
    }

    private static void AssertEveryEntryDeclaresPersistentSelection(string configPath)
    {
        var json = File.ReadAllText(configPath);
        using var document = JsonDocument.Parse(json, new JsonDocumentOptions
        {
            AllowTrailingCommas = true,
            CommentHandling = JsonCommentHandling.Skip,
        });

        var entries = document.RootElement.EnumerateArray().ToList();
        Assert.All(entries, entry =>
        {
            Assert.True(entry.TryGetProperty("selected", out var selected), "Every entry must declare the portable selected checkbox state.");
            Assert.True(
                selected.ValueKind is JsonValueKind.True or JsonValueKind.False,
                "The selected field must be a JSON boolean.");
        });
    }

    private static string FindRepositoryRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "config", "config.json"))
                && File.Exists(Path.Combine(dir.FullName, "config", "settings.json"))
                && File.Exists(Path.Combine(dir.FullName, "README.md"))
                && Directory.Exists(Path.Combine(dir.FullName, "src", "avalonia-launcher")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate WT_Protector repository root from test output directory.");
    }
}
