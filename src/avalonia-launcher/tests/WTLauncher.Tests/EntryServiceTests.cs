using System.Collections.ObjectModel;
using WTLauncher.App.Features;
using WTLauncher.App.Features.Entries;
using WTLauncher.App.Infrastructure;
using WTLauncher.App.Features.Presets;
using WTLauncher.App.ViewModels;
using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed class EntryServiceTests
{
    [Fact]
    public async Task AddEntryAsync_ReturnsTimestampedEntry()
    {
        var timestamp = new DateTimeOffset(2026, 7, 13, 1, 2, 3, TimeSpan.Zero);
        var dialogs = new StubDialogService
        {
            OnRequestEntryDraft = (_, _, _) => Task.FromResult<EntryDraft?>(
                new EntryDraft("one", "One", "D:\\one", "pwsh")),
        };
        var service = new EntryService(dialogs);

        var result = await service.AddEntryAsync(
            [],
            new LauncherSettings(),
            commandTemplates: null,
            presetReferenceCount: 0,
            () => timestamp);

        var success = Assert.IsType<ServiceSuccess<LaunchEntry>>(result);
        Assert.Equal("one", success.Value.Id);
        Assert.Equal(timestamp.ToString("O"), success.Value.UpdatedAtUtc);
    }

    [Fact]
    public async Task AddEntryAsync_DistinguishesCancelFromFailure()
    {
        var canceledService = new EntryService(new StubDialogService());
        var canceled = await canceledService.AddEntryAsync(
            [], new LauncherSettings(), null, 0, () => DateTimeOffset.UtcNow);
        Assert.IsType<ServiceCanceled<LaunchEntry>>(canceled);

        var expected = new IOException("dialog failed");
        var dialogs = new StubDialogService
        {
            OnRequestEntryDraft = (_, _, _) => Task.FromException<EntryDraft?>(expected),
        };
        var failedService = new EntryService(dialogs);
        var failed = await failedService.AddEntryAsync(
            [], new LauncherSettings(), null, 0, () => DateTimeOffset.UtcNow);
        Assert.Equal(
            LauncherEntryStatus.AddDialogFailed(expected),
            Assert.IsType<ServiceFailure<LaunchEntry>>(failed).Status);
    }

    [Fact]
    public async Task DeleteEntryAsync_ReturnsTargetAndFocusedEntryId()
    {
        var dialogs = new StubDialogService
        {
            OnConfirmDeleteEntry = _ => Task.FromResult(true),
        };
        var service = new EntryService(dialogs);
        var appStore = new AppStore("unused.json", new LauncherStoreFile());
        var owner = new MainWindowViewModel(
            "unused.json",
            appStore,
            appStore.Select(s => s),
            new LauncherLaunchWorkflow(_ => { }),
            dialogs,
            service,
            new PresetService(dialogs),
            () => DateTimeOffset.UtcNow);
        var item = new LauncherEntryViewModel(
            new LaunchEntry { Id = "one", Name = "One", Directory = "D:\\one", Command = "pwsh" },
            owner);

        var result = await service.DeleteEntryAsync(item, "one");

        var approval = Assert.IsType<ServiceSuccess<EntryDeleteApproval>>(result).Value;
        Assert.Equal("one", approval.Target.EntryId);
        Assert.Equal("one", approval.ClearKeyboardFocusId);
    }

    [Fact]
    public async Task CloneEntryAsync_UsesUniquePrefillAndReturnsEditedClone()
    {
        using var temp = new TempDirectory();
        var owner = MainWindowEntryCardTestFactory.CreateOwner(temp.Path);
        var source = MainWindowEntryCardTestFactory.CreateEntryViewModel(owner, "one");
        LaunchEntry? prefill = null;
        var dialogs = new StubDialogService
        {
            OnRequestEntryDraft = (entry, _, _) =>
            {
                prefill = entry;
                return Task.FromResult<EntryDraft?>(new EntryDraft("one-copy-2", "副本", "D:\\copy", "pwsh"));
            },
        };
        var service = new EntryService(dialogs);
        var entries = new ObservableCollection<LauncherEntryViewModel>
        {
            source,
            MainWindowEntryCardTestFactory.CreateEntryViewModel(owner, "one-copy"),
        };

        var result = await service.CloneEntryAsync(
            source, entries.Select(e => e.Id), entries.Select(e => e.SnapshotEntry()),
            new LauncherSettings(), null, 0, () => DateTimeOffset.UnixEpoch);

        Assert.Equal("one-copy-2", prefill?.Id);
        var clone = Assert.IsType<ServiceSuccess<LaunchEntry>>(result).Value;
        Assert.Equal("副本", clone.Name);
        Assert.False(clone.Selected);
    }

    [Fact]
    public async Task EditEntryAsync_DistinguishesCancelFromDialogFailure()
    {
        using var temp = new TempDirectory();
        var owner = MainWindowEntryCardTestFactory.CreateOwner(temp.Path);
        var item = MainWindowEntryCardTestFactory.CreateEntryViewModel(owner, "one");
        var entries = new ObservableCollection<LauncherEntryViewModel> { item };
        var canceled = await new EntryService(new StubDialogService()).EditEntryAsync(
            item, entries.Select(e => e.SnapshotEntry()), new LauncherPresetFile(), new LauncherSettings(), null, 0, () => DateTimeOffset.UtcNow);
        Assert.IsType<ServiceCanceled<LaunchEntry>>(canceled);

        var expected = new IOException("dialog failed");
        var dialogs = new StubDialogService
        {
            OnRequestEntryDraft = (_, _, _) => Task.FromException<EntryDraft?>(expected),
        };
        var failed = await new EntryService(dialogs).EditEntryAsync(
            item, entries.Select(e => e.SnapshotEntry()), new LauncherPresetFile(), new LauncherSettings(), null, 0, () => DateTimeOffset.UtcNow);
        Assert.Equal(
            LauncherEntryStatus.EditDialogFailed(item.Name, expected),
            Assert.IsType<ServiceFailure<LaunchEntry>>(failed).Status);
    }

    [Fact]
    public async Task DeleteEntryAsync_DistinguishesCancelFromDialogFailure()
    {
        using var temp = new TempDirectory();
        var owner = MainWindowEntryCardTestFactory.CreateOwner(temp.Path);
        var item = MainWindowEntryCardTestFactory.CreateEntryViewModel(owner, "one");
        var canceled = await new EntryService(new StubDialogService()).DeleteEntryAsync(item, null);
        Assert.Equal(
            LauncherEntryStatus.DeleteCanceled(item.Name),
            Assert.IsType<ServiceCanceled<EntryDeleteApproval>>(canceled).Status);

        var expected = new IOException("dialog failed");
        var dialogs = new StubDialogService
        {
            OnConfirmDeleteEntry = _ => Task.FromException<bool>(expected),
        };
        var failed = await new EntryService(dialogs).DeleteEntryAsync(item, null);
        Assert.Equal(
            LauncherEntryStatus.DeleteConfirmationFailed(item.Name, expected),
            Assert.IsType<ServiceFailure<EntryDeleteApproval>>(failed).Status);
    }

    [Fact]
    public void CreateEntryFromDraft_ReturnsValidationFailureWithoutPayload()
    {
        var service = new EntryService(new StubDialogService());
        var existing = new LaunchEntry { Id = "one", Name = "One", Directory = "D:\\one", Command = "pwsh" };

        var result = service.CreateEntryFromDraft(
            new EntryDraft("one", "Duplicate", "D:\\duplicate", "pwsh"),
            originalId: null,
            previousEntry: null,
            [existing]);

        Assert.IsType<ServiceFailure<LaunchEntry>>(result);
    }

    [Fact]
    public void GenerateUniqueCloneId_UsesFirstAvailableSuffix()
    {
        var service = new EntryService(new StubDialogService());

        var result = service.GenerateUniqueCloneId(" Demo ", ["Demo-copy", "Demo-copy-2"]);

        Assert.Equal("Demo-copy-3", result);
    }
}
