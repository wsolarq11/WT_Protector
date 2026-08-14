using WTLauncher.App.Features;
using WTLauncher.App.ViewModels;

using WTLauncher.Tests;
namespace WTLauncher.Tests.ViewModelBehavior;

[Trait("Category", TestCategories.ViewModelBehavior)]
public sealed partial class MainWindowViewModelTests
{
    [Fact]
    public void StatusContentChange_NotifiesTextAndEveryDerivedPresentationProperty()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreatePortableFixture(temp, oneSelected: false, twoSelected: false);
        var viewModel = CreateViewModel(config, settings);
        var changed = new HashSet<string>();
        viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName is not null)
                changed.Add(args.PropertyName);
        };

        viewModel.StatusContent = LauncherStatusContent.Error("存储保存失败: access denied");

        Assert.Equal(LauncherStatusTone.Error, viewModel.StatusTone);
        Assert.True(viewModel.IsStatusError, "error status should activate the non-neutral projection");
        Assert.Contains(nameof(MainWindowViewModel.StatusText), changed);
        Assert.Contains(nameof(MainWindowViewModel.StatusTone), changed);
        Assert.Contains(nameof(MainWindowViewModel.IsStatusNeutral), changed);
        Assert.Contains(nameof(MainWindowViewModel.IsStatusSuccess), changed);
        Assert.Contains(nameof(MainWindowViewModel.IsStatusWarning), changed);
        Assert.Contains(nameof(MainWindowViewModel.IsStatusError), changed);
    }
}
