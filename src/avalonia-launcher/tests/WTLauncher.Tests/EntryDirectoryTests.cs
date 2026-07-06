using System.Text.Json;
using WTLauncher.Core;

namespace WTLauncher.Tests;

public class EntryDirectoryTests
{
    [Fact]
    public void TryParse_NullOrEmpty_ReturnsEmptyError()
    {
        Assert.True(EntryDirectory.TryParse(null).IsErr);
        Assert.True(EntryDirectory.TryParse("").IsErr);
        Assert.True(EntryDirectory.TryParse("   ").IsErr);
    }

    [Fact]
    public void TryParse_ValidLocalPath_ReturnsOk()
    {
        var temp = System.IO.Path.GetTempPath();
        var result = EntryDirectory.TryParse(temp);
        Assert.True(result.IsOk);
    }

    [Fact]
    public void TryParseAndValidateExists_NonExistentDir_ReturnsNotFound()
    {
        var result = EntryDirectory.TryParseAndValidateExists(@"C:\does-not-exist-12345");
        Assert.True(result.IsErr);
    }

    [Fact]
    public void JsonConverter_RoundTrip_Works()
    {
        var temp = System.IO.Path.GetTempPath();
        var dir = EntryDirectory.TryParse(temp).Match(ok: d => d, err: _ => default(EntryDirectory));
        var options = new JsonSerializerOptions
        {
            Converters = { new EntryDirectoryJsonConverter() }
        };
        var json = JsonSerializer.Serialize(dir, options);
        var deserialized = JsonSerializer.Deserialize<EntryDirectory>(json, options);
        Assert.Equal(dir.Value, deserialized.Value);
    }
}
