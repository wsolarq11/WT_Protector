using CsCheck;
using WTLauncher.Core;

using WTLauncher.Tests;
namespace WTLauncher.Tests.PropertyTest;

[Trait("Category", TestCategories.PropertyTest)]
public sealed class LauncherStoreValidatorPbtTests
{
    private static LauncherStoreFile ValidStore() => new()
    {
        SchemaVersion = LauncherStoreValidator.ExpectedSchemaVersion,
        Entries = [new LaunchEntry
        {
            Id = "one",
            Name = "One",
            Directory = (DirectoryPath)@"C:\",
            Command = "pwsh",
        }],
        Presets = [new LauncherPreset { Id = "default", Name = "Default", EntryIds = ["one"] }],
        DefaultPresetId = "default",
        Settings = new LauncherSettings(),
    };

    [Fact]
    public void ValidStorePasses()
    {
        Gen.Int[0, 5]
            .Sample(assert: _ => LauncherStoreValidator.ThrowIfInvalid(ValidStore()));
    }

    [Fact]
    public void NewerSchemaVersionRejected()
    {
        Gen.Int[LauncherStoreValidator.ExpectedSchemaVersion + 1, 99]
            .Sample(assert: version =>
            {
                var store = ValidStore() with { SchemaVersion = version };
                Assert.Throws<InvalidDataException>(() => LauncherStoreValidator.ThrowIfInvalid(store));
            });
    }

    [Fact]
    public void OlderSchemaVersionRejected()
    {
        Gen.Int[0, LauncherStoreValidator.ExpectedSchemaVersion - 1]
            .Sample(assert: version =>
            {
                var store = ValidStore() with { SchemaVersion = version };
                Assert.Throws<InvalidDataException>(() => LauncherStoreValidator.ThrowIfInvalid(store));
            });
    }
}
