using WTLauncher.App.ViewModels;
using WTLauncher.Core;

using WTLauncher.Tests;
namespace WTLauncher.Tests.ViewModelBehavior;

[Trait("Category", TestCategories.ViewModelBehavior)]
public sealed class DiscoverAgentsViewModelDirectoryModeTests
{
    private static DiscoverAgentsViewModel CreateViewModel() => new(
        _ => AgentDiscoveryResult.Empty,
        [],
        [
            new DirectoryRow(@"D:\beta", 1, ["Beta"]),
            new DirectoryRow(@"D:\alpha", 2, ["Alpha", "Alpha 2"]),
        ],
        new Dictionary<string, string>
        {
            ["opencode"] = "opencode --continue",
            ["codex"] = "codex --full-auto",
        });

    [Fact]
    public void Constructor_SortsDirectoriesAndTemplateKeys()
    {
        var viewModel = CreateViewModel();

        Assert.Equal([@"D:\alpha", @"D:\beta"], viewModel.ExistingDirectories.Select(row => row.Directory));
        Assert.Equal(["codex", "opencode"], viewModel.CommandTemplateKeys);
        Assert.Equal("codex", viewModel.SelectedCommandTemplateKey);
        Assert.Equal("codex --full-auto", viewModel.CommandPreview);
    }

    [Fact]
    public void SelectingDirectory_FillsNameAndEnablesDirectoryConfirmation()
    {
        var viewModel = CreateViewModel();
        viewModel.Mode = DiscoveryMode.ExistingDirectory;

        viewModel.SelectedDirectory = viewModel.ExistingDirectories[0];

        Assert.Equal("alpha", viewModel.EntryName);
        Assert.True(viewModel.CanCreateFromDirectory, "directory and default template should enable creation");
        Assert.True(viewModel.CanConfirm, "directory mode should use directory confirmation state");
    }

    [Fact]
    public void GetResult_ReturnsTypedTrimmedEntryDraft()
    {
        var viewModel = CreateViewModel();
        viewModel.Mode = DiscoveryMode.ExistingDirectory;
        viewModel.SelectedDirectory = viewModel.ExistingDirectories[1];
        viewModel.SelectedCommandTemplateKey = "opencode";
        viewModel.EntryName = "  自定义名称  ";

        var selection = Assert.IsType<ExistingDirectorySelection>(viewModel.GetResult());

        Assert.Equal("自定义名称", selection.Draft.Name);
        Assert.Equal(@"D:\beta", selection.Draft.Directory);
        Assert.Equal("$opencode", selection.Draft.Command);
        Assert.Equal(32, selection.Draft.Id.Length);
    }

    [Fact]
    public void ModeSwitch_PreservesDirectoryState()
    {
        var viewModel = CreateViewModel();
        viewModel.Mode = DiscoveryMode.ExistingDirectory;
        viewModel.SelectedDirectory = viewModel.ExistingDirectories[0];
        viewModel.EntryName = "保留名称";

        viewModel.Mode = DiscoveryMode.PathScan;
        viewModel.Mode = DiscoveryMode.ExistingDirectory;

        Assert.Equal(@"D:\alpha", viewModel.SelectedDirectory?.Directory);
        Assert.Equal("保留名称", viewModel.EntryName);
    }

    [Theory]
    [InlineData(@"D:\projects\demo\", "demo")]
    [InlineData(@"D:\projects\demo", "demo")]
    public void DefaultNameFromDirectory_HandlesTrailingSeparator(string directory, string expected)
    {
        Assert.Equal(expected, DiscoverAgentsViewModel.DefaultNameFromDirectory(directory));
    }
}
