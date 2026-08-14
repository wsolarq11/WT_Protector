using System.Text;
using WTLauncher.App.Infrastructure;

using WTLauncher.Tests;
namespace WTLauncher.Tests.CoreUnit;

[Trait("Category", TestCategories.CoreUnit)]
public sealed class ErrorLogTests
{
    [Fact]
    public void RotatesLogFileWhenItExceeds1MB()
    {
        using var temp = new TempDirectory();
        var logPath = Path.Combine(temp.Path, "errors.log");
        var oldPath = logPath + ".old";

        // Create a log file just over 1 MB.
        CreateLargeLogFile(logPath, sizeInBytes: 1_000_001);

        // Write a new entry — rotation should fire.
        ErrorLog.Write(new InvalidOperationException("test-exception"), "test-context", temp.Path, logPath);

        // The old (large) file should have been moved to .old.
        Assert.True(File.Exists(oldPath), "Expected .old file after rotation.");

        // The new log file should be small (just the new entry).
        Assert.True(File.Exists(logPath), "Expected new log file to exist.");
        var newSize = new FileInfo(logPath).Length;
        Assert.True(newSize < 1_000_000, $"Expected new log file < 1MB, got {newSize} bytes.");

        // Content check: the new entry is in the new file.
        var contents = File.ReadAllText(logPath);
        Assert.Contains("test-exception", contents);
        Assert.Contains("test-context", contents);

        // The old file has the original large content.
        var oldContents = File.ReadAllText(oldPath);
        Assert.DoesNotContain("test-exception", oldContents);
    }

    [Fact]
    public void DoesNotRotateWhenFileIsUnder1MB()
    {
        using var temp = new TempDirectory();
        var logPath = Path.Combine(temp.Path, "errors.log");
        var oldPath = logPath + ".old";

        // Pre-populate a small log file.
        File.WriteAllText(logPath, "small entry" + Environment.NewLine);

        ErrorLog.Write(new InvalidOperationException("second-entry"), "ctx2", temp.Path, logPath);

        Assert.False(File.Exists(oldPath), "No rotation expected for small file.");
    }

    [Fact]
    public void RotationOverwritesExistingOldFile()
    {
        using var temp = new TempDirectory();
        var logPath = Path.Combine(temp.Path, "errors.log");
        var oldPath = logPath + ".old";

        // Step 1: create a large log file and write — rotation fires, moves
        // the large content to .old, creates a fresh logPath with the new entry.
        CreateLargeLogFile(logPath, sizeInBytes: 1_000_001);
        ErrorLog.Write(new InvalidOperationException("round-1"), "ctx1", temp.Path, logPath);

        Assert.True(File.Exists(oldPath), "Expected .old from first rotation.");

        // Step 2: overwrite logPath with a large string (simulating 1MB+ of log
        // from continued writes), then write again — rotation fires again and
        // overwrites the first .old.
        var overwriteContent = new string('x', 1_000_001);
        File.WriteAllText(logPath, overwriteContent);
        ErrorLog.Write(new InvalidOperationException("round-2"), "ctx2", temp.Path, logPath);

        Assert.True(File.Exists(oldPath), "Expected .old from second rotation.");

        // .old now contains the overwrite content (moved from logPath during rotation).
        var oldContents = File.ReadAllText(oldPath);
        Assert.Contains("xxxxx", oldContents);

        // The fresh logPath contains just the round-2 entry.
        var newContents = File.ReadAllText(logPath);
        Assert.Contains("round-2", newContents);
    }

    [Fact]
    public void WriteNeverThrows_EvenWithBogusPath()
    {
        // Use a path on a non-existent drive — rotation and append should fail,
        // but Write must not throw.
        var bogusPath = @"X:\impossible\path\errors.log";
        var bogusDir = @"X:\impossible\path";

        var ex = Record.Exception(() =>
            ErrorLog.Write(new InvalidOperationException("should-not-throw"), "ctx", bogusDir, bogusPath));

        Assert.Null(ex);
    }

    [Fact]
    public async Task ConcurrentWritesAreDropped_NotCorrupted()
    {
        using var temp = new TempDirectory();
        var logPath = Path.Combine(temp.Path, "errors.log");

        var writeCount = 0;
        var ready = new ManualResetEventSlim(false);

        var t1 = Task.Run(() =>
        {
            ready.Wait();
            for (int i = 0; i < 50; i++)
            {
                ErrorLog.Write(new InvalidOperationException($"t1-{i}"), "ctx", temp.Path, logPath);
                Interlocked.Increment(ref writeCount);
            }
        }, TestContext.Current.CancellationToken);

        var t2 = Task.Run(() =>
        {
            ready.Wait();
            for (int i = 0; i < 50; i++)
            {
                ErrorLog.Write(new InvalidOperationException($"t2-{i}"), "ctx", temp.Path, logPath);
                Interlocked.Increment(ref writeCount);
            }
        }, TestContext.Current.CancellationToken);

        ready.Set();
        await Task.WhenAll(t1, t2);

        // Not all writes may land (some dropped by Interlocked guard), but the file
        // must be readable and not corrupted.
        var contents = File.ReadAllText(logPath);
        Assert.True(contents.Length > 0, "At least some writes should land.");
        // The file must contain valid date-time lines.
        var lines = contents.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.All(lines, line => Assert.StartsWith("2", line)); // Each line starts with a year.
    }

    private static void CreateLargeLogFile(string path, long sizeInBytes)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        // Each line is roughly 100 bytes; write enough lines to reach target size.
        var line = new string('A', 90) + Environment.NewLine; // ~92 bytes per line
        var linesNeeded = (int)(sizeInBytes / line.Length) + 1;

        using var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read, 65536);
        using var sw = new StreamWriter(fs, Encoding.UTF8);

        for (int i = 0; i < linesNeeded && fs.Length < sizeInBytes; i++)
        {
            sw.Write(line);
        }
        sw.Flush();
    }
}
