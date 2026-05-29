namespace WTLauncher.Core;

public static class LauncherEntryIds
{
    public static bool IsUsable(string? id) => !string.IsNullOrWhiteSpace(id);

    public static bool Same(string? left, string? right) =>
        string.Equals(left, right, StringComparison.Ordinal);
}
