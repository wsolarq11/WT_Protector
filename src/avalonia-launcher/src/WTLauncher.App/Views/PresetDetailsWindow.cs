using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using WTLauncher.App.ViewModels;
using WTLauncher.Core;

namespace WTLauncher.App.Views;

public sealed class PresetDetailsWindow : Window
{
    private readonly TextBox _nameBox;
    private readonly TextBlock _errorText;

    public PresetDetailsWindow(LauncherPresetSaveTarget target)
    {
        ArgumentNullException.ThrowIfNull(target);

        Title = "保存为启动组";
        Width = 560;
        Height = 260;
        MinWidth = 480;
        MinHeight = 220;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = Brushes.White;

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
            _nameBox.SelectAll();
            _nameBox.Focus();
        };
    }

    private void Submit()
    {
        if (!LauncherPresetNames.TryNormalizeRequired(_nameBox.Text, out var name))
        {
            _errorText.Text = "请输入启动组名称。";
            _errorText.IsVisible = true;
            _nameBox.Focus();
            return;
        }

        Close(name);
    }
}
