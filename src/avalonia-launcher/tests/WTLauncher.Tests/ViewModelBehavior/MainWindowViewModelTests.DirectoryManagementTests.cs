using WTLauncher.App.Features.Dialogs;
using WTLauncher.App.ViewModels;
using WTLauncher.Core;

using WTLauncher.Tests;
namespace WTLauncher.Tests.ViewModelBehavior;

// ponytail: inline arrays in test inputs — perf-irrelevant in tests.
#pragma warning disable CA1861

[Trait("Category", TestCategories.ViewModelBehavior)]
public sealed partial class MainWindowViewModelTests
{
    [Fact]
    public void ApplyDirectoryManagementEdit_RenameDirectory_UpdatesSingleReference()
    {
        using var temp = new TempDirectory();
        var (config, _, _) = CreatePortableFixture(
            temp,
            Entry("a", "A", directory: @"D:\old", command: "pwsh"),
            Entry("b", "B", directory: @"D:\other", command: "pwsh"));
        var viewModel = CreateViewModel(config, config);

        var result = viewModel.ApplyDirectoryManagementEdit(
            new DirectoryManagementEdit.RenameDirectory(@"D:\old", @"D:\new"));

        Assert.True(result.Success);
        Assert.Equal(@"D:\new", viewModel.Entries[0].Directory);
        Assert.Equal(@"D:\other", viewModel.Entries[1].Directory);
    }

    [Fact]
    public void ApplyDirectoryManagementEdit_RenameDirectory_UpdatesAllReferencesCaseInsensitive()
    {
        using var temp = new TempDirectory();
        var entries = new[]
        {
            Entry("a", "A", directory: @"D:\Proj"),
            Entry("b", "B", directory: @"d:\proj"),
            Entry("c", "C", directory: @"D:\PROJ"),
        };
        var (config, _, _) = CreatePortableFixture(temp, entries);
        var viewModel = CreateViewModel(config, config);

        var result = viewModel.ApplyDirectoryManagementEdit(
            new DirectoryManagementEdit.RenameDirectory(@"D:\Proj", @"D:\Renamed"));

        Assert.True(result.Success);
        Assert.All(viewModel.Entries, e => Assert.Equal(@"D:\Renamed", e.Directory));
    }

    [Fact]
    public void ApplyDirectoryManagementEdit_MergeDirectories_ReassignsLoserToKeeper()
    {
        // ponytail: 当前 launcher.json 现状 — 3 对重复 directory 各 2 个 entry
        using var temp = new TempDirectory();
        var (config, _, _) = CreatePortableFixture(
            temp,
            Entry("a", "A", directory: @"D:\proj1", command: "codex"),
            Entry("b", "B", directory: @"D:\proj1", command: "opencode"),
            Entry("c", "C", directory: @"D:\proj2", command: "codex"));
        var viewModel = CreateViewModel(config, config);

        var result = viewModel.ApplyDirectoryManagementEdit(
            new DirectoryManagementEdit.MergeDirectories(
                LoserDirectory: @"D:\proj1",
                KeeperDirectory: @"D:\proj2"));

        Assert.True(result.Success);
        Assert.All(viewModel.Entries, e => Assert.Equal(@"D:\proj2", e.Directory));
    }

    [Fact]
    public void ApplyDirectoryManagementEdit_MergeDirectories_RejectsSameDirectory()
    {
        using var temp = new TempDirectory();
        var (config, _, _) = CreatePortableFixture(
            temp,
            Entry("a", "A", directory: @"D:\x"));
        var viewModel = CreateViewModel(config, config);

        var result = viewModel.ApplyDirectoryManagementEdit(
            new DirectoryManagementEdit.MergeDirectories(@"D:\x", @"D:\x"));

        // 语义阻止 — 但 Success=true 因为 ApplyEdit 返回 status message 而不抛。
        Assert.True(result.Success);
        Assert.Contains("相同", result.StatusMessage);
    }

    [Fact]
    public void ApplyDirectoryManagementEdit_PersistsChangesToStore()
    {
        using var temp = new TempDirectory();
        var (config, _, _) = CreatePortableFixture(
            temp,
            Entry("a", "A", directory: @"D:\old"));
        var viewModel = CreateViewModel(config, config);

        viewModel.ApplyDirectoryManagementEdit(
            new DirectoryManagementEdit.RenameDirectory(@"D:\old", @"D:\new"));

        var reloaded = LauncherStore.Load(config);
        Assert.Equal(@"D:\new", reloaded.Entries[0].Directory.Value);
    }

