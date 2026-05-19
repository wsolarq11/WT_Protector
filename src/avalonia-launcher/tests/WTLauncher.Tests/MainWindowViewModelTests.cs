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
        Assert.All(LauncherConfigStore.Load(config + ".bak"), entry => Assert.False(entry.Selected));

        viewModel.ClearSelectionCommand.Execute(null);

        Assert.Equal(0, viewModel.SelectedCount);
        Assert.All(LauncherConfigStore.Load(config), entry => Assert.False(entry.Selected));
        Assert.All(LauncherConfigStore.Load(config + ".bak"), entry => Assert.True(entry.Selected));
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
    public void GlobalConfirmationSwitchPersistsAndAppliesUniformlyToAllEntries()
    {
        using var temp = new TempDirectory();
        var entries = new[]
        {
            new LaunchEntry
            {
                Id = "one",
                Name = "one",
                Directory = temp.Path,
                Command = "codex --dangerously-bypass-approvals-and-sandbox",
                Confirm = true,
            },
            new LaunchEntry
            {
                Id = "two",
                Name = "two",
                Directory = temp.Path,
                Command = "pwsh",
                Confirm = false,
            },
        };
        var (config, settings, _) = CreatePortableFixture(temp, entries);

        var viewModel = new MainWindowViewModel(
            config,
            settings,
            new LauncherSettings { RequireConfirmationForLaunches = false },
            new LauncherProcess());

        Assert.False(viewModel.RequireConfirmationForLaunches);
        viewModel.Entries.Single(entry => entry.Id == "one").DryRunCommand.Execute(null);
        Assert.Equal("预演: one", viewModel.StatusText);

        viewModel.RequireConfirmationForLaunches = true;

        Assert.True(LauncherSettingsStore.Load(settings).RequireConfirmationForLaunches);
        viewModel.Entries.Single(entry => entry.Id == "one").DryRunCommand.Execute(null);
        Assert.Equal("预演: one（需要确认）", viewModel.StatusText);
        viewModel.Entries.Single(entry => entry.Id == "two").DryRunCommand.Execute(null);
        Assert.Equal("预演: two（需要确认）", viewModel.StatusText);

        viewModel.FilterText = "two";

        Assert.Equal(1, viewModel.VisibleCount);
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
                    ? new EntryDraft("three", "Three", "D:\\three", "pwsh", true)
                    : new EntryDraft("one-renamed", "One Renamed", "D:\\renamed", "opencode", false)),
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
        Assert.False(edited.Confirm);
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
    public void SortModeUsesSelectedModeAsOnlyCustomSortState()
    {
        Assert.Null(typeof(MainWindowViewModel).GetProperty(nameof(MainWindowViewModel.IsCustomSortMode))?.SetMethod);
        Assert.DoesNotContain(
            typeof(MainWindowViewModel).GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance),
            field => field.Name.Contains("CustomSort", StringComparison.OrdinalIgnoreCase));

        using var temp = new TempDirectory();
        var (config, settings, _) = CreatePortableFixture(temp, oneSelected: false, twoSelected: false);
        var viewModel = new MainWindowViewModel(
            config,
            settings,
            new LauncherSettings { SortMode = "custom" },
            new LauncherProcess());

        Assert.True(viewModel.IsCustomSortMode);

        viewModel.SelectedSortMode = viewModel.SortModes.Single(mode => mode.Id == "alphabetical");

        Assert.False(viewModel.IsCustomSortMode);
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
        Assert.True(viewModel.PreviewMoveEntry("three", "one", insertAfter: false));
        Assert.False(viewModel.PreviewMoveEntry("three", "one", insertAfter: false));

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
        Assert.True(viewModel.PreviewMoveEntry("three", "one", insertAfter: false));
        viewModel.CommitDragReorder();

        Assert.Equal(["three", "one", "two"], viewModel.VisibleEntries.Select(entry => entry.Id).ToArray());
        Assert.Equal(["three", "one", "two"], LauncherSettingsStore.Load(settings).CustomEntryOrder);
    }

    [Fact]
    public void DragPreviewStateUsesOneTypedSessionField()
    {
        var dragFields = typeof(MainWindowViewModel)
            .GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .Where(field => field.Name.Contains("Drag", StringComparison.OrdinalIgnoreCase))
            .Select(field => field.Name)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(["_dragReorderSession"], dragFields);
    }

    [Fact]
    public void EntryDragCueStateUsesOneTypedStateSource()
    {
        var dragFields = typeof(LauncherEntryViewModel)
            .GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .Where(field => field.Name.Contains("Drag", StringComparison.OrdinalIgnoreCase))
            .Select(field => field.Name)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(["_dragCueState"], dragFields);
        Assert.Null(typeof(LauncherEntryViewModel).GetProperty(nameof(LauncherEntryViewModel.IsDragPreviewTarget))?.SetMethod);
        Assert.Null(typeof(LauncherEntryViewModel).GetProperty(nameof(LauncherEntryViewModel.IsDragPreviewSource))?.SetMethod);
        Assert.Null(typeof(LauncherEntryViewModel).GetProperty(nameof(LauncherEntryViewModel.IsDragInsertBefore))?.SetMethod);
        Assert.Null(typeof(LauncherEntryViewModel).GetProperty(nameof(LauncherEntryViewModel.IsDragInsertAfter))?.SetMethod);
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
        Assert.Equal(15, entry.TitleFontSize);

        entry.CardWidth = 240;
        entry.CardHeight = 110;
        Assert.False(entry.IsMicroLayout);
        Assert.True(entry.IsCompactLayout);
        Assert.True(entry.ShowDirectory);
        Assert.False(entry.ShowCommand);
        Assert.Equal(16, entry.TitleFontSize);

        entry.CardWidth = 280;
        entry.CardHeight = 140;
        Assert.False(entry.IsMicroLayout);
        Assert.False(entry.IsCompactLayout);
        Assert.True(entry.ShowDirectory);
        Assert.True(entry.ShowCommand);
        Assert.Equal(18, entry.TitleFontSize);
    }

    [Fact]
    public void CardLayoutDensityUsesOneTypedStateSource()
    {
        var densityFields = typeof(LauncherEntryViewModel)
            .GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .Where(field =>
                field.Name.Contains("Layout", StringComparison.OrdinalIgnoreCase)
                || field.Name.Contains("Density", StringComparison.OrdinalIgnoreCase)
                || field.Name.Contains("Compact", StringComparison.OrdinalIgnoreCase)
                || field.Name.Contains("Micro", StringComparison.OrdinalIgnoreCase))
            .Select(field => field.Name)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(["_layoutDensity"], densityFields);
        Assert.Null(typeof(LauncherEntryViewModel).GetProperty(nameof(LauncherEntryViewModel.IsCompactLayout))?.SetMethod);
        Assert.Null(typeof(LauncherEntryViewModel).GetProperty(nameof(LauncherEntryViewModel.IsMicroLayout))?.SetMethod);
    }

    [Fact]
    public void EntryViewModePersistsAndOverridesCardDensityForDetailedScanning()
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
        Assert.False(viewModel.IsDetailedListMode);
        Assert.True(viewModel.IsCardsViewMode);
        Assert.False(entry.ShowDirectory);
        Assert.False(entry.ShowCommand);
        Assert.Equal(15, entry.TitleFontSize);

        viewModel.SelectedDisplayMode = viewModel.DisplayModes.Single(mode => mode.Id == "details");

        Assert.True(viewModel.IsDetailedListMode);
        Assert.False(viewModel.IsNamesViewMode);
        Assert.False(viewModel.IsCardsViewMode);
        Assert.True(entry.ShowDirectory);
        Assert.True(entry.ShowCommand);
        Assert.Equal(13, entry.TitleFontSize);
        Assert.Equal("details", LauncherSettingsStore.Load(settings).EntryViewMode);

        viewModel.SelectedDisplayMode = viewModel.DisplayModes.Single(mode => mode.Id == "names");

        Assert.False(viewModel.IsDetailedListMode);
        Assert.True(viewModel.IsNamesViewMode);
        Assert.False(viewModel.IsCardsViewMode);
        Assert.False(entry.ShowDirectory);
        Assert.False(entry.ShowCommand);
        Assert.Equal(13, entry.TitleFontSize);
        Assert.Equal("names", LauncherSettingsStore.Load(settings).EntryViewMode);

        viewModel.SelectedDisplayMode = viewModel.DisplayModes.Single(mode => mode.Id == "cards");

        Assert.False(viewModel.IsDetailedListMode);
        Assert.False(viewModel.IsNamesViewMode);
        Assert.True(viewModel.IsCardsViewMode);
        Assert.False(entry.ShowDirectory);
        Assert.False(entry.ShowCommand);
        Assert.Equal(15, entry.TitleFontSize);
        Assert.Equal("cards", LauncherSettingsStore.Load(settings).EntryViewMode);
    }

    [Fact]
    public void EntryViewModeUsesOneTypedStateSource()
    {
        Assert.Null(typeof(MainWindowViewModel).GetProperty(nameof(MainWindowViewModel.IsCardsViewMode))?.SetMethod);
        Assert.Null(typeof(MainWindowViewModel).GetProperty(nameof(MainWindowViewModel.IsDetailedListMode))?.SetMethod);
        Assert.Null(typeof(MainWindowViewModel).GetProperty(nameof(MainWindowViewModel.IsNamesViewMode))?.SetMethod);
        Assert.DoesNotContain(
            typeof(MainWindowViewModel).GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance),
            field => field.Name.Contains("Refreshing", StringComparison.OrdinalIgnoreCase));

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

        var viewModel = new MainWindowViewModel(
            config,
            settings,
            new LauncherSettings { EntryViewMode = "details" },
            new LauncherProcess());

        Assert.Equal(new[] { "卡片", "详细列表", "名称墙" }, viewModel.DisplayModes.Select(mode => mode.Label));
        Assert.Equal(LauncherEntryViewMode.Details, viewModel.SelectedDisplayMode?.Mode);
        Assert.Equal("details", viewModel.SelectedDisplayMode?.Id);
        Assert.Equal(
            1,
            new[] { viewModel.IsCardsViewMode, viewModel.IsDetailedListMode, viewModel.IsNamesViewMode }.Count(value => value));
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

        const double gap = 8;
        var columns = Math.Max(1, (int)Math.Floor((640 + gap) / (first.CardWidth + gap)));
        var rows = (int)Math.Ceiling(viewModel.VisibleEntries.Count / (double)columns);
        var usedWidth = (columns * first.CardWidth) + ((columns - 1) * gap);
        var usedHeight = (rows * first.CardHeight) + ((rows - 1) * gap);
        Assert.True(usedWidth <= 640.001, $"expected cards to fit available width, got {usedWidth}");
        Assert.True(usedHeight <= 260.001, $"expected cards to fit available height, got {usedHeight}");
    }

    [Fact]
    public void CardSizingUsesGoldenRatioWhenSpaceAllows()
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

        viewModel.UpdateCardsAvailableSize(900, 520);

        var first = Assert.IsType<LauncherEntryViewModel>(viewModel.VisibleEntries.First());
        const double goldenRatio = 21d / 13d;
        var actualRatio = first.CardWidth / first.CardHeight;
        Assert.InRange(actualRatio, goldenRatio - 0.02, goldenRatio + 0.02);
    }

    [Fact]
    public void NameWallSizingUsesAdaptiveMultiColumnTiles()
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
        Assert.Equal(24, first.NameTileHeight);
        Assert.InRange(first.NameTileWidth, 132, 220);

        const double gap = 6;
        const double rowGap = 4;
        var columns = Math.Max(1, (int)Math.Floor((892 + gap) / (first.NameTileWidth + gap)));
        var rows = (int)Math.Ceiling(viewModel.VisibleEntries.Count / (double)columns);
        var usedHeight = (rows * first.NameTileHeight) + ((rows - 1) * rowGap);
        Assert.True(columns >= 4, $"expected name wall to adapt to multiple columns, got {columns}");
        Assert.True(usedHeight <= 330.001, $"expected name wall to fit all 19 root-scale items in the available height, got {usedHeight}");
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

        const double gap = 8;
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
