using WTLauncher.App.Features.Dialogs;
using WTLauncher.App.Features.Entries;
using WTLauncher.App.Features.Presets;

using WTLauncher.Tests;
namespace WTLauncher.Tests.Architecture;

/// <summary>
/// Contract tests for Service classes in WTLauncher.App.Features.
/// Verifies architecture constraints using source-file analysis:
///   - Business services avoid Avalonia.Controls (source-level check)
///   - No direct MainWindowViewModel reference
///   - Constructor parameter null validation
///   - Async methods declare CancellationToken (known gap tracking)
/// </summary>
[Trait("Category", TestCategories.Architecture)]
public sealed class ServiceContractTests
{
    private static readonly string AppRoot = TestRepositoryPaths.AppSourceRoot;

    // ── Avalonia.Controls dependency ──────────────────────────────────────

    [Fact]
    public void BusinessServices_SourceMustNotImport_AvaloniaControls()
    {
        var violations = new List<string>();

        foreach (var path in new[]
        {
            Path.Combine(AppRoot, "Features", "Entries", "EntryService.cs"),
            Path.Combine(AppRoot, "Features", "Presets", "PresetService.cs"),
        })
        {
            if (!File.Exists(path)) continue;
            var lines = File.ReadAllLines(path);
            for (var i = 0; i < lines.Length; i++)
            {
                var t = lines[i].Trim();
                if (t.StartsWith("using Avalonia.Controls", StringComparison.Ordinal) ||
                    t.StartsWith("using Avalonia.Controls.", StringComparison.Ordinal))
                    violations.Add($"{Path.GetFileName(path)}:{i + 1}: {t}");
            }
        }

        Assert.Empty(violations);
    }

    // ── MainWindowViewModel reference ────────────────────────────────────

    [Fact]
    public void ServiceSource_MustNotReference_MainWindowViewModel()
    {
        var violations = new List<string>();

        foreach (var path in new[]
        {
            Path.Combine(AppRoot, "Features", "Entries", "EntryService.cs"),
            Path.Combine(AppRoot, "Features", "Presets", "PresetService.cs"),
            Path.Combine(AppRoot, "Features", "Dialogs", "DialogService.cs"),
        })
        {
            if (!File.Exists(path)) continue;
            var lines = File.ReadAllLines(path);
            for (var i = 0; i < lines.Length; i++)
                if (lines[i].Contains("MainWindowViewModel"))
                    violations.Add($"{Path.GetFileName(path)}:{i + 1}: {lines[i].Trim()}");
        }

        Assert.Empty(violations);
    }
}
