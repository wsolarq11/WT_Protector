using WTLauncher.Core;

namespace WTLauncher.App.Features.ConfigExchange;

/// <summary>External config file port. Import/export callers depend on this
/// narrow interface instead of touching LauncherStore/LauncherJson directly.</summary>
public interface IConfigExchangeService
{
    public LauncherStoreFile LoadImport(string path);
    public LauncherStoreFile ApplyImport(LauncherStoreFile current, LauncherStoreFile imported, LauncherImportPlan plan);
    public LauncherStoreFile BuildExport(LauncherStoreFile current, IReadOnlyList<string> entryIds);
    public void SaveExport(LauncherStoreFile export, string path);
}

public sealed class ConfigExchangeService : IConfigExchangeService
{
    public LauncherStoreFile LoadImport(string path) => LauncherStore.Load(path);

    public LauncherStoreFile ApplyImport(
        LauncherStoreFile current,
        LauncherStoreFile imported,
        LauncherImportPlan plan) =>
        LauncherConfigExchange.ApplyImport(current, imported, plan);

    public LauncherStoreFile BuildExport(
        LauncherStoreFile current,
        IReadOnlyList<string> entryIds) =>
        LauncherConfigExchange.BuildExportFile(current, entryIds);

    public void SaveExport(LauncherStoreFile export, string path) =>
        LauncherJson.Save(export, path, backup: false);
}
