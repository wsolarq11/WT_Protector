namespace WTLauncher.Core;

internal static class LauncherSettingsCopies
{
    /// <summary>Deep-clones settings. All collection properties use ImmutableList/ImmutableDictionary
    /// with value semantics — no defensive copy needed. Window is a value-type record (cloned via `with { }`).
    /// SchemaVersion is reset to 1 to ensure the clone always conforms to the current schema,
    /// regardless of the source version.</summary>
    public static LauncherSettings Clone(LauncherSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return settings with
        {
            // SchemaVersion is serialized as "schemaVersion". We pin it to 1
            // to ensure the clone conforms to the current schema.
            SchemaVersion = 1,
        };
    }
}
