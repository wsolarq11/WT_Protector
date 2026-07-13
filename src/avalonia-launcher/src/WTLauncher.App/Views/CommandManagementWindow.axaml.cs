using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using WTLauncher.App.ViewModels;
using WTLauncher.Core;

namespace WTLauncher.App.Views;

public sealed partial class CommandManagementWindow : Window
{
    private readonly CommandManagementViewModel _viewModel = null!;

    public CommandManagementWindow()
    {
        InitializeComponent();
    }

    public CommandManagementWindow(CommandManagementViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        _viewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();
    }

    private void OnCloseClicked(object? sender, RoutedEventArgs e) => Close(null);

    private async void OnRenameCommandClicked(object? sender, RoutedEventArgs e)
    {
        if (_viewModel.SelectedCommandRow is { } row)
            await OpenRenameCommandDialog(row);
    }

    private async void OnAddTemplateClicked(object? sender, RoutedEventArgs e) =>
        await OpenAddTemplateDialog();

    private async void OnUpdateTemplateKeyClicked(object? sender, RoutedEventArgs e)
    {
        if (_viewModel.SelectedTemplateRow is { } row)
            await OpenUpdateTemplateKeyDialog(row);
    }

    private async void OnUpdateTemplateValueClicked(object? sender, RoutedEventArgs e)
    {
        if (_viewModel.SelectedTemplateRow is { } row)
            await OpenUpdateTemplateValueDialog(row);
    }

    private async void OnCommandRowDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is Control { DataContext: CommandRow row })
        {
            _viewModel.SelectedCommandRow = row;
            await OpenRenameCommandDialog(row);
        }
    }

    private async void OnTemplateRowDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is Control { DataContext: TemplateRow row })
        {
            _viewModel.SelectedTemplateRow = row;
            await OpenUpdateTemplateValueDialog(row);
        }
    }

    private async Task OpenRenameCommandDialog(CommandRow row)
    {
        var dialog = new CommandEditDialog(
            title: $"编辑命令: {row.Command}",
            label: "新命令 (可填 $key 引用模板, 或裸命令)",
            initialValue: row.Command,
            templateKeys: _viewModel.TemplateKeys);
        var result = await dialog.ShowDialog<string?>(this);
        if (!string.IsNullOrWhiteSpace(result))
            _viewModel.RenameCommandCommand.Execute(result);
    }

    private async Task OpenAddTemplateDialog()
    {
        var dialog = new CommandEditDialog(
            title: "新模板",
            label: "模板 Key (不含 $ 前缀)",
            initialValue: string.Empty,
            templateKeys: Array.Empty<string>(),
            secondLabel: "展开内容 (例: codex --enable goals ...)");
        var result = await dialog.ShowDialog<(string Key, string Value)>(this);
        if (!string.IsNullOrWhiteSpace(result.Key))
            _viewModel.AddTemplateCommand.Execute($"{result.Key}\t{result.Value}");
    }

    private async Task OpenUpdateTemplateKeyDialog(TemplateRow row)
    {
        var dialog = new CommandEditDialog(
            title: $"改 Key: ${row.Key}",
            label: "新 Key (entry 引用会自动同步)",
            initialValue: row.Key,
            templateKeys: _viewModel.TemplateKeys.Except([row.Key]).ToList());
        var result = await dialog.ShowDialog<string?>(this);
        if (!string.IsNullOrWhiteSpace(result))
            _viewModel.UpdateTemplateKeyCommand.Execute(result);
    }

    private async Task OpenUpdateTemplateValueDialog(TemplateRow row)
    {
        var dialog = new CommandEditDialog(
            title: $"改展开: ${row.Key}",
            label: "展开内容",
            initialValue: row.ExpandedValue,
            templateKeys: Array.Empty<string>(),
            isMultiline: true);
        var result = await dialog.ShowDialog<string?>(this);
        if (!string.IsNullOrWhiteSpace(result))
            _viewModel.UpdateTemplateValueCommand.Execute(result);
    }
}
