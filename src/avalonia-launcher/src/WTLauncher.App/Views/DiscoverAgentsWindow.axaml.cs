using System.ComponentModel;
using System.Diagnostics;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using WTLauncher.App.Infrastructure;
using WTLauncher.App.Localization;
using WTLauncher.App.ViewModels;
using WTLauncher.Core;

namespace WTLauncher.App.Views;

/// <summary>
/// Modal dialog for AI Agent CLI discovery. The dialog owns no business
/// decisions — it renders the supplied DiscoverAgentsViewModel and returns
/// the user's selected DiscoveredAgent list via Close().
///
/// ponytail: bindings done via direct PropertyChanged subscriptions rather
/// than WhenAnyValue/ReactiveUI — the project does not use ReactiveUI and
/// adding it just for this dialog would be a dependency regression.
/// </summary>
public sealed partial class DiscoverAgentsWindow : Window
{
    private const string MonospaceFontFamily = "Cascadia Code, Consolas, Courier New, monospace";
    private readonly DiscoverAgentsViewModel _viewModel;

    public DiscoverAgentsWindow()
    {
        _viewModel = null!;
        InitializeComponent();
        ApplyLocalizedChrome();
    }

    public DiscoverAgentsWindow(DiscoverAgentsViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        _viewModel = viewModel;
        DataContext = viewModel;

        InitializeComponent();
        ApplyLocalizedChrome();

        InitDirectoryPanel();
        InitCommandPicker();

        _viewModel.PropertyChanged += HandleViewModelPropertyChanged;
        _viewModel.Rows.CollectionChanged += (_, _) => RenderRows();
        RenderRows();
        SyncUiFromViewModel();

        Opened += async (_, _) =>
        {
            ScanButton.Focus();
            await _viewModel.ScanAsync();
        };
    }

    private void ApplyLocalizedChrome()
    {
        HeadingText.Text = LauncherLocalization.Format("discover.heading");
        SubtitleText.Text = LauncherLocalization.Format("discover.subtitle");
        PathModeRadio.Content = LauncherLocalization.Format("discover.pathMode");
        DirectoryModeRadio.Content = LauncherLocalization.Format("discover.directoryMode");
        ScanButton.Content = LauncherLocalization.Format("discover.rescan");
        AutomationProperties.SetName(ScanButton, LauncherLocalization.Format("discover.rescanAutomation"));
        DirectoryHintText.Text = LauncherLocalization.Format("discover.directoryHint");
        CommandTemplateLabel.Text = LauncherLocalization.Format("discover.commandTemplateLabel");
        EntryNameLabel.Text = LauncherLocalization.Format("discover.entryNameLabel");
        EntryNameBox.PlaceholderText = LauncherLocalization.Format("discover.entryNamePlaceholder");
        CommandPreviewLabel.Text = LauncherLocalization.Format("discover.commandPreviewLabel");
        CancelButton.Content = LauncherLocalization.Format("discover.cancel");
        AutomationProperties.SetName(CancelButton, LauncherLocalization.Format("discover.cancelAutomation"));
        AutomationProperties.SetName(AddButton, LauncherLocalization.Format("discover.addSelected"));
    }

    private void InitDirectoryPanel()
    {
        var directories = new StackPanel { Spacing = 6 };
        foreach (var row in _viewModel.ExistingDirectories)
        {
            var radio = new RadioButton
            {
                GroupName = "ExistingDirectory",
                Content = LauncherLocalization.Format(
                    "discover.referenceCount",
                    row.Directory,
                    row.ReferenceCount),
            };
            radio.IsCheckedChanged += (_, _) =>
            {
                if (radio.IsChecked == true)
                    _viewModel.SelectedDirectory = row;
            };
            directories.Children.Add(radio);
        }
        DirectoryListPanel.Children.Add(directories);
    }

    private void InitCommandPicker()
    {
        CommandPicker.ItemsSource = _viewModel.CommandTemplateKeys;
        CommandPicker.SelectedItem = _viewModel.SelectedCommandTemplateKey;
    }

    private void OnPathModeToggled(object? sender, RoutedEventArgs e)
    {
        if (PathModeRadio.IsChecked == true)
            _viewModel.Mode = DiscoveryMode.PathScan;
    }

    private void OnDirectoryModeToggled(object? sender, RoutedEventArgs e)
    {
        if (DirectoryModeRadio.IsChecked == true)
            _viewModel.Mode = DiscoveryMode.ExistingDirectory;
    }

    private async void OnScanClicked(object? sender, RoutedEventArgs e)
    {
        try { await _viewModel.ScanAsync(); }
        catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
        {
            ErrorLog.Write(ex, "DiscoverAgents.Scan");
        }
    }

    private void OnCancelClicked(object? sender, RoutedEventArgs e) => Close(null);

    private void OnAddClicked(object? sender, RoutedEventArgs e) =>
        Close(_viewModel.GetResult());

    private void OnCommandTemplateChanged(object? sender, SelectionChangedEventArgs e)
    {
        _viewModel.SelectedCommandTemplateKey = CommandPicker.SelectedItem as string;
    }

    private void OnEntryNameChanged(object? sender, TextChangedEventArgs e)
    {
        _viewModel.EntryName = EntryNameBox.Text ?? string.Empty;
    }

