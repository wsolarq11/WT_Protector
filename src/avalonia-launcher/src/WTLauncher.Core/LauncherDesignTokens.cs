namespace WTLauncher.Core;

/// <summary>
/// Centralized design tokens used across the launcher UI.
/// These are the single source of truth for colors, spacing, and sizing constants.
/// </summary>
public static class LauncherDesignTokens
{
    // ── Colors ──────────────────────────────────────────────
    public const string PrimaryColor = "#2563EB";          // primary buttons, accent
    public const string DestructiveColor = "#DC2626";      // delete/danger actions
    public const string ErrorColor = "#B22222";           // error text (Firebrick, contrast 5.24:1 AA)
    public const string BackgroundColor = "#F8FAFC";       // window background
    public const string TextPrimaryColor = "#0F172A";      // headings, primary text
    public const string TextMutedColor = "#4B5563";        // secondary/muted text (contrast 5.8:1 AA)
    public const string BorderColor = "#E2E8F0";           // card/item borders
    public const string SurfaceColor = "White";            // card/item backgrounds
    public const string BoxShadowColor = "#120F172A";      // card shadow, #AARRGGBB 是 Avalonia Color.Parse 标准格式

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

    // ── Drag handle ────────────────────────────────────────
    public const string DragHandleTextColor = "#334155";        // drag handle label text

    // ── Spacing / Rhythm (golden-ratio aligned) ─────────────
    public const double SpacingSmall = 8;

    // ── Dialog ──────────────────────────────────────────────
    public const double DialogFooterSpacing = 10;

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

    // ── Window ──────────────────────────────────────────────
    public const double WindowDefaultWidth = 1180;
    public const double WindowDefaultHeight = 760;
    public const double WindowMinWidth = 940;
    public const double WindowMinHeight = 600;
}
