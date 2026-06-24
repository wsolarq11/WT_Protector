using System.Collections.Immutable;

namespace WTLauncher.Core;

internal static class LauncherSettingsCopies
{
    /// <summary>Deep-clones settings. All collection properties use ImmutableList/ImmutableDictionary
    /// with value semantics — no defensive copy needed. Window is a value-type record (cloned via `with { }`).</summary>
    public static LauncherSettings Clone(LauncherSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return settings with
        {
            // Immutable collections have value semantics; no copy required.
            CommandTemplates = settings.CommandTemplates,
            CustomEntryOrder = settings.CustomEntryOrder,
            SearchHistory = settings.SearchHistory,
            Window = settings.Window,
        };
    }

    public static ImmutableDictionary<string, string> CloneCommandTemplates(ImmutableDictionary<string, string> templates) =>
        templates;
}
