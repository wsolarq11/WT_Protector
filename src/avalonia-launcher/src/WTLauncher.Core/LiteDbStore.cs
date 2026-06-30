using LiteDB;

namespace WTLauncher.Core;

/// <summary>
/// LiteDB-backed implementation of <see cref="IStore"/>. Stores the entire
/// <see cref="LauncherStoreFile"/> as a single BSON document in a collection
/// named "store". Provides ACID transactions via LiteDB's built-in journal.
/// Phase 3 implementation — replaces hand-written JSON crash recovery.
/// </summary>
public sealed class LiteDbStore : IStore, IDisposable
{
    private readonly LiteDatabase _db;

    private LiteDbStore(string dbPath)
    {
        _db = new LiteDatabase($"Filename={dbPath};Connection=direct");
    }

    /// <summary>Opens or creates a LiteDB store at the given path.</summary>
    public static LiteDbStore Open(string storeDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storeDirectory);
        Directory.CreateDirectory(storeDirectory);
        var dbPath = Path.Combine(storeDirectory, "store.db");
        return new LiteDbStore(dbPath);
    }

    public Result<LauncherStoreFile, StoreLoadError> Load(string storePath)
    {
        try
        {
            var collection = _db.GetCollection<StoreDocument>("store");
            var doc = collection.FindOne(_ => true);
            if (doc is null)
                return new LauncherStoreFile();

            return new LauncherStoreFile
            {
                SchemaVersion = doc.SchemaVersion,
                Entries = doc.Entries,
                Settings = doc.Settings,
                Presets = doc.Presets,
                DefaultPresetId = doc.DefaultPresetId,
            };
        }
        catch (LiteException ex)
        {
            return new StoreLoadError.JsonError(ex.Message);
        }
        catch (IOException ex)
        {
            return new StoreLoadError.IoError(ex.Message);
        }
    }

    public Result<Unit, StoreSaveError> Save(LauncherStoreFile data, string storePath)
    {
        try
        {
            var collection = _db.GetCollection<StoreDocument>("store");
            collection.DeleteAll();
            collection.Insert(new StoreDocument
            {
                SchemaVersion = data.SchemaVersion,
                Entries = data.Entries,
                Settings = data.Settings,
                Presets = data.Presets,
                DefaultPresetId = data.DefaultPresetId,
            });
            // LiteDB auto-commits on dispose or explicit checkpoint
            _db.Checkpoint();
            return Unit.Value;
        }
        catch (LiteException ex)
        {
            return new StoreSaveError.SerializationError(ex.Message);
        }
        catch (IOException ex)
        {
            return new StoreSaveError.IoError(ex.Message);
        }
        catch (UnauthorizedAccessException)
        {
            return new StoreSaveError.AccessDenied(storePath);
        }
    }

    public void Dispose() => _db.Dispose();
}

/// <summary>BSON document mirroring <see cref="LauncherStoreFile"/> for LiteDB storage.</summary>
internal sealed class StoreDocument
{
    [BsonId]
    public int Id { get; set; } = 1;

    public int SchemaVersion { get; set; } = 2;
    public System.Collections.Immutable.ImmutableList<LaunchEntry> Entries { get; set; } = System.Collections.Immutable.ImmutableList<LaunchEntry>.Empty;
    public LauncherSettings Settings { get; set; } = new();
    public System.Collections.Immutable.ImmutableList<LauncherPreset> Presets { get; set; } = System.Collections.Immutable.ImmutableList<LauncherPreset>.Empty;
    public string DefaultPresetId { get; set; } = "";
}
