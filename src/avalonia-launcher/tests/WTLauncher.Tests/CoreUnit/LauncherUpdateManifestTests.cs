using WTLauncher.Core;

using WTLauncher.Tests;
namespace WTLauncher.Tests.CoreUnit;

[Trait("Category", TestCategories.CoreUnit)]
public sealed class LauncherUpdateManifestTests
{
    private const string Json = """
    {
      "schemaVersion": 1,
      "version": "1.2.3",
      "fileName": "WTLauncher.App.exe",
      "sha256": "ABCDEF",
      "bytes": 12345,
      "publishedAtUtc": "2026-08-14T00:00:00Z",
      "notes": "test"
    }
    """;

    [Fact]
    public void Parse_ReadsManifestFields()
    {
        var manifest = LauncherUpdateManifestParser.Parse(Json);

        Assert.Equal("1.2.3", manifest.Version);
        Assert.Equal("WTLauncher.App.exe", manifest.FileName);
        Assert.Equal("ABCDEF", manifest.Sha256);
        Assert.Equal(12345, manifest.Bytes);
    }

    [Fact]
    public void IsNewerThan_ComparesVersionsAndPrerelease()
    {
        var manifest = LauncherUpdateManifestParser.Parse(Json);

        Assert.True(manifest.IsNewerThan("1.2.2"));
        Assert.False(manifest.IsNewerThan("1.2.3"));
        Assert.False(manifest.IsNewerThan("1.3.0"));
        Assert.True(manifest.IsNewerThan("1.2.3-dev"));
    }

    [Fact]
    public void Parse_RejectsMissingRequiredFields()
    {
        Assert.Throws<InvalidDataException>(() => LauncherUpdateManifestParser.Parse("{}"));
        Assert.Throws<InvalidDataException>(() => LauncherUpdateManifestParser.Parse("not json"));
    }
}
