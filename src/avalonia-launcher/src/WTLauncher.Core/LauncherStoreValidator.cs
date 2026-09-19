namespace WTLauncher.Core;

/// <summary>
/// Zone 0: Pure schema validation for LauncherStoreFile.
/// No file I/O, no side effects.
///
/// Runtime hard-fails only on invariants that make the UI identity or window
/// layout unsafe: schema version, duplicate/empty entry ids, and non-positive
/// window bounds. Preset-reference and template-reference problems remain
/// soft UI warnings on purpose (see MainWindowViewModel.Validate* methods):
/// a hand-edited launcher.json must stay openable so the user can repair it.
/// </summary>
public static class LauncherStoreValidator
{
    public const int ExpectedSchemaVersion = 2;

    /// <summary>Validates runtime-fatal invariants. Throws InvalidDataException on failure.</summary>
    public static void ThrowIfInvalid(LauncherStoreFile store)
    {
        if (store.SchemaVersion != ExpectedSchemaVersion)
        {
            throw new InvalidDataException(
                $"Launcher store schema version {store.SchemaVersion} is unsupported. " +
                $"Expected version {ExpectedSchemaVersion}.");
        }

        var entryIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entry in store.Entries)
        {
            if (!entryIds.Add(entry.Id.Value))
                throw new InvalidDataException($"Launcher store contains duplicate entry id: {entry.Id.Value}.");
        }

        var window = store.Settings.Window;
        if (window.Width <= 0 || window.Height <= 0)
        {
            throw new InvalidDataException(
                $"Launcher store window dimensions must be positive; got width {window.Width}, height {window.Height}.");
        }
    }
}
