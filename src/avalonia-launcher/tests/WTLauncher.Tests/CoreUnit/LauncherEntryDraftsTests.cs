using WTLauncher.App.ViewModels;
using WTLauncher.Core;

using WTLauncher.Tests;
namespace WTLauncher.Tests.CoreUnit;

[Trait("Category", TestCategories.CoreUnit)]
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
            updatedAtUtc: "2026-05-20T00:00:00Z");

        var entry = LauncherEntryDrafts.CreateEntry(
            new EntryDraft(" new-id ", " New Name ", " D:\\new ", " codex ", true),
            previous);

        Assert.Equal("new-id", entry.Id);
        Assert.Equal("New Name", entry.Name);
        Assert.Equal("D:\\new", entry.Directory.Value);
        Assert.Equal("codex", entry.Command);
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
        bool selected = false,
        string updatedAtUtc = "")
    {
        return new LaunchEntry
        {
            Id = string.IsNullOrWhiteSpace(id) ? EntryId.Empty : id,
            Name = string.IsNullOrWhiteSpace(name) ? EntryName.Empty : name,
            Directory = string.IsNullOrWhiteSpace(directory) ? WTLauncher.Core.DirectoryPath.Empty : (WTLauncher.Core.DirectoryPath)directory,
            Command = command,
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

