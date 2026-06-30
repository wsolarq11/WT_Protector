using System.Globalization;
using System.Text;

namespace WTLauncher.Core;

/// <summary>Entry ID helpers. Prefer <see cref="EntryId"/> for new code —
/// the static methods here delegate to EntryId internally.</summary>
public static class LauncherEntryIds
{
    /// <summary>Prefer <see cref="EntryId.Comparer"/>.</summary>
    public static readonly StringComparer Comparer = EntryId.Comparer;

    /// <summary>Prefer <see cref="EntryId.WindowsReservedNames"/>.</summary>
    internal static readonly HashSet<string> WindowsReservedNames = EntryId.WindowsReservedNames;

    [Obsolete("Use EntryId.TryParse() instead.")]
    public static string Normalize(string? id)
    {
        var result = EntryId.TryParse(id);
        return result.Match(ok: e => e.Value, err: _ => string.Empty);
    }

    [Obsolete("Use EntryId.IsUsable() instead.")]
    public static bool IsUsable(string? id) => EntryId.IsUsable(id);

    [Obsolete("Use EntryId.Same() instead.")]
    public static bool Same(string? left, string? right) => EntryId.Same(left, right);

    [Obsolete("Use EntryId.SameNormalized() instead.")]
    public static bool SameNormalized(string? left, string? right) => EntryId.SameNormalized(left, right);

    [Obsolete("Use EntryId.RequireEntryId() instead.")]
    public static string RequireEntryId(string? id, string paramName) => EntryId.RequireEntryId(id, paramName);

    [Obsolete("Use EntryId.ToRequiredUniqueList() instead.")]
    public static List<string> ToRequiredUniqueList(IEnumerable<string> ids, string paramName)
        => EntryId.ToRequiredUniqueList(ids, paramName);

    [Obsolete("Use EntryId.ToRequiredSet() instead.")]
    public static HashSet<string> ToRequiredSet(IEnumerable<string> ids, string paramName)
        => EntryId.ToRequiredSet(ids, paramName);
}
