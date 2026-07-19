using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using WTLauncher.App.ViewModels;
using WTLauncher.Core;

namespace WTLauncher.App.Views;

public sealed class ConfirmBatchLaunchWindow : Window
{
    private const string MonospaceFontFamily = "Cascadia Code, Consolas, Courier New, monospace";

    public ConfirmBatchLaunchWindow(LauncherBatchLaunchConfirmation confirmation)
    {
        ArgumentNullException.ThrowIfNull(confirmation);

        var hasFailures = confirmation.FailedCount > 0;
        Title = hasFailures ? "预演失败 - 确认一键启动" : "确认一键启动";
        Width = 760;
        Height = hasFailures ? 640 : 520;
        MinWidth = 460;
        MinHeight = hasFailures ? 460 : 340;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = Brush.Parse(LauncherDesignTokens.SurfaceColor);

        var items = confirmation.Items;
        var plans = LauncherBatchLaunchPlanText.Format(items);
        var message = hasFailures
            ? $"预演发现 {confirmation.FailedCount} 个失败项。仍有 {items.Count} 个有效{confirmation.TargetLabel}。请确认实际 wt.exe 命令。"
            : $"将启动 {items.Count} 个{confirmation.TargetLabel}。请确认实际 wt.exe 命令。";
        var title = LauncherDialogText.Title(hasFailures
            ? $"{confirmation.TargetLabel}启动确认（{confirmation.FailedCount} 项预演失败）？"
            : $"{confirmation.TargetLabel}启动确认？");
        var messageText = LauncherDialogText.Body(message);
        messageText.Margin = new Thickness(0, 12, 0, 14);
        var plansText = new TextBox
        {
            Text = plans,
            IsReadOnly = true,
            FontFamily = new FontFamily(MonospaceFontFamily),
            FontSize = 12,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            MinHeight = 220,
        };

        var mainGrid = new Grid
        {
            RowDefinitions = hasFailures
                ? new RowDefinitions("Auto,Auto,*,Auto,Auto")
                : new RowDefinitions("Auto,Auto,*,Auto"),
            Children =
            {
                title,
                messageText.WithGridRow(1),
                new Border
                {
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(10),
                    BorderBrush = Brush.Parse(LauncherDesignTokens.BorderColor),
                    Padding = new Thickness(12),
                    Child = new ScrollViewer
                    {
                        Content = plansText,
                    },
                }.WithGridRow(2),
            },
        };

        int footerRow;
        if (hasFailures)
        {
            var failureLines = string.Join(
                Environment.NewLine,
                confirmation.FailureMessages.Select(f => $"- {f}"));
            var failureHeader = LauncherDialogText.Body("预演失败项:");
            failureHeader.FontWeight = FontWeight.Bold;
            var failureText = new TextBox
            {
                Text = failureLines,
                IsReadOnly = true,
                FontFamily = new FontFamily(MonospaceFontFamily),
                FontSize = 12,
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                MinHeight = 60,
            };
            var failureSection = new StackPanel
            {
                Margin = new Thickness(0, 10, 0, 0),
                Children =
                {
                    failureHeader,
                    new Border
                    {
                        BorderThickness = new Thickness(1),
                        CornerRadius = new CornerRadius(10),
                        BorderBrush = Brush.Parse(LauncherDesignTokens.BorderColor),
                        Padding = new Thickness(12),
                        Child = new ScrollViewer
                        {
                            Content = failureText,
                        },
                    },
                },
            };
            mainGrid.Children.Add(failureSection.WithGridRow(3));
            footerRow = 4;
        }
        else
        {
            footerRow = 3;
        }

        var cancel = LauncherDialogButtons.Cancel(minWidth: 96);
        cancel.Click += (_, _) => Close(false);

        var launch = LauncherDialogButtons.Primary(
            hasFailures ? "仍然启动有效项" : "确认启动",
            minWidth: hasFailures ? 140 : 116);
        launch.Click += (_, _) => Close(true);

        Opened += (_, _) => launch.Focus();

        mainGrid.Children.Add(LauncherDialogButtons.Footer(LauncherDesignTokens.DialogFooterTopMargin, cancel, launch).WithGridRow(footerRow));

        Content = new Border
        {
            Padding = new Thickness(LauncherDesignTokens.DialogOuterPadding),
            Child = mainGrid,
        };
    }
}
