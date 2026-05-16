using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed class LauncherConfigStoreTests
{
    [Fact]
    public void Load_ReadsRootConfigShape()
    {
        using var temp = new TempDirectory();
        var config = System.IO.Path.Combine(temp.Path, "config.json");
        File.WriteAllText(config, """
        [
          {
            "name": "demo",
            "directory": "D:\\demo",
            "command": "codex",
            "confirm": true
          }
        ]
        """);

        var entries = LauncherConfigStore.Load(config);

        var entry = Assert.Single(entries);
        Assert.Equal("demo", entry.Name);
        Assert.Equal("D:\\demo", entry.Directory);
        Assert.Equal("codex", entry.Command);
        Assert.True(entry.Confirm);
    }

    [Fact]
    public void ResolveConfigPath_PrefersExplicitPath()
    {
        var explicitPath = System.IO.Path.Combine("somewhere", "config.json");

        var resolved = LauncherConfigStore.ResolveConfigPath(explicitPath, currentDirectory: "ignored", appBaseDirectory: "ignored");

        Assert.EndsWith(System.IO.Path.Combine("somewhere", "config.json"), resolved);
    }

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "wt-launcher-config-test-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path))
                Directory.Delete(Path, recursive: true);
        }
    }
}
