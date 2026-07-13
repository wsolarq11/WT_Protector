using System.Collections.Immutable;
using WTLauncher.App.ViewModels;
using WTLauncher.Core;

namespace WTLauncher.Tests;

// ponytail: inline arrays in test inputs — perf-irrelevant in tests.
#pragma warning disable CA1861

public sealed partial class MainWindowViewModelTests
{
    private static ImmutableDictionary<string, string> Templates(params (string Key, string Value)[] pairs) =>
        pairs
            .Select(p => new KeyValuePair<string, string>(p.Key, p.Value))
            .ToImmutableDictionary(StringComparer.OrdinalIgnoreCase);

    private static MainWindowViewModel CreateViewModelWithTemplates(
        string config,
        ImmutableDictionary<string, string> templates,
        Func<MainWindowViewModel, LauncherLaunchWorkflow>? workflowFactory = null,
        StubDialogService? dialogService = null)
    {
        var settings = new LauncherSettings { CommandTemplates = templates };
        var workflow = workflowFactory is null ? CreateLaunchWorkflow() : workflowFactory(null!);
        return CreateViewModel(config, config, settings, launchWorkflow: workflow, dialogService: dialogService);
    }

    [Fact]
    public void ApplyCommandManagementEdit_RenameCommand_UpdatesSingleReference()
    {
        using var temp = new TempDirectory();
        var (config, _, _) = CreatePortableFixture(
            temp,
            Entry("a", "A", command: "$codex"),
            Entry("b", "B", command: "pwsh"));
        var viewModel = CreateViewModelWithTemplates(
            config,
            Templates(("codex", "codex --enable goals")));

        var result = viewModel.ApplyCommandManagementEdit(
            new CommandManagementEdit.RenameCommand("$codex", "$claude"));

        Assert.True(result.Success);
        Assert.Equal("$claude", viewModel.Entries[0].Command);
        Assert.Equal("pwsh", viewModel.Entries[1].Command);
    }

    [Fact]
    public void ApplyCommandManagementEdit_RenameCommand_UpdatesAllReferencesCaseInsensitive()
    {
        // ponytail: 当前 launcher.json 现状 — 14 个 $codex entry, rename 应一次全部更新。
        using var temp = new TempDirectory();
        var entries = Enumerable.Range(1, 14)
            .Select(i => Entry($"c{i}", $"C{i}", command: "$codex"))
            .ToArray();
        var (config, _, _) = CreatePortableFixture(temp, entries);
        var viewModel = CreateViewModelWithTemplates(
            config,
            Templates(("codex", "codex --enable goals")));

        var result = viewModel.ApplyCommandManagementEdit(
            new CommandManagementEdit.RenameCommand("$CODEX", "$codex2"));

        Assert.True(result.Success);
        Assert.All(viewModel.Entries, e => Assert.Equal("$codex2", e.Command));
    }

    [Fact]
    public void ApplyCommandManagementEdit_MergeCommands_ReassignsLoserToKeeper()
    {
        using var temp = new TempDirectory();
        var (config, _, _) = CreatePortableFixture(
            temp,
            Entry("a", "A", command: "codex"),
            Entry("b", "B", command: "claude"),
            Entry("c", "C", command: "codex"));
        var viewModel = CreateViewModel(config, config);

        var result = viewModel.ApplyCommandManagementEdit(
            new CommandManagementEdit.MergeCommands(LoserCommand: "claude", KeeperCommand: "codex"));

        Assert.True(result.Success);
        Assert.All(viewModel.Entries, e => Assert.Equal("codex", e.Command));
    }

    [Fact]
    public void ApplyCommandManagementEdit_UpdateTemplateKey_CascadesToEntries()
    {
        using var temp = new TempDirectory();
        var (config, _, _) = CreatePortableFixture(
            temp,
            Entry("a", "A", command: "$codex"),
            Entry("b", "B", command: "$codex"),
            Entry("c", "C", command: "pwsh"));
        var viewModel = CreateViewModelWithTemplates(
            config,
            Templates(("codex", "codex --enable goals")));

        var result = viewModel.ApplyCommandManagementEdit(
            new CommandManagementEdit.UpdateTemplateKey(
                OldKey: "codex",
                NewKey: "codex2",
                ExpandedValue: "codex --enable goals --dangerously-bypass-approvals-and-sandbox"));

        Assert.True(result.Success);
        Assert.Equal("$codex2", viewModel.Entries[0].Command);
        Assert.Equal("$codex2", viewModel.Entries[1].Command);
        Assert.Equal("pwsh", viewModel.Entries[2].Command);
        Assert.False(viewModel.Settings.CommandTemplates.ContainsKey("codex"));
        Assert.True(viewModel.Settings.CommandTemplates.ContainsKey("codex2"));
    }

