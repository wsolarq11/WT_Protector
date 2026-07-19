using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using WTLauncher.App.ViewModels;
using WTLauncher.Core;

namespace WTLauncher.App.Views;

/// <summary>
/// 地址管理 modal。纯 C# code-behind,与 CommandManagementWindow 同模式。
/// 单 tab: 去重 directory + 引用次数。双击编辑,引用 &gt; 0 时禁止删除。
/// ponytail: 无 DataGrid 依赖,用 StackPanel 行渲染。
/// </summary>
public sealed class DirectoryManagementWindow : Window
{
    private const string MonospaceFontFamily = "Cascadia Code, Consolas, Courier New, monospace";

    private readonly DirectoryManagementViewModel _viewModel;
    private readonly StackPanel _rowsPanel;
    private readonly TextBlock _statusText;
    private readonly TextBlock _emptyHint;

    public DirectoryManagementWindow(DirectoryManagementViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        _viewModel = viewModel;

        Title = "📁 地址管理";
        Width = 820;
        Height = 600;
        MinWidth = 560;
        MinHeight = 420;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = Brush.Parse(LauncherDesignTokens.SurfaceColor);

        _rowsPanel = new StackPanel { Spacing = 6 };
        var scroll = new ScrollViewer
        {
            Content = _rowsPanel,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
        };
        _emptyHint = new TextBlock
        {
            Text = "(无 directory — 还没有任何启动按钮)",
            FontSize = 12,
            Foreground = Brush.Parse(LauncherDesignTokens.TextMutedColor),
            Margin = new Thickness(8, 12, 8, 12),
        };

        var rowsPanel = new Panel();
        rowsPanel.Children.Add(scroll);
        rowsPanel.Children.Add(_emptyHint);

        var title = LauncherDialogText.Title("地址集中管理");
        var subtitle = LauncherDialogText.Body(
            "列出每个 directory 的引用次数 — 双击改地址(可浏览),自动同步所有引用 entry。\n" +
            "合并两个地址: 失效方 entry.directory 全部改成保留方。");
        subtitle.Margin = new Thickness(0, 4, 0, 12);

        var renameBtn = ToolButton("✏ 编辑/重命名", async () =>
        {
            if (_viewModel.SelectedDirectoryRow is { } r) await OpenRenameDialog(r);
        });
        var mergeBtn = ToolButton("🔀 合并到...", async () =>
        {
            if (_viewModel.SelectedDirectoryRow is { } r) await OpenMergeDialog(r);
        });
        var delBtn = ToolButton("🗑 删除", () => _viewModel.DeleteDirectoryCommand.Execute(null));

        _statusText = new TextBlock
        {
            FontSize = 12,
            Foreground = Brush.Parse(LauncherDesignTokens.TextMutedColor),
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(12, 6, 0, 0),
        };

        var toolBar = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6,
            Margin = new Thickness(0, 0, 0, 8),
            Children = { renameBtn, mergeBtn, delBtn, _statusText },
        };

        var closeButton = LauncherDialogButtons.Primary("关闭", minWidth: 110);
        closeButton.Click += (_, _) => Close(null);
        var footer = LauncherDialogButtons.Footer(LauncherDesignTokens.DialogFooterTopMargin, closeButton);

        var rowsBorder = new Border
        {
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            BorderBrush = Brush.Parse(LauncherDesignTokens.BorderColor),
            Padding = new Thickness(8),
            Child = rowsPanel,
        };

        var grid = new Grid
        {
            RowDefinitions = new RowDefinitions("Auto,Auto,Auto,*,Auto"),
            Children =
            {
                title.WithGridRow(0),
                subtitle.WithGridRow(1),
                toolBar.WithGridRow(2),
                rowsBorder.WithGridRow(3),
                footer.WithGridRow(4),
            },
        };

        Content = new Border
        {
            Padding = new Thickness(LauncherDesignTokens.DialogOuterPadding),
            Child = grid,
        };

        _viewModel.PropertyChanged += HandleVmPropertyChanged;
        _viewModel.DirectoryRows.CollectionChanged += (_, _) => RenderRows();
        RenderRows();
        _statusText.Text = _viewModel.StatusMessage;
    }

    private static Button ToolButton(string content, Action onClick)
    {
        var b = new Button
        {
            Content = content,
            Classes = { "secondary" },
            MinWidth = 90,
        };
        b.Click += (_, _) => onClick();
        return b;
    }

    private void RenderRows()
    {
        _rowsPanel.Children.Clear();
        _emptyHint.IsVisible = _viewModel.DirectoryRows.Count == 0;
        foreach (var row in _viewModel.DirectoryRows)
            _rowsPanel.Children.Add(BuildRow(row));
    }

    private Border BuildRow(DirectoryRow row)
    {
        var isSelected = ReferenceEquals(_viewModel.SelectedDirectoryRow, row);
        var dirBlock = new TextBlock
        {
            Text = row.Directory,
            FontFamily = new FontFamily(MonospaceFontFamily),
            FontSize = 13,
            FontWeight = FontWeight.SemiBold,
            TextTrimming = TextTrimming.CharacterEllipsis,
        };
        var refCountBlock = new TextBlock
        {
            Text = $"引用 {row.ReferenceCount}",
            FontSize = 12,
            Foreground = Brush.Parse(LauncherDesignTokens.TextMutedColor),
        };
        var namesText = row.ReferencingEntryNames.Count == 0
            ? "(无引用 — 安全删除)"
            : string.Join(", ", row.ReferencingEntryNames);
        var namesBlock = new TextBlock
        {
            Text = namesText,
            FontSize = 11,
            Foreground = Brush.Parse(LauncherDesignTokens.TextMutedColor),
            TextTrimming = TextTrimming.CharacterEllipsis,
        };

        var layout = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,8,Auto"),
            Children =
            {
                dirBlock.WithGridColumn(0),
                namesBlock.WithGridColumn(0),
                refCountBlock.WithGridColumn(2),
            },
        };
        // 上面 Grid 在 dirBlock 和 namesBlock 都用 col 0 — 用 StackPanel 替代避免重叠。
        var rowsLayout = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,8,Auto"),
            Children =
            {
                new StackPanel
                {
                    Spacing = 2,
                    Children = { dirBlock, namesBlock },
                }.WithGridColumn(0),
                refCountBlock.WithGridColumn(2),
            },
        };

        var border = new Border
        {
            Background = Brush.Parse(LauncherDesignTokens.SurfaceColor),
            BorderThickness = new Thickness(1),
            BorderBrush = Brush.Parse(isSelected
                ? LauncherDesignTokens.PrimaryColor
                : LauncherDesignTokens.BorderColor),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(10, 6),
            Child = rowsLayout,
        };
        border.PointerPressed += (_, _) =>
        {
            _viewModel.SelectedDirectoryRow = row;
            RenderRows();
        };
        border.DoubleTapped += async (_, _) => await OpenRenameDialog(row);
        return border;
    }

    private void HandleVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(DirectoryManagementViewModel.StatusMessage):
                _statusText.Text = _viewModel.StatusMessage;
                break;
            case nameof(DirectoryManagementViewModel.SelectedDirectoryRow):
                RenderRows();
                break;
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
