using System.Text;

namespace WTLauncher.Core;

/// <summary>Entry text helpers. Prefer <see cref="EntryName"/> for new code —
/// the static methods here delegate to EntryName internally.</summary>
public static class LauncherEntryText
{
    [Obsolete("Use EntryName.TryParse() instead.")]
    public static string Normalize(string? value)
    {
        var result = EntryName.TryParse(value);
        return result.Match(ok: n => n.Value, err: _ => string.Empty);
    }

    [Obsolete("Use EntryName.RequireNonEmpty() instead.")]
    public static string RequireNonEmpty(string? value, string fieldName = "Entry name")
    {
        var result = EntryName.TryParse(value);
        return result.Match(
            ok: n => n.Value,
            err: _ => throw new InvalidOperationException($"{fieldName} is required."));
    }
}