    [Fact]
    public void ApplyCommandManagementEdit_UpdateTemplateKey_RejectsDuplicateKey()
    {
        using var temp = new TempDirectory();
        var (config, _, _) = CreatePortableFixture(
            temp,
            Entry("a", "A", command: "$codex"));
        var viewModel = CreateViewModelWithTemplates(
            config,
            Templates(("codex", "codex --enable goals"), ("claude", "claude --skip")));

        var result = viewModel.ApplyCommandManagementEdit(
            new CommandManagementEdit.UpdateTemplateKey("codex", "claude", "codex --enable goals"));

        Assert.False(result.Success);
        // 回滚 — entries / templates 保持不变
        Assert.Equal("$codex", viewModel.Entries[0].Command);
        Assert.True(viewModel.Settings.CommandTemplates.ContainsKey("codex"));
        Assert.True(viewModel.Settings.CommandTemplates.ContainsKey("claude"));
    }

    [Fact]
    public void ApplyCommandManagementEdit_UpdateTemplateValue_PreservesEntryCommand()
    {
        using var temp = new TempDirectory();
        var (config, _, _) = CreatePortableFixture(
            temp,
            Entry("a", "A", command: "$codex"));
        var viewModel = CreateViewModelWithTemplates(
            config,
            Templates(("codex", "codex --enable goals")));

        var result = viewModel.ApplyCommandManagementEdit(
            new CommandManagementEdit.UpdateTemplateValue(
                "codex", "codex --enable goals --dangerously-bypass-approvals-and-sandbox --verbose"));

        Assert.True(result.Success);
        Assert.Equal("$codex", viewModel.Entries[0].Command);
        Assert.Equal(
            "codex --enable goals --dangerously-bypass-approvals-and-sandbox --verbose",
            viewModel.Settings.CommandTemplates["codex"]);
    }

    [Fact]
    public void ApplyCommandManagementEdit_AddTemplate_AddsToSettings()
    {
        using var temp = new TempDirectory();
        var (config, _, _) = CreatePortableFixture(temp, Entry("a", "A", command: "pwsh"));
        var viewModel = CreateViewModelWithTemplates(config, Templates());

        var result = viewModel.ApplyCommandManagementEdit(
            new CommandManagementEdit.AddTemplate("codex", "codex --enable goals"));

        Assert.True(result.Success);
        Assert.Equal("codex --enable goals", viewModel.Settings.CommandTemplates["codex"]);
    }

    [Fact]
    public void ApplyCommandManagementEdit_AddTemplate_RejectsDuplicateKey()
    {
        using var temp = new TempDirectory();
        var (config, _, _) = CreatePortableFixture(temp, Entry("a", "A", command: "$codex"));
        var viewModel = CreateViewModelWithTemplates(
            config,
            Templates(("codex", "codex --enable goals")));

        var result = viewModel.ApplyCommandManagementEdit(
            new CommandManagementEdit.AddTemplate("codex", "codex --other-flag"));

        Assert.False(result.Success);
        Assert.Equal("codex --enable goals", viewModel.Settings.CommandTemplates["codex"]);
    }

    [Fact]
    public void ApplyCommandManagementEdit_DeleteTemplate_RemovesWhenUnreferenced()
    {
        using var temp = new TempDirectory();
        var (config, _, _) = CreatePortableFixture(temp, Entry("a", "A", command: "pwsh"));
        var viewModel = CreateViewModelWithTemplates(
            config,
            Templates(("codex", "codex --enable goals"), ("claude", "claude --skip")));

        var result = viewModel.ApplyCommandManagementEdit(
            new CommandManagementEdit.DeleteTemplate("claude"));

        Assert.True(result.Success);
        Assert.False(viewModel.Settings.CommandTemplates.ContainsKey("claude"));
        Assert.True(viewModel.Settings.CommandTemplates.ContainsKey("codex"));
    }

