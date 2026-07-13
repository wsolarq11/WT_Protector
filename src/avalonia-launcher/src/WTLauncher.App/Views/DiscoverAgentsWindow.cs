using System.ComponentModel;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using WTLauncher.App.ViewModels;
using WTLauncher.Core;

namespace WTLauncher.App.Views;

/// <summary>
/// Modal dialog for AI Agent CLI discovery. Pattern matches
/// ConfirmBatchLaunchWindow.cs: pure C# code-behind, no XAML. The dialog
/// owns no business decisions — it renders the supplied DiscoverAgentsViewModel
/// and returns the user's selected DiscoveredAgent list via Close().
///
/// ponytail: bindings done via direct PropertyChanged subscriptions rather
/// than WhenAnyValue/ReactiveUI — the project does not use ReactiveUI and
/// adding it just for this dialog would be a dependency regression.
/// </summary>
public sealed class DiscoverAgentsWindow : Window
{
    private const string MonospaceFontFamily = "Cascadia Code, Consolas, Courier New, monospace";
    private readonly DiscoverAgentsViewModel _viewModel;

    private TextBlock _statusText = null!;
    private Button _scanButton = null!;
    private Button _addButton = null!;
    private StackPanel _rowsPanel = null!;
    private Control _pathPanel = null!;
    private Control _directoryPanel = null!;
    private TextBlock _commandPreview = null!;

    public DiscoverAgentsWindow(DiscoverAgentsViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        _viewModel = viewModel;

        Title = "🔍 发现已安装的 AI Agent";
        Width = 780;
        Height = 560;
        MinWidth = 520;
        MinHeight = 380;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = Brush.Parse(LauncherDesignTokens.SurfaceColor);

        BuildUi();

        _viewModel.PropertyChanged += HandleViewModelPropertyChanged;
        _viewModel.Rows.CollectionChanged += (_, _) => RenderRows();
        RenderRows();
        SyncUiFromViewModel();
    }

    private void BuildUi()
    {
        var title = LauncherDialogText.Title("发现并添加 AI Agent CLI 启动项");
        var subtitle = LauncherDialogText.Body(
            "扫描 PATH 上的 AI Agent CLI，或复用已有目录和命令模板快速创建启动项。");
        subtitle.Margin = new Thickness(0, 4, 0, 12);

        var pathMode = new RadioButton { Content = "发现 AI Agent", GroupName = "DiscoveryMode", IsChecked = true };
        var directoryMode = new RadioButton { Content = "从已有目录创建", GroupName = "DiscoveryMode" };
        pathMode.IsCheckedChanged += (_, _) =>
        {
            if (pathMode.IsChecked == true)
                _viewModel.Mode = DiscoveryMode.PathScan;
        };
        directoryMode.IsCheckedChanged += (_, _) =>
        {
            if (directoryMode.IsChecked == true)
                _viewModel.Mode = DiscoveryMode.ExistingDirectory;
        };
        var modePicker = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 18,
            Margin = new Thickness(0, 0, 0, 10),
            Children = { pathMode, directoryMode },
        };

        _statusText = new TextBlock
        {
            Text = "等待扫描",
            FontSize = 12,
            Foreground = Brush.Parse(LauncherDesignTokens.TextMutedColor),
            VerticalAlignment = VerticalAlignment.Center,
        };

        _scanButton = new Button
        {
            Content = "重新扫描",
            MinWidth = 96,
            Classes = { "secondary" },
        };
        _scanButton.Click += async (_, _) => await _viewModel.ScanAsync();

