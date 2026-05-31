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
            CardsId => CardsId,
            DetailsId => DetailsId,
            NamesId => NamesId,
            _ => throw new InvalidDataException($"Unknown launcher entry view mode: {normalized}."),
        };
    }

    public static string ToId(LauncherEntryViewMode mode) => mode switch
    {
        LauncherEntryViewMode.Cards => CardsId,
        LauncherEntryViewMode.Details => DetailsId,
        LauncherEntryViewMode.Names => NamesId,
        _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown launcher entry view mode."),
    };
}
