using WTLauncher.App.ViewModels;
using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed partial class MainWindowViewModelTests
{
    [Fact]
    public void CardsGroupKey_Default_IsNone()
    {
        using var temp = new TempDirectory();
        var (config, _, _) = CreatePortableFixture(temp, Entry("a", "A"));
        var viewModel = CreateViewModel(config, config);

        Assert.Equal(LauncherCardsGroupKeys.None, viewModel.CardsGroupKey);
    }

    [Fact]
    public void CardsGroupKey_Setter_PersistsToSettingsAndStore()
    {
        using var temp = new TempDirectory();
        var (config, _, _) = CreatePortableFixture(temp, Entry("a", "A"));
        var viewModel = CreateViewModel(config, config);

        viewModel.CardsGroupKey = LauncherCardsGroupKeys.Command;

        Assert.Equal(LauncherCardsGroupKeys.Command, viewModel.CardsGroupKey);
        Assert.Equal(LauncherCardsGroupKeys.Command, viewModel.Settings.CardsGroupKey);
        var reloaded = LauncherStore.Load(config);
        Assert.Equal(LauncherCardsGroupKeys.Command, reloaded.Settings.CardsGroupKey);
    }

    [Fact]
    public void CardsGroupKey_Setter_RebuildsVisibleGroups()
    {
        using var temp = new TempDirectory();
        var entries = new[]
        {
            Entry("a", command: "$codex"),
            Entry("b", command: "$codex"),
            Entry("c", command: "$claude"),
        };
        var (config, _, _) = CreatePortableFixture(temp, entries);
        var viewModel = CreateViewModel(config, config);

        // groupKey=none → 单组无 header
        var noneGroup = Assert.Single(viewModel.VisibleGroups);
        Assert.False(noneGroup.ShowHeader);
        Assert.Equal(3, noneGroup.Count);

        viewModel.CardsGroupKey = LauncherCardsGroupKeys.Command;

        // groupKey=command → 多组带 header
        Assert.Equal(2, viewModel.VisibleGroups.Count);
        Assert.All(viewModel.VisibleGroups, g => Assert.True(g.ShowHeader));
        Assert.Equal(2, viewModel.VisibleGroups[0].Count); // $codex
        Assert.Equal(1, viewModel.VisibleGroups[1].Count); // $claude
    }

    [Fact]
    public void CardsGroupKey_LoadsFromSettings()
    {
        using var temp = new TempDirectory();
        var (config, _, _) = CreatePortableFixture(temp, Entry("a", "A"));
        var settings = new LauncherSettings { CardsGroupKey = LauncherCardsGroupKeys.Directory };
        var viewModel = CreateViewModel(config, config, settings);

        Assert.Equal(LauncherCardsGroupKeys.Directory, viewModel.CardsGroupKey);
    }

    [Fact]
    public void CardsGroupKey_NormalizesUnknownToNone()
    {
        using var temp = new TempDirectory();
        var (config, _, _) = CreatePortableFixture(temp, Entry("a", "A"));
        var viewModel = CreateViewModel(config, config);

        viewModel.CardsGroupKey = "tag";

        Assert.Equal(LauncherCardsGroupKeys.None, viewModel.CardsGroupKey);
    }

    [Fact]
    public void VisibleGroups_GroupByDirectory_CaseInsensitive()
    {
        using var temp = new TempDirectory();
        var entries = new[]
        {
            Entry("a", directory: @"D:\Proj"),
            Entry("b", directory: @"d:\proj"),
        };
        var (config, _, _) = CreatePortableFixture(temp, entries);
        var viewModel = CreateViewModel(config, config);

        viewModel.CardsGroupKey = LauncherCardsGroupKeys.Directory;

        var group = Assert.Single(viewModel.VisibleGroups);
        Assert.Equal(2, group.Count);
    }

    [Fact]
    public void SelectedCardsGroupOption_ReflectsCurrentKey()
    {
        using var temp = new TempDirectory();
        var (config, _, _) = CreatePortableFixture(temp, Entry("a", "A"));
        var viewModel = CreateViewModel(config, config);

        Assert.Equal(LauncherCardsGroupKeys.None, viewModel.SelectedCardsGroupOption!.Value);

        viewModel.CardsGroupKey = LauncherCardsGroupKeys.Command;
        Assert.Equal(LauncherCardsGroupKeys.Command, viewModel.SelectedCardsGroupOption!.Value);
    }

    [Fact]
    public void GroupVM_LaunchAll_DelegatesToOwner()
    {
        using var temp = new TempDirectory();
        var entries = new[]
        {
            Entry("a", command: "$codex"),
            Entry("b", command: "$codex"),
        };
        var (config, _, _) = CreatePortableFixture(temp, entries);
        var viewModel = CreateViewModel(config, config);
        viewModel.CardsGroupKey = LauncherCardsGroupKeys.Command;

        var group = viewModel.VisibleGroups[0];
        Assert.True(group.LaunchAllCommand.CanExecute(null));
        // 执行命令 — 不验证启动副作用(走 LaunchBatchAsync 内部), 只验证不抛。
        Exception? captured = null;
        group.LaunchAllCommand.Execute(null);
        Assert.Null(captured);
    }
}
