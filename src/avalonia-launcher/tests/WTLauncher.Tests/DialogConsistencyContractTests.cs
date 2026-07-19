using System.Text.RegularExpressions;
using WTLauncher.App.Views;

namespace WTLauncher.Tests;

public sealed class DialogConsistencyContractTests
{
    private static readonly string[] ProgrammaticDialogs =
    [
        "CommandEditDialog.cs",
        "DirectoryEditDialog.cs",
        "ConfirmEntryDeleteWindow.cs",
        "ConfirmPresetDeleteWindow.cs",
        "ConfirmLaunchWindow.cs",
        "ConfirmBatchLaunchWindow.cs",
        "PresetDetailsWindow.cs",
        "EntryDetailsWindow.cs",
        "DiscoverAgentsWindow.cs",
        "DirectoryManagementWindow.cs",
    ];

    [Fact]
    public void DialogChrome_UsesSharedSemanticTokens()
    {
        var tokens = File.ReadAllText(TestRepositoryPaths.CoreSource("LauncherDesignTokens.cs"));
        var app = File.ReadAllText(TestRepositoryPaths.AppCodeBehind);
        var textHelper = File.ReadAllText(TestRepositoryPaths.View("LauncherDialogText.cs"));
        var commandManagement = File.ReadAllText(TestRepositoryPaths.View("CommandManagementWindow.axaml"));

        Assert.Contains("DialogTitleFontSize = 20", tokens, StringComparison.Ordinal);
        Assert.Contains("DialogOuterPadding = 24", tokens, StringComparison.Ordinal);
        Assert.Contains("DialogFooterTopMargin = 14", tokens, StringComparison.Ordinal);
        Assert.Contains("Resources[\"DialogTitleFontSize\"]", app, StringComparison.Ordinal);
        Assert.Contains("Resources[\"DialogOuterPadding\"]", app, StringComparison.Ordinal);
        Assert.Contains("Resources[\"DialogFooterMargin\"]", app, StringComparison.Ordinal);
        Assert.Contains("FontSize = LauncherDesignTokens.DialogTitleFontSize", textHelper, StringComparison.Ordinal);
        Assert.Contains("Padding=\"{StaticResource DialogOuterPadding}\"", commandManagement, StringComparison.Ordinal);
        Assert.Contains("FontSize=\"{StaticResource DialogTitleFontSize}\"", commandManagement, StringComparison.Ordinal);
        Assert.Contains("Margin=\"{StaticResource DialogFooterMargin}\"", commandManagement, StringComparison.Ordinal);

        foreach (var fileName in ProgrammaticDialogs)
        {
            var source = File.ReadAllText(TestRepositoryPaths.View(fileName));
            Assert.Contains("LauncherDesignTokens.DialogOuterPadding", source, StringComparison.Ordinal);
            Assert.DoesNotMatch(new Regex(@"LauncherDialogButtons\.Footer\((?:12|14|16|18|24),"), source);
        }

        Assert.Contains("LauncherDialogButtons.Footer(0, cancel, launch)",
            File.ReadAllText(TestRepositoryPaths.View("ConfirmLaunchWindow.cs")), StringComparison.Ordinal);
    }

    [Fact]
    public void DestructiveDialogs_DefaultAndFocusCancel()
    {
        var cancel = LauncherDialogButtons.Cancel(isDefault: true);
        var delete = LauncherDialogButtons.Destructive("删除", isDefault: false);

        Assert.True(cancel.IsDefault);
        Assert.True(cancel.IsCancel);
        Assert.False(delete.IsDefault);
        Assert.False(delete.IsCancel);

        foreach (var fileName in new[] { "ConfirmEntryDeleteWindow.cs", "ConfirmPresetDeleteWindow.cs" })
        {
            var source = File.ReadAllText(TestRepositoryPaths.View(fileName));
            Assert.Contains("LauncherDialogButtons.Cancel(isDefault: true)", source, StringComparison.Ordinal);
            Assert.Contains("LauncherDialogButtons.Destructive(", source, StringComparison.Ordinal);
            Assert.Contains("isDefault: false", source, StringComparison.Ordinal);
            Assert.Contains("Opened += (_, _) => cancel.Focus();", source, StringComparison.Ordinal);
            Assert.DoesNotContain("Opened += (_, _) => delete.Focus();", source, StringComparison.Ordinal);
        }
    }
}
