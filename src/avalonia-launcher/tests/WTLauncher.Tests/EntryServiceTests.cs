using System.Collections.ObjectModel;
using WTLauncher.App.Features;
using WTLauncher.App.Features.Entries;
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
            new ObservableCollection<LauncherEntryViewModel>(),
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
            new ObservableCollection<LauncherEntryViewModel>(), new LauncherSettings(), null, 0, () => DateTimeOffset.UtcNow);
        Assert.IsType<ServiceCanceled<LaunchEntry>>(canceled);

        var expected = new IOException("dialog failed");
        var dialogs = new StubDialogService
        {
            OnRequestEntryDraft = (_, _, _) => Task.FromException<EntryDraft?>(expected),
        };
        var failedService = new EntryService(dialogs);
        var failed = await failedService.AddEntryAsync(
            new ObservableCollection<LauncherEntryViewModel>(), new LauncherSettings(), null, 0, () => DateTimeOffset.UtcNow);
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
        var owner = new MainWindowViewModel(
            "unused.json",
            new LauncherStoreFile(),
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
}
