using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using WTLauncher.App.Infrastructure;
using WTLauncher.App.Localization;
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
        try
        {
            if (_viewModel.SelectedCommandRow is { } row)
                await OpenRenameCommandDialog(row);
        }
        catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
        {
            ErrorLog.Write(ex, "CommandManagement.RenameCommand");
        }
    }

    private async void OnAddTemplateClicked(object? sender, RoutedEventArgs e)
    {
        try { await OpenAddTemplateDialog(); }
        catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
        {
            ErrorLog.Write(ex, "CommandManagement.AddTemplate");
        }
    }

    private async void OnUpdateTemplateKeyClicked(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (_viewModel.SelectedTemplateRow is { } row)
                await OpenUpdateTemplateKeyDialog(row);
        }
        catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
        {
            ErrorLog.Write(ex, "CommandManagement.UpdateKey");
        }
    }

    private async void OnUpdateTemplateValueClicked(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (_viewModel.SelectedTemplateRow is { } row)
                await OpenUpdateTemplateValueDialog(row);
        }
        catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
        {
            ErrorLog.Write(ex, "CommandManagement.UpdateValue");
        }
    }

    private async void OnCommandRowDoubleTapped(object? sender, TappedEventArgs e)
    {
        try
        {
            if (sender is Control { DataContext: CommandRow row })
            {
                _viewModel.SelectedCommandRow = row;
                await OpenRenameCommandDialog(row);
            }
        }
        catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
        {
            ErrorLog.Write(ex, "CommandManagement.CommandRowDoubleTap");
        }
    }

    private async void OnTemplateRowDoubleTapped(object? sender, TappedEventArgs e)
    {
        try
        {
            if (sender is Control { DataContext: TemplateRow row })
            {
                _viewModel.SelectedTemplateRow = row;
                await OpenUpdateTemplateValueDialog(row);
            }
        }
        catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
        {
            ErrorLog.Write(ex, "CommandManagement.TemplateRowDoubleTap");
        }
    }

    private async Task OpenRenameCommandDialog(CommandRow row)
    {
        var dialog = new CommandEditDialog(
            title: LauncherLocalization.Format("cmdmgmt.editCommandTitle", row.Command),
            label: LauncherLocalization.Format("cmdmgmt.editCommandLabel"),
            initialValue: row.Command,
            templateKeys: _viewModel.TemplateKeys);
        var result = await dialog.ShowDialog<string?>(this);
        if (!string.IsNullOrWhiteSpace(result))
            _viewModel.RenameCommandCommand.Execute(result);
    }

    private async Task OpenAddTemplateDialog()
    {
        var dialog = new CommandEditDialog(
            title: LauncherLocalization.Format("cmdmgmt.newTemplateTitle"),
            label: LauncherLocalization.Format("cmdmgmt.templateKeyLabel"),
            initialValue: string.Empty,
            templateKeys: Array.Empty<string>(),
            secondLabel: LauncherLocalization.Format("cmdmgmt.templateValueLabel"));
        var result = await dialog.ShowDialog<(string Key, string Value)>(this);
        if (!string.IsNullOrWhiteSpace(result.Key))
            _viewModel.AddTemplateCommand.Execute($"{result.Key}\t{result.Value}");
    }

    private async Task OpenUpdateTemplateKeyDialog(TemplateRow row)
    {
        var dialog = new CommandEditDialog(
            title: LauncherLocalization.Format("cmdmgmt.updateKeyTitle", row.Key),
            label: LauncherLocalization.Format("cmdmgmt.updateKeyLabel"),
            initialValue: row.Key,
            templateKeys: _viewModel.TemplateKeys.Except([row.Key]).ToList());
        var result = await dialog.ShowDialog<string?>(this);
        if (!string.IsNullOrWhiteSpace(result))
            _viewModel.UpdateTemplateKeyCommand.Execute(result);
    }

    private async Task OpenUpdateTemplateValueDialog(TemplateRow row)
    {
        var dialog = new CommandEditDialog(
            title: LauncherLocalization.Format("cmdmgmt.updateValueTitle", row.Key),
            label: LauncherLocalization.Format("cmdmgmt.updateValueLabel"),
            initialValue: row.ExpandedValue,
            templateKeys: Array.Empty<string>(),
            isMultiline: true);
        var result = await dialog.ShowDialog<string?>(this);
        if (!string.IsNullOrWhiteSpace(result))
            _viewModel.UpdateTemplateValueCommand.Execute(result);
    }
}
