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

    // ── Secondary button ───────────────────────────────────
    public const string SecondaryButtonBackgroundColor = "#EEF2FF"; // secondary button background
    public const string SecondaryButtonForegroundColor = "#1D4ED8"; // secondary button text

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
