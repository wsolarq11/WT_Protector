using Avalonia.Controls;
using Avalonia.Interactivity;
using WTLauncher.App.ViewModels;

namespace WTLauncher.App.Views;

public sealed partial class ConfirmBatchLaunchWindow : Window
{
    public ConfirmBatchLaunchWindow()
    {
        InitializeComponent();
    }

    public ConfirmBatchLaunchWindow(LauncherBatchLaunchConfirmation confirmation)
    {
        ArgumentNullException.ThrowIfNull(confirmation);

        var hasFailures = confirmation.FailedCount > 0;
        InitializeComponent();

        Title = hasFailures ? "预演失败 - 确认一键启动" : "确认一键启动";
        Height = hasFailures ? 640 : 520;
        MinHeight = hasFailures ? 460 : 340;

        TitleText.Text = hasFailures
            ? $"{confirmation.TargetLabel}启动确认（{confirmation.FailedCount} 项预演失败）？"
            : $"{confirmation.TargetLabel}启动确认？";

        var items = confirmation.Items;
        MessageText.Text = hasFailures
            ? $"预演发现 {confirmation.FailedCount} 个失败项。仍有 {items.Count} 个有效{confirmation.TargetLabel}。请确认实际 wt.exe 命令。"
            : $"将启动 {items.Count} 个{confirmation.TargetLabel}。请确认实际 wt.exe 命令。";

        PlansText.Text = LauncherBatchLaunchPlanText.Format(items);

        LaunchButton.Content = hasFailures ? "仍然启动有效项" : "确认启动";
        LaunchButton.MinWidth = hasFailures ? 140 : 116;

        if (hasFailures)
        {
            FailureSection.IsVisible = true;
            var failureLines = string.Join(
                Environment.NewLine,
                confirmation.FailureMessages.Select(f => $"- {f}"));
            FailureText.Text = failureLines;
        }

        Opened += (_, _) => LaunchButton.Focus();
    }

    private void OnCancelClicked(object? sender, RoutedEventArgs e) => Close(false);
    private void OnLaunchClicked(object? sender, RoutedEventArgs e) => Close(true);
}
