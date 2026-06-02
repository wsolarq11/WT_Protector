using System.Diagnostics.CodeAnalysis;
using WTLauncher.App.ViewModels;
using WTLauncher.Core;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace WTLauncher.Tests;

/// <summary>
/// Tests only behavioral formatting logic in status/text helpers — conditional branches,
/// array joins, overload dispatch, and error formatting. Does NOT test static string
/// constants (those are UI surface that changes with UX iteration; testing them verbatim
/// creates change-detector tests with zero behavioral protection).
/// </summary>
public sealed class StatusTextContractTests
{
    // ── LauncherEntryText (Core normalization) ──────────────

    [Theory]
    [InlineData(" One ", "One")]
    [InlineData("", "")]
    [InlineData("  ", "")]
    [InlineData(null, "")]
    public void NormalizeTrimsTextAndConvertsNullToEmpty(string? value, string expected)
    {
        Assert.Equal(expected, LauncherEntryText.Normalize(value));
    }

    // ── LauncherPersistenceStatus (conditional rollback append) ──

    [Fact]
    public void RollbackFailedAppendsRestoreErrorsOnlyWhenPresent()
    {
        Assert.Equal("设置保存失败: disk blocked", LauncherPersistenceStatus.RollbackFailed(
            "设置保存失败: disk blocked",
            []));

        Assert.Equal(
            "设置保存失败: disk blocked；回滚磁盘状态失败: config: denied；presets: denied",
            LauncherPersistenceStatus.RollbackFailed(
                "设置保存失败: disk blocked",
                ["config: denied", "presets: denied"]));
    }

    // ── LauncherSettingsStatus (conditional confirmation text) ──

    [Theory]
    [InlineData(true, "启动确认: 所有启动项启动前统一确认")]
    [InlineData(false, "启动确认: 所有启动项直接启动")]
    public void LaunchConfirmationSavedUsesConditionalText(bool requireConfirmation, string expected)
    {
        Assert.Equal(expected, LauncherSettingsStatus.LaunchConfirmationSaved(requireConfirmation));
    }

    // ── LauncherSingleLaunchStatus (conditional preview + overloads) ──

    [Theory]
    [InlineData(false, "预演: one")]
    [InlineData(true, "预演: one（需要确认）")]
    public void PreviewIncludesConfirmationOnlyWhenRequired(bool requiresConfirmation, string expected)
    {
        Assert.Equal(expected, LauncherSingleLaunchStatus.Preview("one", requiresConfirmation));
    }

    [Fact]
    public void FailedUsesEntryNameOverloadWhenNameAvailable()
    {
        var exception = new InvalidOperationException("missing directory");

        Assert.Equal("预演失败: one", LauncherSingleLaunchStatus.PreviewFailed("one"));
        Assert.Equal("预演失败: missing directory", LauncherSingleLaunchStatus.PreviewFailed(exception));
        Assert.Equal("启动失败: one — missing directory",
            LauncherSingleLaunchStatus.Failed("one", exception));
        Assert.Equal("启动失败: missing directory",
            LauncherSingleLaunchStatus.Failed(exception));
    }

    // ── LauncherSelectionStatus (batch report formatting) ──

    [Fact]
    public void BatchLaunchCompletedFormatsSuccessAndTruncatedFailureReports()
    {
        var success = new LauncherBatchLaunchReport(requestedCount: 2);

        var failures = new LauncherBatchLaunchReport(
            requestedCount: 1,
            skippedCount: 2,
            failureMessages:
            [
                "one: missing one",
                "two: missing two",
                "three: missing three",
                "four: missing four",
            ]);

        Assert.Equal("已请求启动 2 个勾选项", LauncherSelectionStatus.BatchLaunchCompleted(success));
        Assert.Equal(
            "已请求启动 1 个勾选项，4 个失败，2 个未启动：one: missing one；two: missing two；three: missing three；…另有 1 项",
            LauncherSelectionStatus.BatchLaunchCompleted(failures));
    }

    // ── LauncherPresetStatus (array join) ──

    [Fact]
    public void MissingEntryReferencesJoinsIdsIntoSingleMessage()
    {
        Assert.Equal(
            "启动组引用了不存在的启动项: one, two",
            LauncherPresetStatus.MissingEntryReferences(["one", "two"]));
    }
}