    [Fact]
    public async Task OpenDirectoryManagement_InvokesInjectedDialogWithApplyCallback()
    {
        using var temp = new TempDirectory();
        var (config, _, _) = CreatePortableFixture(
            temp,
            Entry("a", "A", directory: @"D:\x"),
            Entry("b", "B", directory: @"D:\x"));
        IDirectoryEditApplier? capturedApply = null;
        var dialogService = new StubDialogService
        {
            OnOpenDirectoryManagement = (_, apply) =>
            {
                capturedApply = apply;
                return Task.CompletedTask;
            },
        };
        var viewModel = CreateViewModel(config, config, dialogService: dialogService);

        await viewModel.OpenDirectoryManagementCommand.ExecuteAsync(null);

        Assert.NotNull(capturedApply);
        var result = capturedApply!.Apply(new DirectoryManagementEdit.RenameDirectory(@"D:\x", @"D:\y"));
        Assert.True(result.Success);
        Assert.All(viewModel.Entries, e => Assert.Equal(@"D:\y", e.Directory));
    }

    [Fact]
    public async Task OpenDirectoryManagement_DefaultDialog_DoesNotThrow()
    {
        using var temp = new TempDirectory();
        var (config, _, _) = CreatePortableFixture(temp, Entry("a", "A"));
        var viewModel = CreateViewModel(config, config);

        await viewModel.OpenDirectoryManagementCommand.ExecuteAsync(null);

        Assert.NotNull(viewModel.StatusText);
    }

    [Fact]
    public void ApplyDirectoryManagementEdit_RenameDirectory_SamePathReturnsNoChangeMessage()
    {
        using var temp = new TempDirectory();
        var (config, _, _) = CreatePortableFixture(
            temp,
            Entry("a", "A", directory: @"D:\x"));
        var viewModel = CreateViewModel(config, config);

        var result = viewModel.ApplyDirectoryManagementEdit(
            new DirectoryManagementEdit.RenameDirectory(@"D:\x", @"D:\x"));

        Assert.True(result.Success);
        Assert.Contains("无变更", result.StatusMessage);
        Assert.Equal(@"D:\x", viewModel.Entries[0].Directory);
    }

    [Fact]
    public void ApplyDirectoryManagementEdit_SuccessRefreshesResultRowsAndFilteredMainList()
    {
        using var temp = new TempDirectory();
        var (config, _, _) = CreatePortableFixture(
            temp,
            Entry("a", "A", directory: @"D:\old"),
            Entry("b", "B", directory: @"D:\old"));
        var viewModel = CreateViewModel(config, config);
        viewModel.FilterText = @"D:\new";
        Assert.Empty(viewModel.VisibleEntries);

        var result = viewModel.ApplyDirectoryManagementEdit(
            new DirectoryManagementEdit.RenameDirectory(@"D:\old", @"D:\new"));

        Assert.True(result.Success);
        var row = Assert.Single(result.UpdatedDirectoryRows);
        Assert.Equal(@"D:\new", row.Directory);
        Assert.Equal(2, row.ReferenceCount);
        Assert.Equal(2, viewModel.VisibleEntries.Count);
    }

    [Fact]
    public void ApplyDirectoryManagementEdit_SaveFailureRestoresEntriesAndResultRows()
    {
        using var temp = new TempDirectory();
        var (config, _, _) = CreatePortableFixture(
            temp,
            Entry("a", "A", directory: @"D:\old"));
        var viewModel = CreateViewModel(config, config);
        var configDir = Path.GetDirectoryName(config)!;
        Directory.Delete(configDir, recursive: true);
        File.WriteAllText(configDir, "blocked");

        var result = viewModel.ApplyDirectoryManagementEdit(
            new DirectoryManagementEdit.RenameDirectory(@"D:\old", @"D:\new"));

        Assert.False(result.Success);
        Assert.Equal(@"D:\old", viewModel.Entries[0].Directory);
        Assert.Equal(@"D:\old", Assert.Single(result.UpdatedDirectoryRows).Directory);
    }
}
