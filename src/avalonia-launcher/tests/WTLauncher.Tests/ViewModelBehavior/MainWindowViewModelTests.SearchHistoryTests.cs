using WTLauncher.Core;

using WTLauncher.Tests;
namespace WTLauncher.Tests.ViewModelBehavior;

[Trait("Category", TestCategories.ViewModelBehavior)]
public sealed partial class MainWindowViewModelTests
{
    [Fact]
    public void RecordSearchTermAddsTermToEmptyHistory()
    {
        using var temp = new TempDirectory();
        var (config, settingsPath, _) = CreatePortableFixture(temp, oneSelected: false, twoSelected: false);
        var initialState = new LauncherSettings();
        var viewModel = CreateViewModel(config, settingsPath, initialState);

        viewModel.FilterText = "codex";
        viewModel.RecordSearchTerm();

        Assert.Single(viewModel.SearchHistoryItems);
        Assert.Equal("codex", viewModel.SearchHistoryItems[0]);
        Assert.Equal("codex", LauncherStore.Load(settingsPath).Settings.SearchHistory[0]);
    }

    [Fact]
    public void RecordSearchTermDedupMovesExistingTermToFront()
    {
        using var temp = new TempDirectory();
        var (config, settingsPath, _) = CreatePortableFixture(temp, oneSelected: false, twoSelected: false);
        var initialState = new LauncherSettings
        {
            SearchHistory = System.Collections.Immutable.ImmutableList.Create("pwsh", "codex", "opencode"),
        };
        var viewModel = CreateViewModel(config, settingsPath, initialState);

        viewModel.FilterText = "   codex   ";
        viewModel.RecordSearchTerm();

        Assert.Equal("codex", viewModel.SearchHistoryItems[0]);
        Assert.Equal("pwsh", viewModel.SearchHistoryItems[1]);
        Assert.Equal("opencode", viewModel.SearchHistoryItems[2]);
        Assert.Equal(3, viewModel.SearchHistoryItems.Count);
        // Verify round-trip persistence.
        Assert.Equal(["codex", "pwsh", "opencode"], LauncherStore.Load(settingsPath).Settings.SearchHistory);
    }

    [Fact]
    public void RecordSearchTerm_KeepsNewestEntriesWhenCapped()
    {
        // Add 25 terms sequentially — only the 20 most recent should survive.
        // RecordSearchTerm inserts at index 0 (most-recent-first), and
        // RequireSearchHistory truncates from the tail.
        using var temp = new TempDirectory();
        var (config, settingsPath, _) = CreatePortableFixture(temp, oneSelected: false, twoSelected: false);
        var initialState = new LauncherSettings();
        var viewModel = CreateViewModel(config, settingsPath, initialState);

        for (var i = 1; i <= 25; i++)
        {
            viewModel.FilterText = $"term-{i:D2}";
            viewModel.RecordSearchTerm();
        }

        Assert.Equal(20, viewModel.SearchHistoryItems.Count);
        // Terms 1-5 (oldest) should be dropped; terms 6-25 survive.
        for (var i = 1; i <= 5; i++)
            Assert.DoesNotContain($"term-{i:D2}", viewModel.SearchHistoryItems);
        for (var i = 6; i <= 25; i++)
            Assert.Contains($"term-{i:D2}", viewModel.SearchHistoryItems);
        // Most recent entry (term-25) must be at front.
        Assert.Equal("term-25", viewModel.SearchHistoryItems[0]);
        // Verify round-trip persistence.
        var loaded = LauncherStore.Load(settingsPath).Settings;
        Assert.Equal(20, loaded.SearchHistory.Count);
        Assert.Equal("term-25", loaded.SearchHistory[0]);
    }

