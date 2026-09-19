using WTLauncher.Core;

using WTLauncher.Tests;
namespace WTLauncher.Tests.CoreUnit;

[Trait("Category", TestCategories.CoreUnit)]
public sealed class LauncherStoreValidatorTests
{
    private static LauncherStoreFile ValidStore() => new()
    {
        Entries = [Entry("one", "One")],
        Settings = new LauncherSettings(),
    };

    [Fact]
    public void ThrowIfInvalid_RejectsDuplicateEntryId()
    {
        var store = ValidStore() with
        {
            Entries = [Entry("one", "One"), Entry("one", "One copy")],
        };

        var ex = Assert.Throws<InvalidDataException>(() => LauncherStoreValidator.ThrowIfInvalid(store));
        Assert.Contains("duplicate entry id", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ThrowIfInvalid_RejectsNonPositiveWindow()
    {
        var store = ValidStore() with
        {
            Settings = new LauncherSettings { Window = new LauncherWindowSettings { Width = 0, Height = 600 } },
        };

        var ex = Assert.Throws<InvalidDataException>(() => LauncherStoreValidator.ThrowIfInvalid(store));
        Assert.Contains("must be positive", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static LaunchEntry Entry(string id, string name) => new()
    {
        Id = id,
        Name = name,
        Directory = (DirectoryPath)@"C:\",
        Command = "pwsh",
    };
}
