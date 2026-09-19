using System.Collections.Immutable;
using WTLauncher.App.ViewModels;
using WTLauncher.Core;

using WTLauncher.Tests;
namespace WTLauncher.Tests.ViewModelBehavior;

[Trait("Category", TestCategories.ViewModelBehavior)]
public sealed class ImportConfigViewModelTests
{
    [Fact]
    public void SummaryAndPlanReflectDuplicateMissingAndInvalidPresetWarnings()
    {
        using var temp = new TempDirectory();
        var missing = Path.Combine(temp.Path, "missing");
        var current = Store([Entry("one", "One")], []);
        var imported = Store(
            [Entry("one", "One"), Entry("two", "Two", missing)],
            [Preset("bad", "Bad", "one", "ghost")]);

        var viewModel = new ImportConfigViewModel(current, imported);

        Assert.Contains("重复", viewModel.Summary, StringComparison.Ordinal);
        Assert.Contains("不存在", viewModel.Summary, StringComparison.Ordinal);
        Assert.Contains("无效", viewModel.Summary, StringComparison.Ordinal);

        var addPlan = viewModel.BuildPlan();
        Assert.Equal(LauncherImportMode.AddNew, addPlan.Mode);

        viewModel.ReplaceMode = true;
        Assert.True(viewModel.ReplaceMode);
        Assert.False(viewModel.AddNewMode);

        var replacePlan = viewModel.BuildPlan();
        Assert.Equal(LauncherImportMode.Replace, replacePlan.Mode);
    }

    private static LaunchEntry Entry(string id, string name, string? directory = null) =>
        new()
        {
            Id = id,
            Name = name,
            Directory = directory is null ? DirectoryPath.Empty : (DirectoryPath)directory,
            Command = "pwsh",
        };

    private static LauncherPreset Preset(string id, string name, params string[] entryIds) =>
        new() { Id = id, Name = name, EntryIds = [.. entryIds] };

    private static LauncherStoreFile Store(
        ImmutableList<LaunchEntry> entries,
        ImmutableList<LauncherPreset> presets,
        string defaultPresetId = "") =>
        new()
        {
            SchemaVersion = 2,
            Entries = entries,
            Presets = presets,
            Settings = new LauncherSettings(),
            DefaultPresetId = defaultPresetId,
        };
}
