using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

/// <summary>
/// 单一事实源: 所有条目的卡片尺寸由此提供。取代之前分散在每个
/// LauncherEntryViewModel 上的 [ObservableProperty] 加硬编码默认值。
/// 无缓存、无默认值陷阱 —— Recalculate() 后所有订阅的 VM 自动更新。
/// </summary>
public sealed class CardLayoutProvider : ObservableObject
{
    public const double MinimumCardsWidth = 220;
    public const double MinimumCardsHeight = 140;

    private double _cardWidth;
    private double _cardHeight;
    private double _nameTileWidth;
    private double _nameTileHeight;
    private int _cardColumns = 1;
    private int _cardRows = 1;
    private bool _fillsAvailableHeight = true;

    public double CardWidth { get => _cardWidth; private set => SetProperty(ref _cardWidth, value); }
    public double CardHeight { get => _cardHeight; private set => SetProperty(ref _cardHeight, value); }
    public double NameTileWidth { get => _nameTileWidth; private set => SetProperty(ref _nameTileWidth, value); }
    public double NameTileHeight { get => _nameTileHeight; private set => SetProperty(ref _nameTileHeight, value); }
    public int CardColumns { get => _cardColumns; private set => SetProperty(ref _cardColumns, value); }
    public int CardRows { get => _cardRows; private set => SetProperty(ref _cardRows, value); }
    public bool FillsAvailableHeight { get => _fillsAvailableHeight; private set => SetProperty(ref _fillsAvailableHeight, value); }

    /// <summary>仅供测试使用 —— 直接设置卡片尺寸以验证布局密度逻辑。</summary>
    internal void SetCardSizesForTest(double cardWidth, double cardHeight, double nameTileWidth, double nameTileHeight)
    {
        CardWidth = cardWidth;
        CardHeight = cardHeight;
        NameTileWidth = nameTileWidth;
        NameTileHeight = nameTileHeight;
    }

    public void Recalculate(Size availableSize, int count)
    {
        var w = double.IsFinite(availableSize.Width) ? Math.Max(MinimumCardsWidth, availableSize.Width) : MinimumCardsWidth;
        var h = double.IsFinite(availableSize.Height) ? Math.Max(MinimumCardsHeight, availableSize.Height) : MinimumCardsHeight;
        count = Math.Max(1, count);

        var nameTileLayout = LauncherEntryLayoutCalculator.Calculate(w, h, count);
        var cardGrid = LauncherEntryLayoutCalculator.CalculateCardGrid(w, h, count);
        CardWidth = cardGrid.Width;
        CardHeight = cardGrid.Height;
        CardColumns = cardGrid.Columns;
        CardRows = cardGrid.Rows;
        FillsAvailableHeight = cardGrid.FillsAvailableHeight;
        NameTileWidth = nameTileLayout.NameTileWidth;
        NameTileHeight = nameTileLayout.NameTileHeight;
    }
}