    [Fact]
    public void ApplyCommandManagementEdit_PersistsChangesToStore()
    {
        using var temp = new TempDirectory();
        var (config, _, _) = CreatePortableFixture(temp, Entry("a", "A", command: "$codex"));
        var viewModel = CreateViewModelWithTemplates(
            config,
            Templates(("codex", "codex --enable goals")));

        viewModel.ApplyCommandManagementEdit(
            new CommandManagementEdit.UpdateTemplateValue("codex", "codex --verbose"));

        var reloaded = LauncherStore.Load(config);
        Assert.Equal("codex --verbose", reloaded.Settings.CommandTemplates["codex"]);
    }

    [Fact]
    public async Task OpenCommandManagement_InvokesInjectedDialogWithApplyCallback()
    {
        using var temp = new TempDirectory();
        var (config, _, _) = CreatePortableFixture(
            temp,
            Entry("a", "A", command: "$codex"),
            Entry("b", "B", command: "$codex"));
        var viewModel = CreateViewModelWithTemplates(
            config,
            Templates(("codex", "codex --enable goals")));

        Func<CommandManagementEdit, CommandManagementResult>? capturedApply = null;
        var dialogService = new StubDialogService
        {
            OnOpenCommandManagement = (_, _, apply) =>
            {
                capturedApply = apply;
                return Task.CompletedTask;
            },
        };
        viewModel = CreateViewModelWithTemplates(config, Templates(("codex", "codex --enable goals")), dialogService: dialogService);

        await viewModel.OpenCommandManagementCommand.ExecuteAsync(null);

        Assert.NotNull(capturedApply);
        var result = capturedApply!(new CommandManagementEdit.RenameCommand("$codex", "$codex2"));
        Assert.True(result.Success);
        Assert.All(viewModel.Entries, e => Assert.Equal("$codex2", e.Command));
    }

    [Fact]
    public async Task OpenCommandManagement_DefaultDialog_DoesNotThrow()
    {
        // 默认 dialog = no-op, 单元测试构造 VM 时不应抛。
        using var temp = new TempDirectory();
        var (config, _, _) = CreatePortableFixture(temp, Entry("a", "A", command: "pwsh"));
        var viewModel = CreateViewModel(config, config);

        await viewModel.OpenCommandManagementCommand.ExecuteAsync(null);

        Assert.NotNull(viewModel.StatusText);
    }

    [Fact]
    public void ApplyCommandManagementEdit_SuccessRefreshesResultRowsAndFilteredMainList()
    {
        using var temp = new TempDirectory();
        var (config, _, _) = CreatePortableFixture(
            temp,
            Entry("a", "A", command: "$codex"),
            Entry("b", "B", command: "$codex"));
        var viewModel = CreateViewModelWithTemplates(
            config,
            Templates(("codex", "codex --enable goals")));
        viewModel.FilterText = "$claude";
        Assert.Empty(viewModel.VisibleEntries);

        var result = viewModel.ApplyCommandManagementEdit(
            new CommandManagementEdit.UpdateTemplateKey("codex", "claude", "claude --skip"));

        Assert.True(result.Success);
        var command = Assert.Single(result.UpdatedCommandRows);
        Assert.Equal("$claude", command.Command);
        Assert.Equal(2, command.ReferenceCount);
        var template = Assert.Single(result.UpdatedTemplateRows);
        Assert.Equal("claude", template.Key);
        Assert.Equal(2, template.ReferenceCount);
        Assert.Equal(2, viewModel.VisibleEntries.Count);
    }

    [Fact]
    public void ApplyCommandManagementEdit_SaveFailureRestoresEntriesTemplatesAndResultRows()
    {
        using var temp = new TempDirectory();
        var (config, _, _) = CreatePortableFixture(
            temp,
            Entry("a", "A", command: "$codex"));
        var viewModel = CreateViewModelWithTemplates(
            config,
            Templates(("codex", "codex --enable goals")));
        var configDir = Path.GetDirectoryName(config)!;
        Directory.Delete(configDir, recursive: true);
        File.WriteAllText(configDir, "blocked");

        var result = viewModel.ApplyCommandManagementEdit(
            new CommandManagementEdit.UpdateTemplateKey("codex", "claude", "claude --skip"));

        Assert.False(result.Success);
        Assert.Equal("$codex", viewModel.Entries[0].Command);
        Assert.True(viewModel.Settings.CommandTemplates.ContainsKey("codex"));
        Assert.False(viewModel.Settings.CommandTemplates.ContainsKey("claude"));
        Assert.Equal("$codex", Assert.Single(result.UpdatedCommandRows).Command);
        Assert.Equal("codex", Assert.Single(result.UpdatedTemplateRows).Key);
    }
}
