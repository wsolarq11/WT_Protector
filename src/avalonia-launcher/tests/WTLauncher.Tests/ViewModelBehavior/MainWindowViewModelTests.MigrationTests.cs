using System.Collections.Immutable;
using WTLauncher.App.ViewModels;
using WTLauncher.Core;

using WTLauncher.Tests;
namespace WTLauncher.Tests.ViewModelBehavior;

[Trait("Category", TestCategories.ViewModelBehavior)]
public sealed partial class MainWindowViewModelTests
{
    [Fact]
    public void MigrationUi_ExposesManagementCommandsAndPathMappingControls()
    {
        var controlBar = File.ReadAllText(TestRepositoryPaths.View("LauncherControlBar.axaml"));
        var importWindow = File.ReadAllText(TestRepositoryPaths.View("ImportMigrationWindow.axaml"));

        Assert.Contains("ExportAllConfigCommand", controlBar, StringComparison.Ordinal);
        Assert.Contains("ExportSelectedConfigCommand", controlBar, StringComparison.Ordinal);
        Assert.Contains("ImportConfigCommand", controlBar, StringComparison.Ordinal);
        Assert.Contains("DirectoryOverride, Mode=TwoWay", importWindow, StringComparison.Ordinal);
        Assert.Contains("IsChecked=\"{Binding ReplaceMode}\"", importWindow, StringComparison.Ordinal);
        Assert.Contains("IsChecked=\"{Binding IsSkipped}\"", importWindow, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExportAllConfig_WritesValidStoreFile()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreatePortableFixture(
            temp,
            Entry("one", "One"),
            Entry("two", "Two"));
        var exportPath = Path.Combine(temp.Path, "export.json");
        var dialog = new StubDialogService
        {
            OnPickExportPath = () => Task.FromResult<string?>(exportPath),
        };
        var viewModel = CreateViewModel(config, settings, dialogService: dialog);

        await viewModel.ExportAllConfigCommand.ExecuteAsync(null);

        var exported = LauncherStore.Load(exportPath);
        Assert.Equal(2, exported.Entries.Count);
        Assert.Single(exported.Presets);
    }

    [Fact]
    public async Task ExportSelectedConfig_ExportsOnlySelectedEntries()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreatePortableFixture(
            temp,
            Entry("one", "One"),
            Entry("two", "Two"));
        var exportPath = Path.Combine(temp.Path, "selected.json");
        var dialog = new StubDialogService
        {
            OnPickExportPath = () => Task.FromResult<string?>(exportPath),
        };
        var viewModel = CreateViewModel(config, settings, dialogService: dialog);
        viewModel.Entries.Single(entry => entry.Id == "one").IsSelected = true;

        await viewModel.ExportSelectedConfigCommand.ExecuteAsync(null);

        var exported = LauncherStore.Load(exportPath);
        var entry = Assert.Single(exported.Entries);
        Assert.Equal("one", entry.Id.Value);
    }

    [Fact]
    public async Task ImportConfig_AddsNewEntryAndAppliesDirectoryMapping()
    {
        using var temp = new TempDirectory();
        var existing = Directory.CreateDirectory(Path.Combine(temp.Path, "existing")).FullName;
        var missing = Path.Combine(temp.Path, "missing");
        var (config, settings, _) = CreatePortableFixture(temp, Entry("one", "One", existing));
        var importPath = Path.Combine(temp.Path, "import.json");
        LauncherStore.Save(
            new LauncherStoreFile
            {
                SchemaVersion = 2,
                Entries = [Entry("three", "Three", missing)],
                Presets = [new LauncherPreset { Id = "three", Name = "Three", EntryIds = ["three"] }],
                Settings = new LauncherSettings(),
                DefaultPresetId = "three",
            },
            importPath,
            backup: false);
        var dialog = new StubDialogService
        {
            OnPickImportPath = () => Task.FromResult<string?>(importPath),
            OnRequestImport = (_, _) => Task.FromResult<LauncherImportPlan?>(new LauncherImportPlan
            {
                Mode = LauncherImportMode.AddNew,
                DirectoryOverrides = new Dictionary<string, string> { ["three"] = existing },
            }),
        };
        var viewModel = CreateViewModel(config, settings, dialogService: dialog);

        await viewModel.ImportConfigCommand.ExecuteAsync(null);

        Assert.Equal(2, viewModel.Entries.Count);
        Assert.Equal(existing, viewModel.Entries.Single(entry => entry.Id == "three").Directory);
        Assert.Equal(existing, LauncherStore.Load(config).Entries.Single(entry => entry.Id.Value == "three").Directory.Value);
    }

    [Fact]
    public async Task ImportConfig_AddNewSkipsDuplicateEntryId()
    {
        using var temp = new TempDirectory();
        var existing = Directory.CreateDirectory(Path.Combine(temp.Path, "existing")).FullName;
        var (config, settings, _) = CreatePortableFixture(temp, Entry("one", "One", existing));
        var importPath = Path.Combine(temp.Path, "import.json");
        LauncherStore.Save(
            new LauncherStoreFile
            {
                SchemaVersion = 2,
                Entries = [Entry("one", "One", Path.Combine(temp.Path, "other"))],
                Presets = [],
                Settings = new LauncherSettings(),
                DefaultPresetId = "",
            },
            importPath,
            backup: false);
        var dialog = new StubDialogService
        {
            OnPickImportPath = () => Task.FromResult<string?>(importPath),
            OnRequestImport = (_, _) => Task.FromResult<LauncherImportPlan?>(new LauncherImportPlan()),
        };
        var viewModel = CreateViewModel(config, settings, dialogService: dialog);

        await viewModel.ImportConfigCommand.ExecuteAsync(null);

        var entry = Assert.Single(viewModel.Entries);
        Assert.Equal(existing, entry.Directory);
    }

    [Fact]
    public async Task ImportConfig_ReplaceUsesImportedStore()
    {
        using var temp = new TempDirectory();
        var existing = Directory.CreateDirectory(Path.Combine(temp.Path, "existing")).FullName;
        var (config, settings, _) = CreatePortableFixture(
            temp,
            Entry("one", "One", existing),
            Entry("two", "Two", existing));
        var importPath = Path.Combine(temp.Path, "import.json");
        LauncherStore.Save(
            new LauncherStoreFile
            {
                SchemaVersion = 2,
                Entries = [Entry("three", "Three", existing)],
                Presets = [],
                Settings = new LauncherSettings(),
                DefaultPresetId = "",
            },
            importPath,
            backup: false);
        var dialog = new StubDialogService
        {
            OnPickImportPath = () => Task.FromResult<string?>(importPath),
            OnRequestImport = (_, _) => Task.FromResult<LauncherImportPlan?>(new LauncherImportPlan
            {
                Mode = LauncherImportMode.Replace,
            }),
        };
        var viewModel = CreateViewModel(config, settings, dialogService: dialog);

        await viewModel.ImportConfigCommand.ExecuteAsync(null);

        Assert.Single(LauncherStore.Load(config).Entries);
        Assert.True(viewModel.Entries.Count == 1, viewModel.StatusContent.ToString());
        var entry = Assert.Single(viewModel.Entries);
        Assert.Equal("three", entry.Id);
    }
}
