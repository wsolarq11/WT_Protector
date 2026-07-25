using System.Text.Json;
using WTLauncher.Core;

namespace WTLauncher.Tests;

public class DirectoryPathTests
{
    [Fact]
    public void Parse_NullOrEmpty_Throws()
    {
        Assert.Throws<FormatException>(() => DirectoryPath.Parse(null));
        Assert.Throws<FormatException>(() => DirectoryPath.Parse(""));
        Assert.Throws<FormatException>(() => DirectoryPath.Parse("   "));
    }

    [Fact]
    public void Parse_ValidLocalPath_ReturnsOk()
    {
        var temp = System.IO.Path.GetTempPath();
        var dir = DirectoryPath.Parse(temp);
        Assert.Equal(temp.TrimEnd('\\'), dir.Value.TrimEnd('\\'));
    }

    [Fact]
    public void ParseAndValidateExists_NonExistentDir_Throws()
    {
        Assert.Throws<DirectoryNotFoundException>(() =>
            DirectoryPath.ParseAndValidateExists(@"C:\does-not-exist-12345"));
    }

    [Fact]
    public void Parse_UncPath_Throws()
    {
        Assert.Throws<FormatException>(() => DirectoryPath.Parse(@"\\server\share\folder"));
    }

    [Fact]
    public void Parse_FullwidthPercentInjection_Throws()
    {
        // U+FF05 (fullwidth percent sign) normalizes to U+0025 (%) via FormKC.
        var fullwidthPercent = '\uFF05';
        Assert.Throws<FormatException>(() => DirectoryPath.Parse($"C:\\tmp\\user{fullwidthPercent}data"));
    }

    [Fact]
    public void Parse_LiteralPercent_DoesNotThrow()
    {
        // Literal % in the original directory is allowed.
        var ex = Record.Exception(() => DirectoryPath.Parse(@"C:\tmp\user%data"));
        Assert.Null(ex);
    }

    [Fact]
    public void JsonConverter_RoundTrip_Works()
    {
        var temp = System.IO.Path.GetTempPath();
        var dir = DirectoryPath.Parse(temp);
        var options = new JsonSerializerOptions
        {
            Converters = { new DirectoryPathJsonConverter() }
        };
        var json = JsonSerializer.Serialize(dir, options);
        var deserialized = JsonSerializer.Deserialize<DirectoryPath>(json, options);
        Assert.Equal(dir.Value, deserialized.Value);
    }
}
