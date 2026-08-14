using WTLauncher.App.Features;
using WTLauncher.App.ViewModels;
using WTLauncher.Core;

using WTLauncher.Tests;
namespace WTLauncher.Tests.ViewModelBehavior;

[Trait("Category", TestCategories.ViewModelBehavior)]
public sealed partial class MainWindowViewModelTests
{
    [Fact]
    public void StaleDirectory_LoadsMissingEntryAsVisibleAndCounts()
    {
        using var temp = new TempDirectory();
        var existing = System.IO.Path.Combine(temp.Path, "existing");
        System.IO.Directory.CreateDirectory(existing);
        var missing = System.IO.Path.Combine(temp.Path, "missing");
        var (config, _, _) = CreatePortableFixture(temp,
            Entry("existing", "Existing", existing),
            Entry("missing", "Missing", missing));
        var vm = CreateViewModel(config, config);

        Assert.True(vm.Entries.Single(e => e.Id == "missing").IsStaleDirectory);
        Assert.False(vm.Entries.Single(e => e.Id == "existing").IsStaleDirectory);
        Assert.Equal(1, vm.StaleDirectoryCount);
        Assert.True(vm.HasStaleDirectories);
    }

    [Fact]
    public async Task RepairDirectory_EditToExistingPathClearsStaleState()
    {
        using var temp = new TempDirectory();
        var existing = System.IO.Path.Combine(temp.Path, "existing");
        System.IO.Directory.CreateDirectory(existing);
        var missing = System.IO.Path.Combine(temp.Path, "missing");
        var (config, _, _) = CreatePortableFixture(temp, Entry("one", "One", missing));
        var dialog = new StubDialogService
        {
            OnRequestEntryDraft = (_, _, _) => Task.FromResult<EntryDraft?>(
                new EntryDraft("one", "One", existing, "pwsh")),
        };
        var vm = CreateViewModel(config, config, dialogService: dialog);
        var entry = vm.Entries.Single();
        Assert.True(entry.IsStaleDirectory);

        await entry.RepairDirectoryCommand.ExecuteAsync(null);

        Assert.False(entry.IsStaleDirectory);
        Assert.Equal(0, vm.StaleDirectoryCount);
        Assert.False(vm.HasStaleDirectories);
        Assert.Equal(existing, LauncherStore.Load(config).Entries.Single().Directory.Value);
    }

    [Fact]
    public async Task DeleteStaleEntry_UpdatesCountAndReferences()
    {
        using var temp = new TempDirectory();
        var existing = System.IO.Path.Combine(temp.Path, "existing");
        System.IO.Directory.CreateDirectory(existing);
        var missing = System.IO.Path.Combine(temp.Path, "missing");
        var (config, settings, presets) = CreatePortableFixture(temp,
            Entry("one", "One", missing),
            Entry("two", "Two", existing));
        SavePresetFile(presets, "daily", Preset("daily", "Daily", "one", "two"));
        var dialog = new StubDialogService
        {
            OnConfirmDeleteEntry = _ => Task.FromResult(true),
        };
        var vm = CreateViewModel(config, settings, dialogService: dialog);
        var stale = vm.Entries.Single(e => e.Id == "one");

        await stale.DeleteCommand.ExecuteAsync(null);

        Assert.Equal(0, vm.StaleDirectoryCount);
        Assert.DoesNotContain(vm.Entries, e => e.Id == "one");
        Assert.Equal(["two"], LauncherStore.Load(config).Entries.Select(e => e.Id.Value).ToArray());
        Assert.Equal(["two"], LauncherStore.Load(config).Presets.Single().EntryIds);
    }

    [Fact]
    public void StaleDirectoryPresentation_IsExposedInViewsAndTokens()
    {
        var entriesXaml = System.IO.File.ReadAllText(TestRepositoryPaths.View("LauncherEntriesView.axaml"));
        var controlXaml = System.IO.File.ReadAllText(TestRepositoryPaths.View("LauncherControlBar.axaml"));
        var tokens = System.IO.File.ReadAllText(TestRepositoryPaths.CoreSource("LauncherDesignTokens.cs"));
        var app = System.IO.File.ReadAllText(TestRepositoryPaths.AppCodeBehind);

        Assert.Equal(3, CountOccurrences(entriesXaml, "Classes.stale=\"{Binding IsStaleDirectory}\""));
        Assert.Equal(3, CountOccurrences(entriesXaml, "Command=\"{Binding RepairDirectoryCommand}\""));
        Assert.Contains("StaleDirectoryCount", controlXaml, StringComparison.Ordinal);
        Assert.Contains("BadgeWarningBackgroundColor", tokens, StringComparison.Ordinal);
        Assert.Contains("BadgeWarningForegroundColor", tokens, StringComparison.Ordinal);
        Assert.Contains("Resources[\"BadgeWarningBackgroundBrush\"]", app, StringComparison.Ordinal);
        Assert.Contains("LauncherDesignTokens.BadgeWarningBackgroundColor", app, StringComparison.Ordinal);
        Assert.Contains("Resources[\"BadgeWarningForegroundBrush\"]", app, StringComparison.Ordinal);
        Assert.Contains("LauncherDesignTokens.BadgeWarningForegroundColor", app, StringComparison.Ordinal);
    }

    private static int CountOccurrences(string text, string value)
    {
        var count = 0;
        var index = 0;
        while ((index = text.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }
        return count;
    }
}