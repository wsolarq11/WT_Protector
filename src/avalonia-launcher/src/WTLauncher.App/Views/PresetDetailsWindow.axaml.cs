using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using WTLauncher.App.ViewModels;
using WTLauncher.Core;

namespace WTLauncher.App.Views;

public sealed partial class PresetDetailsWindow : Window
{
    private readonly IReadOnlySet<string> _existingPresetNames = null!;

    public PresetDetailsWindow()
    {
        InitializeComponent();
    }

    public PresetDetailsWindow(LauncherPresetSaveTarget target, IReadOnlySet<string> existingPresetNames)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(existingPresetNames);

        _existingPresetNames = existingPresetNames;

        InitializeComponent();

        SummaryText.Text = $"将当前 {target.EntryCount} 个勾选项保存为可复用启动组。";

        Opened += (_, _) =>
        {
            NameBox.Focus();
            Dispatcher.UIThread.Post(() => NameBox.SelectAll(), DispatcherPriority.Loaded);
        };
    }

    private void OnCancelClicked(object? sender, RoutedEventArgs e) => Close((string?)null);

    private void OnSaveClicked(object? sender, RoutedEventArgs e) => Submit();

    // ponytail: intentional duplicate of ViewModel validation.
    // This UI-layer check gives instant visual feedback (red error text + focus)
    // before the save-path delegates to ViewModel validation. Keep both.
    private void Submit()
    {
        var name = EntryName.Normalize(NameBox.Text);
        if (name.Length == 0)
        {
            ErrorText.Text = "请输入启动组名称。";
            ErrorText.IsVisible = true;
            NameBox.Focus();
            return;
        }

        if (_existingPresetNames.Contains(name))
        {
            ErrorText.Text = "该名称已存在，请使用其他名称。";
            ErrorText.IsVisible = true;
            NameBox.Focus();
            return;
        }

        Close(name);
    }
}
