using IOPath = System.IO.Path;

namespace WTLauncher.Tests;

internal sealed class TempDirectory : IDisposable
{
    public TempDirectory(string? prefix = null)
    {
        Path = IOPath.Combine(
            IOPath.GetTempPath(),
            $"{prefix ?? "wt-launcher-test"}-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path);
    }

    public string Path { get; }

    public void Dispose()
    {
        if (Directory.Exists(Path))
            Directory.Delete(Path, recursive: true);
    }
}
