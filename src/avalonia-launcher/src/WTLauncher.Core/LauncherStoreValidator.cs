namespace WTLauncher.Core;

/// <summary>
/// Zone 0: Pure schema validation for LauncherStoreFile.
/// No file I/O, no side effects.
///
/// Example
/// -------
/// LauncherStoreValidator.ThrowIfInvalid(store);
/// </summary>
public static class LauncherStoreValidator
{
    public const int ExpectedSchemaVersion = 2;
    public const int MinimumSupportedSchemaVersion = 1;

    /// <summary>Validates schema version. Throws InvalidDataException on failure.</summary>
    public static void ThrowIfInvalid(LauncherStoreFile store)
    {
        if (store.SchemaVersion > ExpectedSchemaVersion)
            throw new InvalidDataException(
                $"Launcher store schema version {store.SchemaVersion} is from a newer version of the app. " +
                "Please upgrade WT Launcher.");

        if (store.SchemaVersion < MinimumSupportedSchemaVersion)
            throw new InvalidDataException(
                $"Launcher store schema version {store.SchemaVersion} is too old. " +
                $"Minimum supported version is {MinimumSupportedSchemaVersion}. " +
                "Regenerate config or use an older app version.");
    }
}
