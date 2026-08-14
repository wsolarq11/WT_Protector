using CsCheck;
using WTLauncher.Core;

using WTLauncher.Tests;
namespace WTLauncher.Tests.PropertyTest;

[Trait("Category", TestCategories.PropertyTest)]
public sealed class LauncherStoreValidatorPbtTests
{
    /// <summary>
    /// Property: A store with valid schema version passes validation.
    /// </summary>
    [Fact]
    public void ValidStorePasses()
    {
        Gen.Int[0, 5]
            .Sample(assert: _ =>
            {
                var store = new LauncherStoreFile
                {
                    SchemaVersion = LauncherStoreValidator.ExpectedSchemaVersion,
                };

                // Should not throw for valid schema version
                LauncherStoreValidator.ThrowIfInvalid(store);
            });
    }

    /// <summary>
    /// Property: A store with schema version > ExpectedSchemaVersion is rejected.
    /// </summary>
    [Fact]
    public void NewerSchemaVersionRejected()
    {
        Gen.Int[LauncherStoreValidator.ExpectedSchemaVersion + 1, 99]
            .Sample(assert: version =>
            {
                var store = new LauncherStoreFile { SchemaVersion = version };

                Assert.Throws<InvalidDataException>(() =>
                    LauncherStoreValidator.ThrowIfInvalid(store));
            });
    }

    /// <summary>
    /// Property: A store with schema version < MinimumSupportedSchemaVersion is rejected.
    /// </summary>
    [Fact]
    public void OlderSchemaVersionRejected()
    {
        Gen.Int[0, LauncherStoreValidator.MinimumSupportedSchemaVersion - 1]
            .Sample(assert: version =>
            {
                var store = new LauncherStoreFile { SchemaVersion = version };

                Assert.Throws<InvalidDataException>(() =>
                    LauncherStoreValidator.ThrowIfInvalid(store));
            });
    }
}
