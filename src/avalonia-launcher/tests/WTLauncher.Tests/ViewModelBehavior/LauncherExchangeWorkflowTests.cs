using System.Collections.Immutable;
using WTLauncher.App.ViewModels;
using WTLauncher.Core;

using WTLauncher.Tests;
namespace WTLauncher.Tests.ViewModelBehavior;

[Trait("Category", TestCategories.ViewModelBehavior)]
public sealed partial class MainWindowViewModelTests
{
    [Fact]
    public async Task ExchangeWorkflowExportsSelectedEntriesToFile()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreatePortableFixture(
            temp,
            Entry("one", "One", command: "codex", selected: true),
            Entry("two", "Two", command: "opencode"));
        var exportPath = Path.Combine(temp.Path, "export.json");
        var dialogs = new StubDialogService
        {
            OnPickExportPath = () => Task.FromResult<string?>(exportPath),
        };
        var viewModel = CreateViewModel(config, settings, dialogService: dialogs);

        await viewModel._exchangeWorkflow.ExportSelectedAsync();
        var exported = LauncherStore.Load(exportPath);

        Assert.True(File.Exists(exportPath));
        Assert.Single(exported.Entries);
        Assert.Equal("one", exported.Entries.Single().Id.Value);
    }

    [Fact]
    public async Task ExchangeWorkflowImportsAndPersistsMergedStore()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreatePortableFixture(temp, Entry("one", "One"));
        var importPath = Path.Combine(temp.Path, "import.json");
        LauncherStore.Save(
            new LauncherStoreFile
            {
                Entries = [Entry("imported", "Imported", directory: temp.Path)],
                Settings = new LauncherSettings(),
                Presets = [],
            },
            importPath,
            backup: false);
        var dialogs = new StubDialogService
        {
            OnPickImportPath = () => Task.FromResult<string?>(importPath),
            OnRequestImport = (_, _) => Task.FromResult<LauncherImportPlan?>(new LauncherImportPlan { Mode = LauncherImportMode.AddNew }),
        };
        var viewModel = CreateViewModel(config, settings, dialogService: dialogs);

        await viewModel._exchangeWorkflow.ImportAsync();
        var store = LauncherStore.Load(config);

        Assert.Contains(viewModel.Entries, entry => entry.Id == "imported");
        Assert.Contains(store.Entries, entry => entry.Id.Value == "imported");
    }
}
