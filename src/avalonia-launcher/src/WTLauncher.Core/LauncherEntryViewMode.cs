namespace WTLauncher.Core;

public enum LauncherEntryViewMode
{
    Cards,
    Details,
    Names,
}

public static class LauncherEntryViewModes
{
    public static LauncherEntryViewMode Parse(string? value)
    {
        return value switch
        {
            "details" => LauncherEntryViewMode.Details,
            "names" => LauncherEntryViewMode.Names,
            _ => LauncherEntryViewMode.Cards,
        };
    }

    public static string ToSettingsValue(this LauncherEntryViewMode mode)
    {
        return mode switch
        {
            LauncherEntryViewMode.Details => "details",
            LauncherEntryViewMode.Names => "names",
            _ => "cards",
        };
    }
}
