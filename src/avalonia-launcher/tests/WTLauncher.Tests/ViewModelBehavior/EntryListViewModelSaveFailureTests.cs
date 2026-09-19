using System.Collections.ObjectModel;
using WTLauncher.App.Features;
using WTLauncher.App.Features.Entries;
using WTLauncher.App.Infrastructure;
using WTLauncher.App.ViewModels;
using WTLauncher.Core;

using WTLauncher.Tests;
namespace WTLauncher.Tests.ViewModelBehavior;

[Trait("Category", TestCategories.ViewModelBehavior)]
public sealed class EntryListViewModelSaveFailureTests
{
    [Fact]
    public async Task DeleteEntryAsync_WhenSaveFails_KeepsEntryAndReportsSaveError()
    {
        using var temp = new TempDirectory();
        var path = Path.Combine(temp.Path, "launcher.json");
        var entry = new LaunchEntry { Id = "one", Name = "One", Directory = (DirectoryPath)@"D:\one", Command = "pwsh" };
        LauncherStore.Save(new LauncherStoreFile
        {
            Entries = [entry],
            Settings = new LauncherSettings(),
            Presets = [],
            DefaultPresetId = "",
        }, path, backup: false);

        File.SetAttributes(path, FileAttributes.ReadOnly);
        var store = new StoreGrain(path, new FileStorePersistence());
        var list = new EntryListViewModel(
            new DeleteOnlyEntryService(),
            () => DateTimeOffset.UtcNow,
            store,
            new CardLayoutProvider());
        string? status = null;
        list.StatusApplier = content => status = content.ToString();
        list.DragPreviewDiscarder = () => { };
        var item = new LauncherEntryViewModel(entry, list);

        try
        {
            await list.DeleteEntryAsync(item);
        }
        finally
        {
            File.SetAttributes(path, FileAttributes.Normal);
        }

        Assert.NotNull(status);
        Assert.Contains("存储保存失败", status, StringComparison.Ordinal);
        Assert.Single(store.Select(s => s.Entries));
    }

    private sealed class DeleteOnlyEntryService : IEntryService
    {
        public Task<ServiceResult<LaunchEntry>> AddEntryAsync(
            IEnumerable<LaunchEntry> existingEntries,
            LauncherSettings settings,
            IReadOnlyDictionary<string, string>? commandTemplates,
            int presetReferenceCount,
            Func<DateTimeOffset> utcNow) => throw new NotSupportedException();

        public Task<ServiceResult<LaunchEntry>> CloneEntryAsync(
            LauncherEntryViewModel source,
            IEnumerable<string> existingEntryIds,
            IEnumerable<LaunchEntry> existingEntries,
            LauncherSettings settings,
            IReadOnlyDictionary<string, string>? commandTemplates,
            int presetReferenceCount,
            Func<DateTimeOffset> utcNow) => throw new NotSupportedException();

        public Task<ServiceResult<LaunchEntry>> EditEntryAsync(
            LauncherEntryViewModel item,
            IEnumerable<LaunchEntry> existingEntries,
            LauncherPresetFile presetFile,
            LauncherSettings settings,
            IReadOnlyDictionary<string, string>? commandTemplates,
            int presetReferenceCount,
            Func<DateTimeOffset> utcNow) => throw new NotSupportedException();

        public async Task<ServiceResult<EntryDeleteApproval>> DeleteEntryAsync(
            LauncherEntryViewModel item,
            string? keyboardFocusedEntryId)
        {
            await Task.Yield();
            var target = new LauncherEntryDeleteTarget(item.SnapshotEntry());
            return new ServiceSuccess<EntryDeleteApproval>(new EntryDeleteApproval(target, null));
        }

        public ServiceResult<LaunchEntry> CreateEntryFromDraft(
            EntryDraft draft,
            string? originalId,
            LaunchEntry? previousEntry,
            IEnumerable<LaunchEntry> existingEntries) => throw new NotSupportedException();

        public string GenerateUniqueCloneId(string sourceId, IEnumerable<string> existingIds) => throw new NotSupportedException();
    }
}
