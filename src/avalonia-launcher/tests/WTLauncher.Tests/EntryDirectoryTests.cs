using System.Text.Json;
using WTLauncher.Core;

namespace WTLauncher.Tests;

public class EntryDirectoryTests
{
    [Fact]
    public void Parse_NullOrEmpty_Throws()
    {
        Assert.Throws<FormatException>(() => EntryDirectory.Parse(null));
        Assert.Throws<FormatException>(() => EntryDirectory.Parse(""));
        Assert.Throws<FormatException>(() => EntryDirectory.Parse("   "));
    }

    [Fact]
    public void Parse_ValidLocalPath_ReturnsOk()
    {
        var temp = System.IO.Path.GetTempPath();
        var dir = EntryDirectory.Parse(temp);
        Assert.Equal(temp.TrimEnd('\\'), dir.Value.TrimEnd('\\'));
    }

    [Fact]
    public void ParseAndValidateExists_NonExistentDir_Throws()
    {
        Assert.Throws<DirectoryNotFoundException>(() =>
            EntryDirectory.ParseAndValidateExists(@"C:\does-not-exist-12345"));
    }

    [Fact]
    public void JsonConverter_RoundTrip_Works()
    {
        var temp = System.IO.Path.GetTempPath();
        var dir = EntryDirectory.Parse(temp);
        var options = new JsonSerializerOptions
        {
            Converters = { new EntryDirectoryJsonConverter() }
        };
        var json = JsonSerializer.Serialize(dir, options);
        var deserialized = JsonSerializer.Deserialize<EntryDirectory>(json, options);
        Assert.Equal(dir.Value, deserialized.Value);
    }
}
