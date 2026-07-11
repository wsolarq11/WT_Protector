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
/// 命令管理 modal。纯 C# code-behind,与 DiscoverAgentsWindow 同模式。
/// Tab 1 = 命令引用,Tab 2 = 命令模板。双击编辑,引用 &gt; 0 时禁止删除。
/// ponytail: 无 DataGrid 依赖,用 StackPanel 行渲染;无 XAML,与现有 dialog 一致。
/// </summary>
public sealed class CommandManagementWindow : Window
{
    private const string MonospaceFontFamily = "Cascadia Code, Consolas, Courier New, monospace";

    private readonly CommandManagementViewModel _viewModel;
    private readonly StackPanel _commandRowsPanel;
    private readonly StackPanel _templateRowsPanel;
    private readonly Panel _commandArea;
    private readonly Panel _templateArea;
    private readonly Button _tabCommandButton;
    private readonly Button _tabTemplateButton;
    private readonly TextBlock _statusText;
    private readonly TextBlock _commandEmpty;
    private readonly TextBlock _templateEmpty;

    public CommandManagementWindow(CommandManagementViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        _viewModel = viewModel;

        Title = "🛠 命令管理";
        Width = 820;
        Height = 600;
        MinWidth = 560;
        MinHeight = 420;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = Brush.Parse(LauncherDesignTokens.SurfaceColor);

        // — Rows renderers —
        _commandRowsPanel = new StackPanel { Spacing = 6 };
        _templateRowsPanel = new StackPanel { Spacing = 6 };
        var commandScroll = new ScrollViewer
        {
            Content = _commandRowsPanel,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
        };
        var templateScroll = new ScrollViewer
        {
            Content = _templateRowsPanel,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
        };
        _commandEmpty = EmptyHint("(无 command — 还没有任何启动按钮)");
        _templateEmpty = EmptyHint("(无模板 — 添加一个模板来批量定义命令参数)");
        _commandArea = LayeredArea(commandScroll, _commandEmpty);
        _templateArea = LayeredArea(templateScroll, _templateEmpty);

        // — Tab bar —
        _tabCommandButton = new Button
        {
            Content = "📋 命令引用",
            MinWidth = 130,
            Classes = { "secondary" },
        };
        _tabCommandButton.Click += (_, _) => _viewModel.ActiveTab = 0;
        _tabTemplateButton = new Button
        {
            Content = "📑 命令模板",
            MinWidth = 130,
            Classes = { "secondary" },
        };
        _tabTemplateButton.Click += (_, _) => _viewModel.ActiveTab = 1;

        // — Tool bar (per-tab) —
        var commandTools = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6,
            Margin = new Thickness(0, 6, 0, 0),
            Children =
            {
                ToolButton("✏ 编辑/重命名", () =>
                {
                    if (_viewModel.SelectedCommandRow is { } r) _ = OpenRenameCommandDialog(r);
                }),
                ToolButton("🗑 删除", () => _viewModel.DeleteCommandCommand.Execute(null)),
            },
        };
        var templateTools = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6,
            Margin = new Thickness(0, 6, 0, 0),
            Children =
            {
                ToolButton("➕ 新模板", () => _ = OpenAddTemplateDialog()),
                ToolButton("✏ 改 Key", () =>
                {
                    if (_viewModel.SelectedTemplateRow is { } r) _ = OpenUpdateTemplateKeyDialog(r);
                }),
                ToolButton("📝 改展开内容", () =>
                {
                    if (_viewModel.SelectedTemplateRow is { } r) _ = OpenUpdateTemplateValueDialog(r);
                }),
                ToolButton("🗑 删除", () => _viewModel.DeleteTemplateCommand.Execute(null)),
            },
        };

        _statusText = new TextBlock
        {
            FontSize = 12,
            Foreground = Brush.Parse(LauncherDesignTokens.TextMutedColor),
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(12, 6, 0, 0),
        };

        var tabBar = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6,
            Children = { _tabCommandButton, _tabTemplateButton, _statusText },
        };

        var toolBar = new Grid
        {
            RowDefinitions = new RowDefinitions("Auto,Auto"),
            Margin = new Thickness(0, 0, 0, 8),
            Children =
            {
                tabBar.WithGridRow(0),
                commandTools.WithGridRow(1),
                templateTools.WithGridRow(1),
            },
        };

        // — Title + footer —
        var title = LauncherDialogText.Title("命令与模板集中管理");
        var subtitle = LauncherDialogText.Body(
            "Tab 1 列出每个 command 字段的引用次数 — 双击改命令,自动同步所有引用 entry。\n" +
            "Tab 2 列出 commandTemplates 字典 — 改 key 自动同步 $key 引用,改展开内容立即影响所有引用 entry。");
        subtitle.Margin = new Thickness(0, 4, 0, 12);

        var closeButton = LauncherDialogButtons.Primary("关闭", minWidth: 110);
        closeButton.Click += (_, _) => Close(null);
        var footer = LauncherDialogButtons.Footer(12, closeButton);

        var rowsBorder = new Border
        {
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            BorderBrush = Brush.Parse(LauncherDesignTokens.BorderColor),
            Padding = new Thickness(8),
            Child = new Panel
            {
                Children = { _commandArea, _templateArea },
            },
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
            Padding = new Thickness(22),
            Child = grid,
        };

        // — Hook VM events —
        _viewModel.PropertyChanged += HandleVmPropertyChanged;
        _viewModel.CommandRows.CollectionChanged += (_, _) => RenderCommandRows();
        _viewModel.TemplateRows.CollectionChanged += (_, _) => RenderTemplateRows();
        RenderCommandRows();
        RenderTemplateRows();
        SyncTab();
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

    private static TextBlock EmptyHint(string text) => new()
    {
        Text = text,
        FontSize = 12,
        Foreground = Brush.Parse(LauncherDesignTokens.TextMutedColor),
        Margin = new Thickness(8, 12, 8, 12),
    };

    private static Panel LayeredArea(Control scroll, Control empty)
    {
        var panel = new Panel();
        panel.Children.Add(scroll);
        panel.Children.Add(empty);
        return panel;
    }

    private void RenderCommandRows()
    {
        _commandRowsPanel.Children.Clear();
        _commandEmpty.IsVisible = _viewModel.CommandRows.Count == 0;
        foreach (var row in _viewModel.CommandRows)
            _commandRowsPanel.Children.Add(BuildCommandRow(row));
    }

    private void RenderTemplateRows()
    {
        _templateRowsPanel.Children.Clear();
        _templateEmpty.IsVisible = _viewModel.TemplateRows.Count == 0;
        foreach (var row in _viewModel.TemplateRows)
            _templateRowsPanel.Children.Add(BuildTemplateRow(row));
    }

    private Border BuildCommandRow(CommandRow row)
    {
        var isSelected = ReferenceEquals(_viewModel.SelectedCommandRow, row);
        var commandBlock = new TextBlock
        {
            Text = row.Command,
            FontFamily = new FontFamily(MonospaceFontFamily),
            FontSize = 14,
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

        var rowsLayout = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,8,*,8,Auto"),
            Children =
            {
                commandBlock.WithGridColumn(0),
                namesBlock.WithGridColumn(2),
                refCountBlock.WithGridColumn(4),
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
            _viewModel.SelectedCommandRow = row;
            RenderCommandRows();
        };
        border.DoubleTapped += async (_, _) => await OpenRenameCommandDialog(row);
        return border;
    }

    private Border BuildTemplateRow(TemplateRow row)
    {
        var isSelected = ReferenceEquals(_viewModel.SelectedTemplateRow, row);
        var keyBlock = new TextBlock
        {
            Text = $"${row.Key}",
            FontFamily = new FontFamily(MonospaceFontFamily),
            FontSize = 14,
            FontWeight = FontWeight.SemiBold,
        };
        var refBlock = new TextBlock
        {
            Text = $"引用 {row.ReferenceCount}",
            FontSize = 12,
            Foreground = Brush.Parse(LauncherDesignTokens.TextMutedColor),
        };
        var valueBlock = new TextBlock
        {
            Text = row.ExpandedValue,
            FontFamily = new FontFamily(MonospaceFontFamily),
            FontSize = 11,
            Foreground = Brush.Parse(LauncherDesignTokens.TextMutedColor),
            TextTrimming = TextTrimming.CharacterEllipsis,
        };

        var rowsLayout = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,8,*,8,Auto"),
            Children =
            {
                keyBlock.WithGridColumn(0),
                valueBlock.WithGridColumn(2),
                refBlock.WithGridColumn(4),
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
            _viewModel.SelectedTemplateRow = row;
            RenderTemplateRows();
        };
        border.DoubleTapped += async (_, _) => await OpenUpdateTemplateValueDialog(row);
        return border;
    }

    private void HandleVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(CommandManagementViewModel.ActiveTab):
                SyncTab();
                break;
            case nameof(CommandManagementViewModel.StatusMessage):
                _statusText.Text = _viewModel.StatusMessage;
                break;
            case nameof(CommandManagementViewModel.SelectedCommandRow):
                RenderCommandRows();
                break;
            case nameof(CommandManagementViewModel.SelectedTemplateRow):
                RenderTemplateRows();
                break;
        }
    }

    private void SyncTab()
    {
        var onCommands = _viewModel.ActiveTab == 0;
        _commandArea.IsVisible = onCommands;
        _templateArea.IsVisible = !onCommands;
        _tabCommandButton.FontWeight = onCommands ? FontWeight.SemiBold : FontWeight.Normal;
        _tabTemplateButton.FontWeight = !onCommands ? FontWeight.SemiBold : FontWeight.Normal;
    }

    // — Edit sub-dialogs ———————————————————————————

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
        var kv = await dialog.ShowDialog<(string Key, string Value)>(this);
        if (!string.IsNullOrWhiteSpace(kv.Key))
            _viewModel.AddTemplateCommand.Execute($"{kv.Key}\t{kv.Value}");
    }

    private async Task OpenUpdateTemplateKeyDialog(TemplateRow row)
    {
        var dialog = new CommandEditDialog(
            title: $"改 Key: ${row.Key}",
            label: "新 Key (entry 引用会自动同步)",
            initialValue: row.Key,
            templateKeys: _viewModel.TemplateKeys.Except(new[] { row.Key }).ToList());
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
