using WTLauncher.App.ViewModels;
using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed class LauncherEntryDraftsTests
{
    [Fact]
    public void NormalizeDraftTrimsTextFieldsAndPreservesSelection()
    {
        var draft = LauncherEntryDrafts.NormalizeDraft(
            new EntryDraft(" new-id ", " New Name ", " D:\\new ", " codex ", true));

        Assert.Equal("new-id", draft.Id);
        Assert.Equal("New Name", draft.Name);
        Assert.Equal("D:\\new", draft.Directory);
        Assert.Equal("codex", draft.Command);
        Assert.True(draft.Selected);
    }

    [Fact]
    public void TryNormalizeRequiredDraftReturnsNormalizedDraftWhenRequiredFieldsExist()
    {
        var accepted = LauncherEntryDrafts.TryNormalizeRequiredDraft(
            new EntryDraft(" new-id ", " New Name ", " D:\\new ", " codex ", true),
            out var draft,
            out var requiredFieldError);

        Assert.True(accepted);
        Assert.Null(requiredFieldError);
        Assert.Equal(new EntryDraft("new-id", "New Name", "D:\\new", "codex", true), draft);
    }

    [Fact]
    public void TryNormalizeRequiredDraftReportsErrorAfterNormalization()
    {
        var accepted = LauncherEntryDrafts.TryNormalizeRequiredDraft(
            new EntryDraft(" new-id ", "   ", " D:\\new ", " codex ", true),
            out var draft,
            out var requiredFieldError);

        Assert.False(accepted);
        Assert.Equal(LauncherEntryDraftStatus.NameRequired, requiredFieldError);
        Assert.Equal(new EntryDraft("new-id", "", "D:\\new", "codex", true), draft);
    }

    [Theory]
    [MemberData(nameof(RequiredFieldCases))]
    public void GetRequiredFieldErrorReportsTheFirstMissingField(
        string id,
        string name,
        string directory,
        string command,
        string? expected)
    {
        var draft = new EntryDraft(id, name, directory, command, false);

        Assert.Equal(expected, LauncherEntryDrafts.GetRequiredFieldError(draft));
    }

    [Fact]
    public void CreateEntryTrimsDraftFieldsAndPreservesPreviousTimestamp()
    {
        var previous = Entry(
            "old",
            "Old",
            "D:\\old",
            confirm: true,
            updatedAtUtc: "2026-05-20T00:00:00Z");

        var entry = LauncherEntryDrafts.CreateEntry(
            new EntryDraft(" new-id ", " New Name ", " D:\\new ", " codex ", true),
            previous);

        Assert.Equal("new-id", entry.Id);
        Assert.Equal("New Name", entry.Name);
        Assert.Equal("D:\\new", entry.Directory);
        Assert.Equal("codex", entry.Command);
        Assert.False(entry.Confirm);
        Assert.True(entry.Selected);
        Assert.Equal(previous.UpdatedAtUtc, entry.UpdatedAtUtc);
    }

    [Theory]
    [MemberData(nameof(BlankRequiredFieldCases))]
    public void ValidateEntryRejectsRequiredBlankFields(
        string id,
        string name,
        string directory,
        string command,
        string expected)
    {
        var entry = Entry(id, name, directory, command);

        var result = LauncherEntryDrafts.ValidateEntry(entry, originalId: null, existingEntries: []);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void ValidateEntryRejectsDuplicateIdsExceptOriginalEntry()
    {
        var existing = new[]
        {
            Entry("one", "One", "D:\\one"),
            Entry("two", "Two", "D:\\two", "codex"),
        };

        var sameAsOriginal = Entry("one", "Renamed", "D:\\new", "opencode");
        var duplicateOther = Entry("two", "Renamed", "D:\\new", "opencode");

        Assert.Null(LauncherEntryDrafts.ValidateEntry(sameAsOriginal, originalId: "one", existingEntries: existing));
        Assert.Equal(
            LauncherEntryDraftStatus.DuplicateId("two"),
            LauncherEntryDrafts.ValidateEntry(duplicateOther, originalId: "one", existingEntries: existing));
    }

    [Fact]
    public void ValidateEntryNormalizesIdsBeforeCheckingDuplicates()
    {
        var existing = new[]
        {
            Entry(" one ", "One", "D:\\one"),
            Entry(" two ", "Two", "D:\\two", "codex"),
        };

        var sameAsOriginal = Entry(" one ", "Renamed", "D:\\new", "opencode");
        var duplicateOther = Entry(" two ", "Renamed", "D:\\new", "opencode");

        Assert.Null(LauncherEntryDrafts.ValidateEntry(sameAsOriginal, originalId: "one", existingEntries: existing));
        Assert.Equal(
            LauncherEntryDraftStatus.DuplicateId("two"),
            LauncherEntryDrafts.ValidateEntry(duplicateOther, originalId: "one", existingEntries: existing));
    }

    private static LaunchEntry Entry(
        string id,
        string name,
        string directory,
        string command = "pwsh",
        bool confirm = false,
        bool selected = false,
        string updatedAtUtc = "")
    {
        return new LaunchEntry
        {
            Id = id,
            Name = name,
            Directory = directory,
            Command = command,
            Confirm = confirm,
            Selected = selected,
            UpdatedAtUtc = updatedAtUtc,
        };
    }

    public static IEnumerable<object?[]> RequiredFieldCases()
    {
        yield return ["", "Name", "D:\\one", "pwsh", LauncherEntryDraftStatus.IdRequired];
        yield return ["one", "", "D:\\one", "pwsh", LauncherEntryDraftStatus.NameRequired];
        yield return ["one", "Name", "", "pwsh", LauncherEntryDraftStatus.DirectoryRequired];
        yield return ["one", "Name", "D:\\one", "", LauncherEntryDraftStatus.CommandRequired];
        yield return ["one", "Name", "D:\\one", "pwsh", null];
    }

    public static IEnumerable<object?[]> BlankRequiredFieldCases() =>
        RequiredFieldCases().Where(testCase => testCase[4] is not null);
}
