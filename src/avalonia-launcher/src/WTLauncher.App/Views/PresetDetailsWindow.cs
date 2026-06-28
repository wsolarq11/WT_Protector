using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using WTLauncher.App.ViewModels;
using WTLauncher.Core;

namespace WTLauncher.App.Views;

public sealed class PresetDetailsWindow : Window
{
    private readonly TextBox _nameBox;
    private readonly TextBlock _errorText;
    private readonly IReadOnlySet<string> _existingPresetNames;

    public PresetDetailsWindow(LauncherPresetSaveTarget target, IReadOnlySet<string> existingPresetNames)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(existingPresetNames);

        _existingPresetNames = existingPresetNames;

        Title = "保存为启动组";
        Width = 560;
        Height = 260;
        MinWidth = 480;
        MinHeight = 220;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = Brush.Parse(LauncherDesignTokens.SurfaceColor);

        _nameBox = LauncherDialogText.Input("新启动组");

        _errorText = LauncherDialogText.Error();

        var summary = LauncherDialogText.Body($"将当前 {target.EntryCount} 个勾选项保存为可复用启动组。");

        var cancel = LauncherDialogButtons.Cancel();
        cancel.Click += (_, _) => Close((string?)null);

        var save = LauncherDialogButtons.Primary("保存启动组");
        save.Click += (_, _) => Submit();

        var footer = LauncherDialogButtons.Footer(14, cancel, save);

        Content = new Border
        {
            Padding = new Thickness(24),
            Child = new Grid
            {
                RowDefinitions = new RowDefinitions("Auto,Auto,Auto,Auto,Auto,Auto"),
                RowSpacing = 12,
                Children =
                {
                    LauncherDialogText.Title("保存当前勾选为新启动组"),
                    summary.WithGridRow(1),
                    LauncherDialogText.Label("名称").WithGridRow(2),
                    _nameBox.WithGridRow(3),
                    _errorText.WithGridRow(4),
                    footer.WithGridRow(5),
                },
            },
        };

        Opened += (_, _) =>
        {
            _nameBox.Focus();
            Dispatcher.UIThread.Post(() => _nameBox.SelectAll(), DispatcherPriority.Loaded);
        };
    }

    private void Submit()
    {
        var name = LauncherEntryText.Normalize(_nameBox.Text);
        if (name.Length == 0)
        {
            _errorText.Text = "请输入启动组名称。";
            _errorText.IsVisible = true;
            _nameBox.Focus();
            return;
        }

        if (_existingPresetNames.Contains(name))
        {
            _errorText.Text = "该名称已存在，请使用其他名称。";
            _errorText.IsVisible = true;
            _nameBox.Focus();
            return;
        }

        Close(name);
    }
}
