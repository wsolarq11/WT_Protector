using WTLauncher.App.ViewModels;
using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed class MainWindowViewModelTests
{
    [Fact]
    public void BatchSelectionCommandsPersistPortableConfig()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreatePortableFixture(temp, oneSelected: false, twoSelected: false);

        var viewModel = new MainWindowViewModel(config, settings, new LauncherSettings(), new LauncherProcess());

        Assert.Equal(0, viewModel.SelectedCount);

        viewModel.SelectVisibleCommand.Execute(null);

        Assert.Equal(2, viewModel.SelectedCount);
        Assert.All(LauncherConfigStore.Load(config), entry => Assert.True(entry.Selected));

        viewModel.ClearSelectionCommand.Execute(null);

        Assert.Equal(0, viewModel.SelectedCount);
        Assert.All(LauncherConfigStore.Load(config), entry => Assert.False(entry.Selected));
        Assert.True(File.Exists(config + ".bak"));
    }

    [Fact]
    public void FilterTextPersistsPortableSettingsAndNarrowsVisibleEntries()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreatePortableFixture(temp, oneSelected: false, twoSelected: false);
        var viewModel = new MainWindowViewModel(config, settings, new LauncherSettings(), new LauncherProcess());

        Assert.Equal(2, viewModel.VisibleCount);

        viewModel.FilterText = "codex";

        var visible = Assert.Single(viewModel.VisibleEntries);
        Assert.Equal("one", visible.Entry.Id);
        Assert.Equal("codex", LauncherSettingsStore.Load(settings).LastFilterText);
    }

    [Fact]
    public void ApplyPresetCommandReplacesAndPersistsPortableSelection()
    {
        using var temp = new TempDirectory();
        var (config, settings, presets) = CreatePortableFixture(temp, oneSelected: false, twoSelected: true);
        File.WriteAllText(presets, """
        {
          "schemaVersion": 1,
          "defaultPresetId": "daily",
          "presets": [
            {
              "id": "daily",
              "name": "Daily",
              "description": "demo preset",
              "entryIds": ["one"]
            }
          ]
        }
        """);

        var viewModel = new MainWindowViewModel(config, settings, new LauncherSettings(), new LauncherProcess());

        Assert.Equal("daily", Assert.Single(viewModel.Presets).Id);
        Assert.Equal(1, viewModel.PresetCount);
        Assert.Equal("daily", viewModel.SelectedPreset?.Id);
        Assert.True(viewModel.ApplyPresetCommand.CanExecute(null));
        Assert.Equal("应用启动组", viewModel.ApplyPresetButtonText);
        Assert.Contains("缺 1", viewModel.PresetSelectionStatusText);
        Assert.Contains("额外 1", viewModel.PresetSelectionStatusText);

        viewModel.ApplyPresetCommand.Execute(null);

        Assert.Equal(1, viewModel.SelectedCount);
        Assert.False(viewModel.ApplyPresetCommand.CanExecute(null));
        Assert.Equal("已匹配", viewModel.ApplyPresetButtonText);
        Assert.Contains("已匹配启动组", viewModel.PresetSelectionStatusText);
        Assert.Contains("1/1", viewModel.PresetSelectionStatusText);
        var entries = LauncherConfigStore.Load(config).OrderBy(entry => entry.Id).ToList();
        Assert.True(entries.Single(entry => entry.Id == "one").Selected);
        Assert.False(entries.Single(entry => entry.Id == "two").Selected);
    }

    [Fact]
    public void BrokenPresetWarnsAndDoesNotRewriteSelection()
    {
        using var temp = new TempDirectory();
        var (config, settings, presets) = CreatePortableFixture(temp, oneSelected: false, twoSelected: true);
        File.WriteAllText(presets, """
        {
          "schemaVersion": 1,
          "defaultPresetId": "broken",
          "presets": [
            {
              "id": "broken",
              "name": "Broken",
              "description": "references a retired config id",
              "entryIds": ["one", "missing-entry"]
            }
          ]
        }
        """);

        var viewModel = new MainWindowViewModel(config, settings, new LauncherSettings(), new LauncherProcess());

        Assert.Equal("检查启动组", viewModel.ApplyPresetButtonText);
        Assert.Contains("引用了 1 个不存在的启动项", viewModel.PresetSelectionStatusText);

        viewModel.ApplyPresetCommand.Execute(null);

        Assert.Contains("missing-entry", viewModel.StatusText);
        Assert.False(File.Exists(config + ".bak"));
        var entries = LauncherConfigStore.Load(config).OrderBy(entry => entry.Id).ToList();
        Assert.False(entries.Single(entry => entry.Id == "one").Selected);
        Assert.True(entries.Single(entry => entry.Id == "two").Selected);
    }

    [Fact]
    public async Task LaunchGroupCommandsCreateUpdateAndDeletePortablePresets()
    {
        using var temp = new TempDirectory();
        var (config, settings, presets) = CreatePortableFixture(temp, oneSelected: true, twoSelected: false);
        File.WriteAllText(presets, """
        {
          "schemaVersion": 1,
          "defaultPresetId": "daily",
          "presets": [
            {
              "id": "daily",
              "name": "Daily",
              "description": "demo preset",
              "entryIds": ["one"]
            },
            {
              "id": "blank-manual",
              "name": "Manual",
              "description": "locked empty preset",
              "entryIds": [],
              "locked": true
            }
          ]
        }
        """);

        var viewModel = new MainWindowViewModel(config, settings, new LauncherSettings(), new LauncherProcess())
        {
            RequestNewPresetAsync = () => Task.FromResult<PresetDraft?>(new PresetDraft("Project Pair")),
        };

        Assert.True(viewModel.SaveSelectionAsPresetCommand.CanExecute(null));

        await viewModel.SaveSelectionAsPresetCommand.ExecuteAsync(null);

        Assert.Equal("project-pair", viewModel.SelectedPreset?.Id);
        var presetFile = LauncherPresetStore.Load(presets);
        var created = presetFile.Presets.Single(preset => preset.Id == "project-pair");
        Assert.Equal("Project Pair", created.Name);
        Assert.Equal(["one"], created.EntryIds);
        Assert.False(created.Locked);
        Assert.False(viewModel.ApplyPresetCommand.CanExecute(null));

        viewModel.Entries.Single(entry => entry.Entry.Id == "two").IsSelected = true;

        Assert.True(viewModel.UpdateSelectedPresetCommand.CanExecute(null));

        viewModel.UpdateSelectedPresetCommand.Execute(null);

        presetFile = LauncherPresetStore.Load(presets);
        created = presetFile.Presets.Single(preset => preset.Id == "project-pair");
        Assert.Equal(["one", "two"], created.EntryIds);
        Assert.False(viewModel.UpdateSelectedPresetCommand.CanExecute(null));

        viewModel.ConfirmDeletePresetAsync = preset => Task.FromResult(preset.Id == "project-pair");

        Assert.True(viewModel.DeleteSelectedPresetCommand.CanExecute(null));

        await viewModel.DeleteSelectedPresetCommand.ExecuteAsync(null);

        presetFile = LauncherPresetStore.Load(presets);
        Assert.DoesNotContain(presetFile.Presets, preset => preset.Id == "project-pair");
        Assert.Contains(presetFile.Presets, preset => preset.Id == "daily");

        viewModel.SelectedPreset = viewModel.Presets.Single(preset => preset.Id == "blank-manual");

        Assert.True(viewModel.SelectedPreset.IsLocked);
        Assert.False(viewModel.UpdateSelectedPresetCommand.CanExecute(null));
        Assert.False(viewModel.DeleteSelectedPresetCommand.CanExecute(null));
        Assert.True(viewModel.ApplyPresetCommand.CanExecute(null));
    }

    [Fact]
    public async Task EntryCommandsPersistPortableConfigAndReferences()
    {
        using var temp = new TempDirectory();
        var (config, settings, presets) = CreatePortableFixture(temp, oneSelected: false, twoSelected: false);
        File.WriteAllText(presets, """
        {
          "schemaVersion": 1,
          "defaultPresetId": "daily",
          "presets": [
            {
              "id": "daily",
              "name": "Daily",
              "entryIds": ["one"]
            }
          ]
        }
        """);

        var requestCount = 0;
        var viewModel = new MainWindowViewModel(
            config,
            settings,
            new LauncherSettings { SortMode = "custom", CustomEntryOrder = ["one", "two"] },
            new LauncherProcess())
        {
            RequestEntryDraftAsync = _ => Task.FromResult<EntryDraft?>(
                requestCount++ == 0
                    ? new EntryDraft("three", "Three", "D:\\three", "pwsh", false, true)
                    : new EntryDraft("one-renamed", "One Renamed", "D:\\renamed", "opencode", true, false)),
            ConfirmDeleteEntryAsync = _ => Task.FromResult(true),
        };

        await viewModel.AddEntryCommand.ExecuteAsync(null);

        var savedEntries = LauncherConfigStore.Load(config).OrderBy(entry => entry.Id).ToList();
        var added = savedEntries.Single(entry => entry.Id == "three");
        Assert.True(added.Selected);
        Assert.False(string.IsNullOrWhiteSpace(added.UpdatedAtUtc));
        var savedSettings = LauncherSettingsStore.Load(settings);
        Assert.Equal(["one", "two", "three"], savedSettings.CustomEntryOrder);

        await viewModel.Entries.Single(entry => entry.Id == "one").EditCommand.ExecuteAsync(null);

        savedEntries = LauncherConfigStore.Load(config).OrderBy(entry => entry.Id).ToList();
        Assert.DoesNotContain(savedEntries, entry => entry.Id == "one");
        var edited = savedEntries.Single(entry => entry.Id == "one-renamed");
        Assert.Equal("One Renamed", edited.Name);
        Assert.Equal("D:\\renamed", edited.Directory);
        Assert.Equal("opencode", edited.Command);
        Assert.True(edited.Confirm);
        Assert.False(edited.Selected);
        Assert.False(string.IsNullOrWhiteSpace(edited.UpdatedAtUtc));

        var savedPresetFile = LauncherPresetStore.Load(presets);
        Assert.Equal(["one-renamed"], savedPresetFile.Presets.Single(preset => preset.Id == "daily").EntryIds);
        savedSettings = LauncherSettingsStore.Load(settings);
        Assert.Equal(["one-renamed", "two", "three"], savedSettings.CustomEntryOrder);

        await viewModel.Entries.Single(entry => entry.Id == "one-renamed").DeleteCommand.ExecuteAsync(null);

        savedEntries = LauncherConfigStore.Load(config).OrderBy(entry => entry.Id).ToList();
        Assert.DoesNotContain(savedEntries, entry => entry.Id == "one-renamed");
        savedPresetFile = LauncherPresetStore.Load(presets);
        Assert.Empty(savedPresetFile.Presets.Single(preset => preset.Id == "daily").EntryIds);
        savedSettings = LauncherSettingsStore.Load(settings);
        Assert.Equal(["two", "three"], savedSettings.CustomEntryOrder);
    }

    [Fact]
    public void SortModesAndCustomDragOrderPersistPortableSettings()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreatePortableFixture(
            temp,
            new LaunchEntry
            {
                Id = "one",
                Name = "Zulu",
                Directory = "D:\\one",
                Command = "codex",
                Confirm = true,
                UpdatedAtUtc = "2026-05-17T00:00:00Z",
            },
            new LaunchEntry
            {
                Id = "two",
                Name = "Alpha",
                Directory = "D:\\two",
                Command = "opencode",
                Confirm = false,
                UpdatedAtUtc = "2026-05-18T00:00:00Z",
            },
            new LaunchEntry
            {
                Id = "three",
                Name = "Mike",
                Directory = "D:\\three",
                Command = "pwsh",
                Confirm = false,
                UpdatedAtUtc = "",
            });

        var viewModel = new MainWindowViewModel(
            config,
            settings,
            new LauncherSettings { SortMode = "custom", CustomEntryOrder = ["three", "one", "two"] },
            new LauncherProcess());

        Assert.Equal(["three", "one", "two"], viewModel.VisibleEntries.Select(entry => entry.Id).ToArray());

        viewModel.MoveEntryBefore("two", "three");

        Assert.Equal(["two", "three", "one"], viewModel.VisibleEntries.Select(entry => entry.Id).ToArray());
        var savedSettings = LauncherSettingsStore.Load(settings);
        Assert.Equal("custom", savedSettings.SortMode);
        Assert.Equal(["two", "three", "one"], savedSettings.CustomEntryOrder);

        viewModel.SelectedSortMode = viewModel.SortModes.Single(mode => mode.Id == "alphabetical");

        Assert.Equal(["two", "three", "one"], viewModel.VisibleEntries.Select(entry => entry.Id).ToArray());
        savedSettings = LauncherSettingsStore.Load(settings);
        Assert.Equal("alphabetical", savedSettings.SortMode);
        Assert.Equal(["two", "three", "one"], savedSettings.CustomEntryOrder);

        viewModel.SelectedSortMode = viewModel.SortModes.Single(mode => mode.Id == "updated");

        Assert.Equal(["two", "one", "three"], viewModel.VisibleEntries.Select(entry => entry.Id).ToArray());
        savedSettings = LauncherSettingsStore.Load(settings);
        Assert.Equal("updated", savedSettings.SortMode);
    }

    [Fact]
    public void DragPreviewReordersInMemoryAndCanBeCancelledOrCommitted()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreatePortableFixture(
            temp,
            new LaunchEntry
            {
                Id = "one",
                Name = "One",
                Directory = "D:\\one",
                Command = "codex",
                Confirm = false,
            },
            new LaunchEntry
            {
                Id = "two",
                Name = "Two",
                Directory = "D:\\two",
                Command = "opencode",
                Confirm = false,
            },
            new LaunchEntry
            {
                Id = "three",
                Name = "Three",
                Directory = "D:\\three",
                Command = "pwsh",
                Confirm = false,
            });

        var viewModel = new MainWindowViewModel(
            config,
            settings,
            new LauncherSettings { SortMode = "custom", CustomEntryOrder = ["one", "two", "three"] },
            new LauncherProcess());
        LauncherSettingsStore.Save(new LauncherSettings
        {
            SortMode = "custom",
            CustomEntryOrder = ["one", "two", "three"],
        }, settings);

        Assert.Equal(["one", "two", "three"], viewModel.VisibleEntries.Select(entry => entry.Id).ToArray());

        viewModel.BeginDragReorder("three");
        viewModel.PreviewMoveEntry("three", "one", insertAfter: false);

        Assert.Equal(["three", "one", "two"], viewModel.VisibleEntries.Select(entry => entry.Id).ToArray());
        Assert.True(viewModel.Entries.Single(entry => entry.Id == "three").IsDragPreviewSource);
        var target = viewModel.Entries.Single(entry => entry.Id == "one");
        Assert.True(target.IsDragPreviewTarget);
        Assert.True(target.IsDragInsertBefore);
        Assert.False(target.IsDragInsertAfter);
        Assert.Equal(["one", "two", "three"], LauncherSettingsStore.Load(settings).CustomEntryOrder);

        viewModel.CancelDragReorder();

        Assert.Equal(["one", "two", "three"], viewModel.VisibleEntries.Select(entry => entry.Id).ToArray());
        Assert.DoesNotContain(viewModel.Entries, entry =>
            entry.IsDragPreviewSource
            || entry.IsDragPreviewTarget
            || entry.IsDragInsertBefore
            || entry.IsDragInsertAfter);

        viewModel.BeginDragReorder("three");
        viewModel.PreviewMoveEntry("three", "one", insertAfter: false);
        viewModel.CommitDragReorder();

        Assert.Equal(["three", "one", "two"], viewModel.VisibleEntries.Select(entry => entry.Id).ToArray());
        Assert.Equal(["three", "one", "two"], LauncherSettingsStore.Load(settings).CustomEntryOrder);
    }

    [Fact]
    public void CardLayoutDensitySwitchesBetweenNormalCompactAndMicro()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreatePortableFixture(
            temp,
            new LaunchEntry
            {
                Id = "one",
                Name = "One",
                Directory = "D:\\one",
                Command = "codex",
                Confirm = false,
            });

        var viewModel = new MainWindowViewModel(config, settings, new LauncherSettings(), new LauncherProcess());
        var entry = Assert.Single(viewModel.Entries);

        entry.CardWidth = 210;
        entry.CardHeight = 90;
        Assert.True(entry.IsMicroLayout);
        Assert.True(entry.IsCompactLayout);
        Assert.False(entry.ShowDirectory);
        Assert.False(entry.ShowCommand);
        Assert.False(entry.ShowUpdatedLabel);
        Assert.Equal(15, entry.TitleFontSize);

        entry.CardWidth = 240;
        entry.CardHeight = 110;
        Assert.False(entry.IsMicroLayout);
        Assert.True(entry.IsCompactLayout);
        Assert.True(entry.ShowDirectory);
        Assert.False(entry.ShowCommand);
        Assert.True(entry.ShowUpdatedLabel);
        Assert.Equal(16, entry.TitleFontSize);

        entry.CardWidth = 280;
        entry.CardHeight = 140;
        Assert.False(entry.IsMicroLayout);
        Assert.False(entry.IsCompactLayout);
        Assert.True(entry.ShowDirectory);
        Assert.True(entry.ShowCommand);
        Assert.True(entry.ShowUpdatedLabel);
        Assert.Equal(18, entry.TitleFontSize);
    }

    [Fact]
    public void CardSizingShrinksButtonsToFitVisibleEntriesInsideWindowRegion()
    {
        using var temp = new TempDirectory();
        var entries = Enumerable.Range(1, 6)
            .Select(index => new LaunchEntry
            {
                Id = $"entry-{index}",
                Name = $"Entry {index}",
                Directory = $"D:\\entry-{index}",
                Command = "pwsh",
                Confirm = false,
            })
            .ToArray();
        var (config, settings, _) = CreatePortableFixture(temp, entries);
        var viewModel = new MainWindowViewModel(config, settings, new LauncherSettings(), new LauncherProcess());

        viewModel.UpdateCardsAvailableSize(640, 260);

        var first = Assert.IsType<LauncherEntryViewModel>(viewModel.VisibleEntries.First());
        Assert.True(first.CardWidth < 300, "expected card width to shrink below the default width");
        Assert.True(first.CardHeight < 164, "expected card height to shrink below the default height");
        Assert.All(viewModel.VisibleEntries, entry =>
        {
            Assert.Equal(first.CardWidth, entry.CardWidth);
            Assert.Equal(first.CardHeight, entry.CardHeight);
        });

        const double gap = 10;
        var columns = Math.Max(1, (int)Math.Floor((640 + gap) / (first.CardWidth + gap)));
        var rows = (int)Math.Ceiling(viewModel.VisibleEntries.Count / (double)columns);
        var usedWidth = (columns * first.CardWidth) + ((columns - 1) * gap);
        var usedHeight = (rows * first.CardHeight) + ((rows - 1) * gap);
        Assert.True(usedWidth <= 640.001, $"expected cards to fit available width, got {usedWidth}");
        Assert.True(usedHeight <= 260.001, $"expected cards to fit available height, got {usedHeight}");
    }

    [Fact]
    public void CardSizingPrefersScrollFallbackBeforeShrinkingBelowReadableMinimum()
    {
        using var temp = new TempDirectory();
        var entries = Enumerable.Range(1, 19)
            .Select(index => new LaunchEntry
            {
                Id = $"entry-{index}",
                Name = $"Entry {index}",
                Directory = $"D:\\entry-{index}",
                Command = "pwsh",
                Confirm = false,
            })
            .ToArray();
        var (config, settings, _) = CreatePortableFixture(temp, entries);
        var viewModel = new MainWindowViewModel(config, settings, new LauncherSettings(), new LauncherProcess());

        viewModel.UpdateCardsAvailableSize(892, 330);

        var first = Assert.IsType<LauncherEntryViewModel>(viewModel.VisibleEntries.First());
        Assert.True(first.CardHeight >= 96, "expected card height to stop shrinking before actions become unreachable; scrolling is the fallback");

        const double gap = 10;
        var columns = Math.Max(1, (int)Math.Floor((892 + gap) / (first.CardWidth + gap)));
        var rows = (int)Math.Ceiling(viewModel.VisibleEntries.Count / (double)columns);
        var usedWidth = (columns * first.CardWidth) + ((columns - 1) * gap);
        var usedHeight = (rows * first.CardHeight) + ((rows - 1) * gap);
        Assert.True(usedWidth <= 892.001, $"expected cards to fit minimum-window width, got {usedWidth}");
        Assert.True(usedHeight > 330.001, $"expected min-window height to overflow so the ScrollViewer can act as fallback, got {usedHeight}");
    }

    private static (string Config, string Settings, string Presets) CreatePortableFixture(
        TempDirectory temp,
        bool oneSelected,
        bool twoSelected)
    {
        var configDir = Path.Combine(temp.Path, "config");
        Directory.CreateDirectory(configDir);
        var config = Path.Combine(configDir, "config.json");
        WriteTwoEntryConfig(config, oneSelected, twoSelected);
        return (config, Path.Combine(configDir, "settings.json"), Path.Combine(configDir, "presets.json"));
    }

    private static (string Config, string Settings, string Presets) CreatePortableFixture(
        TempDirectory temp,
        params LaunchEntry[] entries)
    {
        var configDir = Path.Combine(temp.Path, "config");
        Directory.CreateDirectory(configDir);
        var config = Path.Combine(configDir, "config.json");
        LauncherConfigStore.Save(entries, config);
        return (config, Path.Combine(configDir, "settings.json"), Path.Combine(configDir, "presets.json"));
    }

    private static void WriteTwoEntryConfig(string config, bool oneSelected, bool twoSelected)
    {
        File.WriteAllText(config, $$"""
        [
          {
            "id": "one",
            "name": "one",
            "directory": "D:\\one",
            "command": "codex",
            "confirm": true,
            "selected": {{JsonBool(oneSelected)}}
          },
          {
            "id": "two",
            "name": "two",
            "directory": "D:\\two",
            "command": "opencode",
            "confirm": false,
            "selected": {{JsonBool(twoSelected)}}
          }
        ]
        """);
    }

    private static string JsonBool(bool value)
    {
        return value ? "true" : "false";
    }
}