    [Fact]
    public void RecordSearchTermCapsAt20Entries()
    {
        using var temp = new TempDirectory();
        var (config, settingsPath, _) = CreatePortableFixture(temp, oneSelected: false, twoSelected: false);
        var fullHistory = System.Collections.Immutable.ImmutableList.CreateRange(
            Enumerable.Range(1, 20).Select(i => $"term-{i}"));
        var initialState = new LauncherSettings { SearchHistory = fullHistory };
        var viewModel = CreateViewModel(config, settingsPath, initialState);

        viewModel.FilterText = "new-term";
        viewModel.RecordSearchTerm();

        Assert.Equal(20, viewModel.SearchHistoryItems.Count);
        Assert.Equal("new-term", viewModel.SearchHistoryItems[0]);
        // term-20 should be dropped (21st entry capped)
        Assert.DoesNotContain("term-20", viewModel.SearchHistoryItems);
        Assert.Contains("term-1", viewModel.SearchHistoryItems);
        // Verify round-trip persistence.
        Assert.Equal(20, LauncherStore.Load(settingsPath).Settings.SearchHistory.Count);
        Assert.Equal("new-term", LauncherStore.Load(settingsPath).Settings.SearchHistory[0]);
    }

    [Fact]
    public void RecordSearchTermIgnoresEmptyOrWhitespaceInput()
    {
        using var temp = new TempDirectory();
        var (config, settingsPath, _) = CreatePortableFixture(temp, oneSelected: false, twoSelected: false);
        var initialState = new LauncherSettings
        {
            SearchHistory = System.Collections.Immutable.ImmutableList.Create("codex"),
        };
        var viewModel = CreateViewModel(config, settingsPath, initialState);

        viewModel.FilterText = "";
        viewModel.RecordSearchTerm();
        Assert.Single(viewModel.SearchHistoryItems);

        viewModel.FilterText = "   ";
        viewModel.RecordSearchTerm();
        Assert.Single(viewModel.SearchHistoryItems);
    }

    [Fact]
    public void RecordSearchTermIsNoOpWhenTermAlreadyAtFront()
    {
        using var temp = new TempDirectory();
        var (config, settingsPath, _) = CreatePortableFixture(temp, oneSelected: false, twoSelected: false);
        var initialState = new LauncherSettings
        {
            SearchHistory = System.Collections.Immutable.ImmutableList.Create("codex", "pwsh"),
        };
        var viewModel = CreateViewModel(config, settingsPath, initialState);
        var initialCount = viewModel.SearchHistoryItems.Count;

        viewModel.FilterText = "codex";
        viewModel.RecordSearchTerm();

        Assert.Equal(initialCount, viewModel.SearchHistoryItems.Count);
        Assert.Equal("codex", viewModel.SearchHistoryItems[0]);
        // When term is already at front, no new persistence is triggered.
        Assert.Equal(["codex", "pwsh"], LauncherStore.Load(settingsPath).Settings.SearchHistory);
    }

    [Fact]
    public void SearchHistoryItemsReflectsSettingsSearchHistory()
    {
        using var temp = new TempDirectory();
        var (config, settingsPath, _) = CreatePortableFixture(temp, oneSelected: false, twoSelected: false);
        var initialState = new LauncherSettings
        {
            SearchHistory = System.Collections.Immutable.ImmutableList.Create("pwsh", "cmd"),
        };
        var viewModel = CreateViewModel(config, settingsPath, initialState);

        Assert.Equal(["pwsh", "cmd"], viewModel.SearchHistoryItems);
    }

    [Fact]
    public void RecordSearchTermRollsBackOnSaveFailure()
    {
        using var temp = new TempDirectory();
        var (config, settingsPath, _) = CreatePortableFixture(temp, oneSelected: false, twoSelected: false);
        var initialState = new LauncherSettings
        {
            SearchHistory = System.Collections.Immutable.ImmutableList.Create("pwsh"),
        };
        var viewModel = CreateViewModel(config, settingsPath, initialState);
        // Make settings path a directory so save always fails
        File.Delete(settingsPath);
        Directory.CreateDirectory(settingsPath);

        viewModel.FilterText = "codex";
        viewModel.RecordSearchTerm();

        Assert.Equal(["pwsh"], viewModel.SearchHistoryItems);
        Assert.StartsWith("存储保存失败", viewModel.StatusText);
    }
}
