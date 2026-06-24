using System.Collections.Immutable;

namespace WTLauncher.Core;

internal static class LauncherSettingsCopies
{
    /// <summary>Deep-clones settings. All collection properties use ImmutableList/ImmutableDictionary
    /// with value semantics — no defensive copy needed. Window is a value-type record (cloned via `with { }`).
    /// SchemaVersion is reset to 1 to ensure the clone always conforms to the current schema,
    /// regardless of the source version. [JsonIgnore] prevents serialization in either case.</summary>
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
            // SchemaVersion is not serialized ([JsonIgnore]) but we explicitly
            // pin it to 1 so any non-serialization use of the clone is safe.
            SchemaVersion = 1,
        };
    }

    [Obsolete("ImmutableDictionary has value semantics; CloneCommandTemplates is a no-op. Use the dictionary directly.")]
    public static ImmutableDictionary<string, string> CloneCommandTemplates(ImmutableDictionary<string, string> templates) =>
        templates;
}
