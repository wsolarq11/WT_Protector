namespace WTLauncher.Core;

/// <summary>
/// Centralized design tokens used across the launcher UI.
/// These are the single source of truth for colors, spacing, and sizing constants.
/// </summary>
public static class LauncherDesignTokens
{
    // ── Colors ──────────────────────────────────────────────
    public const string ActiveBorderColor = "#2563EB";   // active/selected emphasis border (tabs, list rows, swatches)
    public const string DestructiveColor = "#DC2626";      // delete/danger actions
    public const string ErrorColor = "#B22222";           // error text (Firebrick, contrast 5.24:1 AA)
    public const string BackgroundColor = "#F8FAFC";       // window background
    public const string TextPrimaryColor = "#0F172A";      // headings, primary text
    public const string TextMutedColor = "#4B5563";        // secondary/muted text (contrast 5.8:1 AA)
    public const string BorderColor = "#E2E8F0";           // card/item borders
    public const string SurfaceColor = "White";            // card/item backgrounds
    public const string BoxShadowColor = "#120F172A";      // card shadow, #AARRGGBB 是 Avalonia Color.Parse 标准格式
    public const string ControlShadowColor = "#140F172A";  // foreground control elevation shadow
    public const string FocusGlowColor = "#3B82F6";        // foreground focal glow
    public const string DragGhostShadowColor = "#220F172A"; // elevated drag preview shadow

    // ── Semantic state colors ──────────────────────────────
    public const string HoverBackgroundColor = "#F1F5F9";       // generic hover background
    public const string SelectionBackgroundColor = "#EEF2FF";    // selected item background
    public const string SelectionBorderColor = "#C7D2FE";        // selected item border
    public const string SelectionHoverBackgroundColor = "#E0E7FF"; // selected + hover background

    // ── Drag indicator ─────────────────────────────────────
    public const string DragHighlightColor = "#38BDF8";          // drag target border & insert indicator

    // ── Control box ────────────────────────────────────────
    public const string ControlBoxLabelColor = "#475569";        // control box label text

    // ── Status badges ──────────────────────────────────────
    public const string BadgeSuccessBackgroundColor = "#ECFDF5"; // green badge background
    public const string BadgeSuccessForegroundColor = "#047857"; // green badge text
    public const string BadgeInfoBackgroundColor = "#E0F2FE";   // blue badge background
    public const string BadgeInfoForegroundColor = "#0369A1";   // blue badge text
    public const string BadgeWarningBackgroundColor = "#FEF3C7"; // warning badge background
    public const string BadgeWarningForegroundColor = "#B45309"; // warning badge text

    // ── Drag handle ────────────────────────────────────────
    public const string DragHandleTextColor = "#334155";        // drag handle label text

    // ── Per-agent brand badge ──────────────────────────────
    public const double BrandBadgeSize = 22;                    // brand badge width/height on cards
    public const double BrandBadgeCornerRadius = 6;             // brand badge corner radius
    public const string BrandBadgeForegroundColor = "White";    // brand badge glyph text color

    // ── Spacing / Rhythm (golden-ratio aligned) ─────────────
    public const double SpacingSmall = 8;
    public const double SpacingStep = 13;
    public const double SpacingWide = 21;
    public const double SpacingFrame = 34;

    // ── Foreground control shape ─────────────────────────────
    public const double ControlCornerRadius = 10;
    public const double DropdownCornerRadius = 12;
    public const double DialogCornerRadius = 16;
    public const double ButtonCornerRadius = 10;
    public const double BadgeCornerRadius = 14;   // status badge pill radius
    public const double FocusBorderThickness = 2;

    // ── Typography ─────────────────────────────────────────
    public const double FontSizeCaption = 11;
    public const double FontSizeSmall = 12;
    public const double FontSizeBody = 14;
    public const double FontSizeEmphasis = 16;

    // ── Dialog ──────────────────────────────────────────────
    public const double DialogFooterSpacing = 10;
    public const double DialogTitleFontSize = 20;
    public const double DialogOuterPadding = 24;
    public const double DialogFooterTopMargin = 14;

    // ── Card ────────────────────────────────────────────────
    public const double CardBorderRadius = 13;
    public const double CardPadding = 13;

    // ── Card hover lift ─────────────────────────────────────
    // 上浮动画的唯一权威源。第一行卡片 hover 上浮 L px 后，其顶部边框必须仍
    // 高于最近的 clip 边界，否则顶边被裁。嵌套的 ItemsControl 默认 ClipToBounds，
    // 其 clip 边界与卡片顶部 y=0 对齐，因此预留 P 必须加在最内层卡片排列器
    // （FillGridPanel）上，让卡片相对所有 clip 层都有 P px 余量。P 覆盖全部向上
    // 侵占的因素——Avalonia Border 是中心描边（BackgroundSizing.CenterBorder），
    // 边框最外沿比 bounds 高 O px：
    //   L  = CardHoverLift                上浮幅度（translateY 的负值绝对值）
    //   T  = CardSelectedBorderThickness  selected 卡片边框厚度（2px）
    //   O  = T / 2                        中心描边外溢：border 顶沿高出 bounds 顶部 O px
    //   AA = CardHoverLiftAntialias       抗锯齿晕染向上取整为 1px
    //   P  = CardHoverLiftMarginTop = L + O + AA
    // 卡片自身必须 ClipToBounds=false：clip 会连同上浮部分、描边外半侧、BoxShadow
    // 一起裁掉，让 P 无论取多少都无效。
    // ScrollViewer 负 margin = -P 把 P 从根 Grid 顶部间距借出，静止位置不变（16px）。
    public const double CardHoverLift = 2;
    public const double CardSelectedBorderThickness = 2;
    public const double CardHoverLiftAntialias = 1;
    public static double CardSelectedBorderOverflow => CardSelectedBorderThickness / 2;
    public static double CardHoverLiftMarginTop => CardHoverLift + CardSelectedBorderOverflow + CardHoverLiftAntialias;
    public static double CardHoverLiftScrollMarginTop => -CardHoverLiftMarginTop;

    // ── Name Tile ──────────────────────────────────────────
    public const double NameTileGap = 6;
    public const double NameTileRowGap = 4;

    // ── Details view column proportions ─────────────────────
    public const double DetailsNameColumnWeight = 2;
    public const double DetailsDirectoryColumnWeight = 2;
    public const double DetailsCommandColumnWeight = 3;

    // ── Layout chrome (non-card area consumed by header/footer/margins) ──
    // Horizontal: outer Grid left + right margin (24 + 24 in MainWindow.axaml).
    // Recalibrate when the outer Grid Margin changes.
    public const double CardsRegionHorizontalChromeWidth = 48;
    // Vertical: total chrome in header (Row 0) + status bar (Row 2) + margins/gaps.
    // Conservative estimate — recalibrate when header/footer layout changes.
    public const double CardsRegionVerticalChromeHeight = 270;

    // ── Window ──────────────────────────────────────────────
    public const double WindowDefaultWidth = 1180;
    public const double WindowDefaultHeight = 760;
    public const double WindowMinWidth = 940;
    public const double WindowMinHeight = 600;
}
