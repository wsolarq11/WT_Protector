namespace WTLauncher.Core;

public static class MigrationTool
{
    public static void MigrateIfNeeded(string storeDirectory)
    {
        var jsonPath = Path.Combine(storeDirectory, LauncherStore.StoreFileName);
        var dbPath = Path.Combine(storeDirectory, "store.db");
        var bakPath = jsonPath + ".bak";

        if (File.Exists(dbPath))
            return;

        if (!File.Exists(jsonPath))
            return;

        var storeFile = LauncherStore.Load(jsonPath);

        using var db = LiteDbStore.Open(storeDirectory);
        db.Save(storeFile, dbPath);

        try
        {
            File.Move(jsonPath, bakPath, overwrite: true);
        }
        catch (IOException)
        {
        }
    }
}
