using WTLauncher.App.ViewModels;

namespace WTLauncher.Tests;

public sealed class LauncherPresetSelectionPreviewTests
{
    [Fact]
    public void NoPresetReportsWhetherPresetsExist()
    {
        var empty = LauncherPresetSelectionPreview.NoPreset(presetCount: 0);
        var available = LauncherPresetSelectionPreview.NoPreset(presetCount: 1);

        Assert.False(empty.HasChanges);
        Assert.Equal("应用启动组", empty.ApplyButtonText);
        Assert.Contains("暂无启动组", empty.StatusText);
        Assert.Contains("请选择一个启动组", available.StatusText);
    }

    [Fact]
    public void ForPresetComputesMatchedMissingAndExtraCounts()
    {
        var preview = LauncherPresetSelectionPreview.ForPreset(
            "Daily",
            selectedIds: ["one", "extra"],
            knownIds: ["one", "two", "extra"],
            presetIds: ["one", "two"]);

        Assert.True(preview.HasChanges);
        Assert.Equal(1, preview.MatchedCount);
        Assert.Equal(1, preview.MissingCount);
        Assert.Equal(1, preview.ExtraCount);
        Assert.Empty(preview.MissingConfigIds);
        Assert.Equal("应用启动组", preview.ApplyButtonText);
        Assert.Contains("已匹配 1/2", preview.StatusText);
        Assert.Contains("缺 1", preview.StatusText);
        Assert.Contains("额外 1", preview.StatusText);
    }

    [Fact]
    public void ForPresetReportsAlreadyMatchedPreset()
    {
        var preview = LauncherPresetSelectionPreview.ForPreset(
            "Daily",
            selectedIds: ["one"],
            knownIds: ["one"],
            presetIds: ["one"]);

        Assert.False(preview.HasChanges);
        Assert.Equal(1, preview.MatchedCount);
        Assert.Equal(0, preview.MissingCount);
        Assert.Equal(0, preview.ExtraCount);
        Assert.Equal("已匹配", preview.ApplyButtonText);
        Assert.Contains("已匹配启动组", preview.StatusText);
    }

    [Theory]
    [InlineData(false, "手动模式", false, 0)]
    [InlineData(true, "清空勾选", true, 1)]
    public void ForPresetDescribesEmptyManualPreset(
        bool hasSelection,
        string expectedButtonText,
        bool expectedHasChanges,
        int expectedExtraCount)
    {
        var selectedIds = hasSelection ? new[] { "one" } : Array.Empty<string>();

        var preview = LauncherPresetSelectionPreview.ForPreset(
            "Manual",
            selectedIds,
            knownIds: ["one"],
            presetIds: []);

        Assert.Equal(expectedHasChanges, preview.HasChanges);
        Assert.Equal(expectedExtraCount, preview.ExtraCount);
        Assert.Equal(expectedButtonText, preview.ApplyButtonText);
    }

    [Fact]
    public void ForPresetDedupesAndReportsMissingConfigIds()
    {
        var preview = LauncherPresetSelectionPreview.ForPreset(
            "Broken",
            selectedIds: [],
            knownIds: ["known"],
            presetIds: ["missing", "missing", "", " "]);

        Assert.True(preview.HasChanges);
        Assert.Equal(["missing"], preview.MissingConfigIds);
        Assert.Equal("检查启动组", preview.ApplyButtonText);
        Assert.Contains("引用了 1 个不存在", preview.StatusText);
    }
}
