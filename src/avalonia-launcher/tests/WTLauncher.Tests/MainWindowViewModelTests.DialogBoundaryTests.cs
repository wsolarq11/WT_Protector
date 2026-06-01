using WTLauncher.App.ViewModels;

namespace WTLauncher.Tests;

public sealed partial class MainWindowViewModelTests
{
    [Fact]
    public void DialogBoundaryHandlersRejectNullAssignments()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreateSingleEntryFixture(temp);
        var viewModel = CreateViewModel(config, settings);

        AssertRejectsNullHandler(() => viewModel.RequestEntryDraftAsync = null!);
        AssertRejectsNullHandler(() => viewModel.ConfirmDeleteEntryAsync = null!);
        AssertRejectsNullHandler(() => viewModel.ConfirmLaunchAsync = null!);
        AssertRejectsNullHandler(() => viewModel.ConfirmBatchLaunchAsync = null!);
        AssertRejectsNullHandler(() => viewModel.RequestNewPresetAsync = null!);
        AssertRejectsNullHandler(() => viewModel.ConfirmDeletePresetAsync = null!);
    }

    private static void AssertRejectsNullHandler(Action assignNull)
    {
        var exception = Assert.Throws<ArgumentNullException>(assignNull);

        Assert.Equal("value", exception.ParamName);
    }
}
