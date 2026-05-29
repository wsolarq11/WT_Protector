using WTLauncher.App.ViewModels;
using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed class LauncherEntryDraftsTests
{
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
    [InlineData("", "Name", "D:\\one", "pwsh", "按钮 ID 不能为空")]
    [InlineData("one", "", "D:\\one", "pwsh", "按钮名称不能为空")]
    [InlineData("one", "Name", "", "pwsh", "按钮目录不能为空")]
    [InlineData("one", "Name", "D:\\one", "", "按钮命令不能为空")]
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
            "按钮 ID 已存在: two",
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
}