        var headerRow = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto"),
            Children =
            {
                _statusText.WithGridColumn(0),
                _scanButton.WithGridColumn(1),
            },
        };

        var rowsPanel = new StackPanel { Spacing = 6 };
        _rowsPanel = rowsPanel;
        var rowsScroll = new ScrollViewer
        {
            Content = rowsPanel,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
        };

        _pathPanel = new Grid
        {
            RowDefinitions = new RowDefinitions("Auto,*"),
            Children =
            {
                headerRow.WithGridRow(0),
                new Border
                {
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(10),
                    BorderBrush = Brush.Parse(LauncherDesignTokens.BorderColor),
                    Padding = new Thickness(8),
                    Margin = new Thickness(0, 10, 0, 0),
                    Child = rowsScroll,
                }.WithGridRow(1),
            },
        };
        _directoryPanel = BuildDirectoryPanel();

        var cancelButton = LauncherDialogButtons.Cancel(minWidth: 96);
        cancelButton.Click += (_, _) => Close(null);

        _addButton = LauncherDialogButtons.Primary("添加选中", minWidth: 132);
        _addButton.Click += (_, _) => Close(_viewModel.GetResult());

        var footer = LauncherDialogButtons.Footer(18, cancelButton, _addButton);

        var grid = new Grid
        {
            RowDefinitions = new RowDefinitions("Auto,Auto,Auto,*,Auto"),
            Children =
            {
                title.WithGridRow(0),
                subtitle.WithGridRow(1),
                modePicker.WithGridRow(2),
                new Grid
                {
                    Children = { _pathPanel, _directoryPanel },
                }.WithGridRow(3),
                footer.WithGridRow(4),
            },
        };

        Content = new Border
        {
            Padding = new Thickness(22),
            Child = grid,
        };

        Opened += async (_, _) =>
        {
            _scanButton.Focus();
            await _viewModel.ScanAsync();
        };
    }

    private void HandleViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(DiscoverAgentsViewModel.StatusText):
                _statusText.Text = _viewModel.StatusText;
                break;
            case nameof(DiscoverAgentsViewModel.IsScanning):
                _scanButton.IsEnabled = !_viewModel.IsScanning;
                break;
            case nameof(DiscoverAgentsViewModel.SelectedCount):
            case nameof(DiscoverAgentsViewModel.CanAddSelected):
            case nameof(DiscoverAgentsViewModel.CanConfirm):
            case nameof(DiscoverAgentsViewModel.Mode):
            case nameof(DiscoverAgentsViewModel.CommandPreview):
                SyncUiFromViewModel();
                break;
        }
    }

    private void SyncUiFromViewModel()
    {
        _statusText.Text = _viewModel.StatusText;
        _scanButton.IsEnabled = !_viewModel.IsScanning;
        _pathPanel.IsVisible = _viewModel.IsPathScanMode;
        _directoryPanel.IsVisible = _viewModel.IsExistingDirectoryMode;
        _addButton.IsEnabled = _viewModel.CanConfirm;
        _addButton.Content = _viewModel.IsExistingDirectoryMode
            ? "创建启动项"
            : _viewModel.SelectedCount == 0 ? "添加选中" : $"添加选中 ({_viewModel.SelectedCount})";
        _commandPreview.Text = _viewModel.CommandPreview;
    }

    private Control BuildDirectoryPanel()
    {
        var directories = new StackPanel { Spacing = 6 };
        foreach (var row in _viewModel.ExistingDirectories)
        {
            var radio = new RadioButton
            {
                GroupName = "ExistingDirectory",
                Content = $"{row.Directory}  ·  被 {row.ReferenceCount} 个启动项使用",
            };
            radio.IsCheckedChanged += (_, _) =>
            {
                if (radio.IsChecked == true)
                    _viewModel.SelectedDirectory = row;
            };
            directories.Children.Add(radio);
        }

        var commandPicker = new ComboBox
        {
            ItemsSource = _viewModel.CommandTemplateKeys,
            SelectedItem = _viewModel.SelectedCommandTemplateKey,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        commandPicker.SelectionChanged += (_, _) =>
            _viewModel.SelectedCommandTemplateKey = commandPicker.SelectedItem as string;

        var entryName = new TextBox { Watermark = "启动项名称" };
        entryName.TextChanged += (_, _) => _viewModel.EntryName = entryName.Text ?? string.Empty;
        _viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(DiscoverAgentsViewModel.EntryName) && entryName.Text != _viewModel.EntryName)
                entryName.Text = _viewModel.EntryName;
        };

        _commandPreview = new TextBlock
        {
            FontFamily = new FontFamily(MonospaceFontFamily),
            FontSize = 12,
            Foreground = Brush.Parse(LauncherDesignTokens.TextMutedColor),
            TextWrapping = TextWrapping.Wrap,
        };

        return new StackPanel
        {
            Spacing = 10,
            Children =
            {
                LauncherDialogText.Body("选择一个已在使用的目录："),
                new Border
                {
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(10),
                    BorderBrush = Brush.Parse(LauncherDesignTokens.BorderColor),
                    Padding = new Thickness(10),
                    Child = new ScrollViewer { Content = directories, MaxHeight = 220 },
                },
                LauncherDialogText.Body("命令模板"),
                commandPicker,
                LauncherDialogText.Body("启动项名称"),
                entryName,
                LauncherDialogText.Body("命令预览"),
                new Border
                {
                    Background = Brush.Parse(LauncherDesignTokens.BackgroundColor),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(10),
                    Child = _commandPreview,
                },
            },
        };
    }

    private void RenderRows()
    {
        var panel = _rowsPanel;
        panel.Children.Clear();

        if (_viewModel.Rows.Count == 0)
        {
            panel.Children.Add(new TextBlock
            {
                Text = "（暂无发现项 — 点击「重新扫描」）",
                FontSize = 12,
                Foreground = Brush.Parse(LauncherDesignTokens.TextMutedColor),
                Margin = new Thickness(8, 12, 8, 12),
            });
            return;
        }

        foreach (var row in _viewModel.Rows)
        {
            panel.Children.Add(BuildRowCard(row));
        }
    }

    private Border BuildRowCard(DiscoveredAgentRow row)
    {
        var checkbox = new CheckBox
        {
            VerticalAlignment = VerticalAlignment.Center,
            IsEnabled = !row.IsAlreadyAdded,
            IsChecked = row.IsSelected,
        };
        checkbox.IsCheckedChanged += (_, _) =>
        {
            row.IsSelected = checkbox.IsChecked == true;
            _viewModel.RecomputeSelectedCount();
        };

        row.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(DiscoveredAgentRow.IsSelected))
                checkbox.IsChecked = row.IsSelected;
        };

        var nameBlock = new TextBlock
        {
            Text = row.DisplayName,
            FontSize = 14,
            FontWeight = FontWeight.SemiBold,
            Foreground = Brush.Parse(row.IsAlreadyAdded
                ? LauncherDesignTokens.TextMutedColor
                : LauncherDesignTokens.TextPrimaryColor),
        };

        var brandBadge = new Border
        {
            Width = LauncherDesignTokens.BrandBadgeSize,
            Height = LauncherDesignTokens.BrandBadgeSize,
            CornerRadius = new CornerRadius(LauncherDesignTokens.BrandBadgeCornerRadius),
            Background = Brush.Parse(row.BrandColorHex),
            IsVisible = row.HasBrand,
            VerticalAlignment = VerticalAlignment.Center,
            Child = new TextBlock
            {
                Text = row.BrandGlyph,
                FontSize = 11,
                FontWeight = FontWeight.Bold,
                Foreground = Brush.Parse(LauncherDesignTokens.BrandBadgeForegroundColor),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            },
        };

        var vendorBadge = new Border
        {
            Background = Brush.Parse(LauncherDesignTokens.BadgeInfoBackgroundColor),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(8, 2),
            Child = new TextBlock
            {
                Text = row.Vendor,
                FontSize = 11,
                Foreground = Brush.Parse(LauncherDesignTokens.BadgeInfoForegroundColor),
            },
            VerticalAlignment = VerticalAlignment.Center,
        };

        var versionText = string.IsNullOrEmpty(row.Version) ? "(版本未知)" : row.Version;
        var versionBlock = new TextBlock
        {
            Text = versionText,
            FontSize = 11,
            FontFamily = new FontFamily(MonospaceFontFamily),
            Foreground = Brush.Parse(LauncherDesignTokens.TextMutedColor),
            VerticalAlignment = VerticalAlignment.Center,
        };

        var pathBlock = new TextBlock
        {
            Text = row.ResolvedPath,
            FontSize = 11,
            FontFamily = new FontFamily(MonospaceFontFamily),
            Foreground = Brush.Parse(LauncherDesignTokens.TextMutedColor),
            TextTrimming = TextTrimming.CharacterEllipsis,
        };

        var alreadyLabel = new TextBlock
        {
            Text = "已添加",
            FontSize = 11,
            FontWeight = FontWeight.SemiBold,
            Foreground = Brush.Parse(LauncherDesignTokens.BadgeSuccessForegroundColor),
            VerticalAlignment = VerticalAlignment.Center,
        };

        var headerLine = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Children =
            {
                brandBadge,
                nameBlock,
                vendorBadge,
                versionBlock,
            },
        };

        if (row.IsAlreadyAdded)
            headerLine.Children.Add(alreadyLabel);

        Control? infoButton = null;
        if (!string.IsNullOrEmpty(row.HomePageUrl))
        {
            var ib = new Button
            {
                Content = "ℹ️",
                FontSize = 12,
                Padding = new Thickness(6, 2),
                Classes = { "secondary" },
                VerticalAlignment = VerticalAlignment.Center,
            };
            ib.Click += (_, _) =>
            {
                try
                {
                    Process.Start(new ProcessStartInfo(row.HomePageUrl) { UseShellExecute = true });
                }
                catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
                {
                    // Browser launch failed — non-fatal.
                }
            };
            infoButton = ib;
        }

        var rowGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto"),
            Children =
            {
                checkbox.WithGridColumn(0),
                new StackPanel
                {
                    Spacing = 4,
                    Margin = new Thickness(8, 0, 0, 0),
                    Children = { headerLine, pathBlock },
                }.WithGridColumn(1),
            },
        };

        if (infoButton is not null)
            rowGrid.Children.Add(infoButton.WithGridColumn(2));

        return new Border
        {
            Background = Brush.Parse(LauncherDesignTokens.SurfaceColor),
            BorderThickness = new Thickness(1),
            BorderBrush = Brush.Parse(LauncherDesignTokens.BorderColor),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(10),
            Opacity = row.IsAlreadyAdded ? 0.55 : 1.0,
            Child = rowGrid,
        };
    }
}