    private void HandleViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(DiscoverAgentsViewModel.StatusText):
                StatusText.Text = _viewModel.StatusText;
                break;
            case nameof(DiscoverAgentsViewModel.IsScanning):
                ScanButton.IsEnabled = !_viewModel.IsScanning;
                break;
            case nameof(DiscoverAgentsViewModel.SelectedCount):
            case nameof(DiscoverAgentsViewModel.CanAddSelected):
            case nameof(DiscoverAgentsViewModel.CanConfirm):
            case nameof(DiscoverAgentsViewModel.Mode):
            case nameof(DiscoverAgentsViewModel.CommandPreview):
                SyncUiFromViewModel();
                break;
            case nameof(DiscoverAgentsViewModel.EntryName):
                if (EntryNameBox.Text != _viewModel.EntryName)
                    EntryNameBox.Text = _viewModel.EntryName;
                break;
        }
    }

    private void SyncUiFromViewModel()
    {
        StatusText.Text = _viewModel.StatusText;
        ScanButton.IsEnabled = !_viewModel.IsScanning;
        PathPanel.IsVisible = _viewModel.IsPathScanMode;
        DirectoryPanel.IsVisible = _viewModel.IsExistingDirectoryMode;
        AddButton.IsEnabled = _viewModel.CanConfirm;
        AddButton.Content = _viewModel.IsExistingDirectoryMode
            ? LauncherLocalization.Format("discover.createEntry")
            : _viewModel.SelectedCount == 0
                ? LauncherLocalization.Format("discover.addSelected")
                : LauncherLocalization.Format("discover.addSelectedCount", _viewModel.SelectedCount);
        CommandPreview.Text = _viewModel.CommandPreview;
    }

    private void RenderRows()
    {
        var panel = RowsPanel;
        panel.Children.Clear();

        if (_viewModel.Rows.Count == 0)
        {
            panel.Children.Add(new TextBlock
            {
                Text = LauncherLocalization.Format("discover.empty"),
                FontSize = 12,
                Foreground = (Brush)Application.Current!.FindResource("TextMutedBrush")!,
                Margin = new Thickness(8, 12, 8, 12),
            });
            return;
        }

        foreach (var row in _viewModel.Rows)
            panel.Children.Add(BuildRowCard(row));
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
            Foreground = (Brush)Application.Current!.FindResource(
                row.IsAlreadyAdded ? "TextMutedBrush" : "TextPrimaryBrush")!,
        };

        var brandBadge = new Border
        {
            Width = LauncherDesignTokens.BrandBadgeSize,
            Height = LauncherDesignTokens.BrandBadgeSize,
            CornerRadius = new CornerRadius(LauncherDesignTokens.BrandBadgeCornerRadius),
            Background = new SolidColorBrush(Color.Parse(row.BrandColorHex)),
            IsVisible = row.HasBrand,
            VerticalAlignment = VerticalAlignment.Center,
            Child = new TextBlock
            {
                Text = row.BrandGlyph,
                FontSize = 11,
                FontWeight = FontWeight.Bold,
                Foreground = new SolidColorBrush(Color.Parse(LauncherDesignTokens.BrandBadgeForegroundColor)),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            },
        };

        var vendorBadge = new Border
        {
            Background = new SolidColorBrush(Color.Parse(LauncherDesignTokens.BadgeInfoBackgroundColor)),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(8, 2),
            Child = new TextBlock
            {
                Text = row.Vendor,
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.Parse(LauncherDesignTokens.BadgeInfoForegroundColor)),
            },
            VerticalAlignment = VerticalAlignment.Center,
        };

        var versionText = string.IsNullOrEmpty(row.Version)
            ? LauncherLocalization.Format("discover.versionUnknown")
            : row.Version;
        var versionBlock = new TextBlock
        {
            Text = versionText,
            FontSize = 11,
            FontFamily = new FontFamily(MonospaceFontFamily),
            Foreground = (Brush)Application.Current!.FindResource("TextMutedBrush")!,
            VerticalAlignment = VerticalAlignment.Center,
        };

        var pathBlock = new TextBlock
        {
            Text = row.ResolvedPath,
            FontSize = 11,
            FontFamily = new FontFamily(MonospaceFontFamily),
            Foreground = (Brush)Application.Current!.FindResource("TextMutedBrush")!,
            TextTrimming = TextTrimming.CharacterEllipsis,
        };

        var alreadyLabel = new TextBlock
        {
            Text = LauncherLocalization.Format("discover.alreadyAdded"),
            FontSize = 11,
            FontWeight = FontWeight.SemiBold,
            Foreground = new SolidColorBrush(Color.Parse(LauncherDesignTokens.BadgeSuccessForegroundColor)),
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
                Content = LauncherLocalization.Format("discover.info"),
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
                catch (Exception ex) when (ex is Win32Exception or InvalidOperationException)
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
            Background = (Brush)Application.Current!.FindResource("SurfaceBrush")!,
            BorderThickness = new Thickness(1),
            BorderBrush = (Brush)Application.Current!.FindResource("CardBorderBrush")!,
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(10),
            Opacity = row.IsAlreadyAdded ? 0.55 : 1.0,
            Child = rowGrid,
        };
    }
}
