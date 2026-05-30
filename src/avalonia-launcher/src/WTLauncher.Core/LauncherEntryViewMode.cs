namespace WTLauncher.Core;

public enum LauncherEntryViewMode
{
    Cards,
    Details,
    Names,
}

public static class LauncherEntryViewModes
{
    public const string CardsId = "cards";
    public const string DetailsId = "details";
    public const string NamesId = "names";

    public static string NormalizeId(string? entryViewMode)
    {
        var normalized = (entryViewMode ?? string.Empty).Trim();
        return normalized switch
        {
            DetailsId => DetailsId,
            NamesId => NamesId,
            _ => CardsId,
        };
    }

    public static string ToId(LauncherEntryViewMode mode) => mode switch
    {
        LauncherEntryViewMode.Details => DetailsId,
        LauncherEntryViewMode.Names => NamesId,
        _ => CardsId,
    };
}
