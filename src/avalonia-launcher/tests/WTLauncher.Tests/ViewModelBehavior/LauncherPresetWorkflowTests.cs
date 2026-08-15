using WTLauncher.App.ViewModels;
using WTLauncher.Core;

using WTLauncher.Tests;
namespace WTLauncher.Tests.ViewModelBehavior;

[Trait("Category", TestCategories.ViewModelBehavior)]
public sealed partial class MainWindowViewModelTests
{
    [Fact]
    public void PresetWorkflowBuildsSelectionPreviewFromSelectedEntries()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreatePortableFixture(
            temp,
            Entry("one", "One", command: "codex", selected: true),
            Entry("two", "Two", command: "opencode"));
        var viewModel = CreateViewModel(config, settings);

        var preview = viewModel._presetWorkflow.BuildSelectionPreview(["one"]);
        Assert.NotNull(preview);
        Assert.Single(preview.SelectedIds);
        Assert.True(preview.HasChanges);
    }

    [Fact]
    public void PresetWorkflowApplyPersistsPresetSelection()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreatePortableFixture(
            temp,
            Entry("one", "One", command: "codex", selected: true),
            Entry("two", "Two", command: "opencode"));
        var viewModel = CreateViewModel(config, settings);

        viewModel._presetWorkflow.ApplyPreset();

        Assert.Equal(2, viewModel.SelectedCount);
    }
}
