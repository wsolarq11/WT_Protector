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

    // ── Constructor parameter null validation ────────────────────────────
    //
    // Rule: All public constructors of Service classes must validate
    // reference-type parameters (?? throw / ThrowIfNull).

    [Fact]
    public void ServiceConstructors_ValidateParameters()
    {
        var violations = new List<string>();

        // EntryService: public EntryService(IDialogService dialog)
        var entrySource = File.ReadAllText(
            Path.Combine(AppRoot, "Features", "Entries", "EntryService.cs"));
        if (!entrySource.Contains("dialog ?? throw new ArgumentNullException(nameof(dialog))"))
            violations.Add("EntryService(IDialogService dialog) missing null guard for 'dialog'");

        // PresetService: public PresetService(IDialogService dialog)
        var presetSource = File.ReadAllText(
            Path.Combine(AppRoot, "Features", "Presets", "PresetService.cs"));
        if (!presetSource.Contains("dialog ?? throw new ArgumentNullException(nameof(dialog))"))
            violations.Add("PresetService(IDialogService dialog) missing null guard for 'dialog'");

        Assert.Empty(violations);
    }

    // ── Async method CancellationToken (informational gap tracking) ───────
    //
    // Rule: Async public methods in service interfaces should declare a
    // CancellationToken parameter. This test documents the current gap.

    [Fact]
    public void ServiceInterfaces_AsyncMethods_Gap_NoCancellationToken()
    {
        var violations = new List<string>();

        foreach (var (name, path) in new Dictionary<string, string>
        {
            ["IEntryService"] = Path.Combine(AppRoot, "Features", "Entries", "IEntryService.cs"),
            ["IPresetService"] = Path.Combine(AppRoot, "Features", "Presets", "IPresetService.cs"),
            ["IDialogService"] = Path.Combine(AppRoot, "Features", "Dialogs", "IDialogService.cs"),
        })
        {
            if (!File.Exists(path)) continue;
            var source = File.ReadAllText(path);
            var lines = source.Split('\n');

            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (line.Contains("Task<") || line.Contains("Task "))
                {
                    var decl = ExtractMethodSignature(lines, i);
                    var hasTask = decl.Contains("Task<") || decl.Contains("Task ");
                    var hasCt = decl.Contains("CancellationToken");

                    if (hasTask && !hasCt)
                        violations.Add($"{name}: {decl.Split('(')[0].Trim()}(...)");
                }
            }
        }

        var msg = violations.Count > 0
            ? $"Known gap: {violations.Count} async method(s) lack CancellationToken:\n"
              + string.Join("\n", violations.Select(v => $"  {v}"))
            : "All async methods declare CancellationToken.";
        // ponytail: Known gap — services don't yet declare CancellationToken.
        // Output as informational; update threshold as methods are migrated.
        Assert.True(violations.Count <= 21, msg);
    }

    // ── Helper ───────────────────────────────────────────────────────────

    private static string ExtractMethodSignature(string[] lines, int start)
    {
        var sig = "";
        var depth = 0;
        var started = false;

        for (var i = start; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            sig += line + " ";

            if (!started)
            {
                if (line.Contains('(')) { started = true; depth = 1; }
                continue;
            }

            depth += line.Count(c => c == '(') - line.Count(c => c == ')');
            if (depth <= 0) break;
        }

        return sig.Trim();
    }
}
