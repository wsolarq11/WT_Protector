using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed partial class LauncherConfigStoreTests
{
    private static LauncherPresetFile PresetFile(string name, params string[] entryIds) =>
        new()
        {
            DefaultPresetId = "daily",
            Presets =
            [
                new LauncherPreset
                {
                    Id = "daily",
                    Name = name,
                    EntryIds = [.. entryIds],
                },
            ],
        };

    private static LaunchEntry Entry(bool confirm = false, bool selected = false) =>
        new()
        {
            Id = "demo",
            Name = "演示",
            Directory = "D:\\demo",
            Command = "codex",
            Confirm = confirm,
            Selected = selected,
        };

    private static void AssertUsesRepositoryLineEndings(string text)
    {
        Assert.EndsWith("\n", text, StringComparison.Ordinal);
        Assert.DoesNotContain("\r", text);
    }
}
