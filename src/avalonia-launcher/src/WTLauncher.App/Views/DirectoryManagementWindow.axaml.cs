using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using WTLauncher.App.Infrastructure;
using WTLauncher.App.ViewModels;
using WTLauncher.Core;

namespace WTLauncher.App.Views;

public sealed partial class DirectoryManagementWindow : Window
{
    private readonly DirectoryManagementViewModel _viewModel = null!;

    public DirectoryManagementWindow()
    {
        InitializeComponent();
    }

    public DirectoryManagementWindow(DirectoryManagementViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        _viewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();
    }

    private void OnCloseClicked(object? sender, RoutedEventArgs e) => Close(null);

    private async void OnRenameClicked(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (_viewModel.SelectedDirectoryRow is { } row)
                await OpenRenameDialog(row);
        }
        catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
        {
            ErrorLog.Write(ex, "DirectoryManagement.Rename");
        }
    }

    private async void OnMergeClicked(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (_viewModel.SelectedDirectoryRow is { } row)
                await OpenMergeDialog(row);
        }
        catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
        {
            ErrorLog.Write(ex, "DirectoryManagement.Merge");
        }
    }

    private async void OnRowDoubleTapped(object? sender, TappedEventArgs e)
    {
        try
        {
            if (sender is Control { DataContext: DirectoryRow row })
            {
                _viewModel.SelectedDirectoryRow = row;
                await OpenRenameDialog(row);
            }
        }
        catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
        {
            ErrorLog.Write(ex, "DirectoryManagement.DoubleTap");
        }
    }

    private async Task OpenRenameDialog(DirectoryRow row)
    {
        var dialog = new DirectoryEditDialog(
            title: $"编辑地址: {row.Directory}",
            label: "新地址 (支持手输或浏览)",
            initialValue: row.Directory,
            affectedEntryCount: row.ReferenceCount);
        var result = await dialog.ShowDialog<string?>(this);
        if (!string.IsNullOrWhiteSpace(result))
            _viewModel.RenameDirectoryCommand.Execute(result);
    }

    private async Task OpenMergeDialog(DirectoryRow row)
    {
        var dialog = new DirectoryEditDialog(
            title: $"合并到...: {row.Directory}",
            label: "保留方地址 (输入或浏览)",
            initialValue: string.Empty,
            affectedEntryCount: row.ReferenceCount);
        var result = await dialog.ShowDialog<string?>(this);
        if (!string.IsNullOrWhiteSpace(result))
            _viewModel.MergeDirectoriesCommand.Execute(result);
    }
}
