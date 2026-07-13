using WTLauncher.App.ViewModels;
using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed class CommandManagementViewModelTests
{
    [Fact]
    public void Constructor_ExposesBindableTabEmptyAndSelectionProjections()
    {
        var viewModel = new CommandManagementViewModel(
            [new CommandRow("codex", 1, ["项目"])],
            [],
            _ => throw new InvalidOperationException("not used"));

        Assert.True(viewModel.IsCommandTabActive, "command tab should be active initially");
        Assert.False(viewModel.IsTemplateTabActive, "template tab should be inactive initially");
        Assert.True(viewModel.HasCommandRows, "initial command row should be visible");
        Assert.False(viewModel.HasNoCommandRows, "command empty state should be hidden");
        Assert.False(viewModel.HasTemplateRows, "template list should start empty");
        Assert.True(viewModel.HasNoTemplateRows, "template empty state should be visible");
        Assert.False(viewModel.HasSelectedCommand, "nothing should be selected initially");
        Assert.False(viewModel.HasSelectedTemplate, "nothing should be selected initially");
    }

    [Fact]
    public void TabCommands_UpdateSingleActiveTabProjection()
    {
        var viewModel = new CommandManagementViewModel([], [], _ => throw new InvalidOperationException("not used"));

        viewModel.SelectTemplateTabCommand.Execute(null);

        Assert.False(viewModel.IsCommandTabActive, "command tab should deactivate");
        Assert.True(viewModel.IsTemplateTabActive, "template tab should activate");

        viewModel.SelectCommandTabCommand.Execute(null);

        Assert.True(viewModel.IsCommandTabActive, "command tab should reactivate");
        Assert.False(viewModel.IsTemplateTabActive, "template tab should deactivate");
    }

    [Fact]
    public void SuccessfulEdit_RefreshesRowsAndEmptyStateProjections()
    {
        var viewModel = new CommandManagementViewModel(
            [],
            [],
            _ => new CommandManagementResult(
                true,
                "已添加模板",
                [],
                [new TemplateRow("codex", "codex --full-auto", 0)]));

        viewModel.AddTemplateCommand.Execute("codex\tcodex --full-auto");

        Assert.Equal("已添加模板", viewModel.StatusMessage);
        Assert.Single(viewModel.TemplateRows);
        Assert.True(viewModel.HasTemplateRows, "successful result should reveal the template list");
        Assert.False(viewModel.HasNoTemplateRows, "successful result should hide the empty state");
    }
}
