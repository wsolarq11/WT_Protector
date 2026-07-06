namespace WTLauncher.Core;

/// <summary>
/// One-time JSON → LiteDB migration. Called at startup before loading the store.
/// If the JSON store file exists and store.db does not, imports data and renames JSON to .bak.
/// Phase 3 migration — safe to leave in production permanently (no-op after first run).
/// </summary>
public static class MigrationTool
{
    public static Result<Unit, StoreLoadError> MigrateIfNeeded(string storeDirectory)
    {
        var jsonPath = Path.Combine(storeDirectory, LauncherStore.StoreFileName);
        var dbPath = Path.Combine(storeDirectory, "store.db");
        var bakPath = jsonPath + ".bak";

        // Already migrated — nothing to do.
        if (File.Exists(dbPath))
            return Unit.Value;

        // No JSON source — fresh install, LiteDB will be created on first save.
        if (!File.Exists(jsonPath))
            return Unit.Value;

        // Load JSON, save to LiteDB, rename JSON to .bak.
        var loadResult = LauncherStore.TryLoad(jsonPath);
        if (loadResult.IsErr)
            return loadResult.Match<Result<Unit, StoreLoadError>>(
                ok: _ => default!,
                err: e => e);

        var storeFile = loadResult.Match(ok: f => f, err: _ => default!);

        // Write to LiteDB
        using var db = LiteDbStore.Open(storeDirectory);
        var saveResult = db.Save(storeFile, dbPath);
        if (saveResult.IsErr)
            return saveResult.Match<Result<Unit, StoreLoadError>>(
                ok: _ => default!,
                err: e => new StoreLoadError.IoError(e.Message));

        // Atomic rename — keeps the original as backup
        try
        {
            File.Move(jsonPath, bakPath, overwrite: true);
        }
        catch (IOException)
        {
            // If rename fails, the JSON still exists — LiteDB is primary now.
            // The next startup will skip migration (dbPath exists) and JSON serves
            // as a manual backup.
        }

        return Unit.Value;
    }
}
