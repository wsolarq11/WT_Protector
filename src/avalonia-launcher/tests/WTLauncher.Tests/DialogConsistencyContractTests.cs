using System.Text.RegularExpressions;

namespace WTLauncher.Tests;

public sealed class DialogConsistencyContractTests
{
    [Fact]
    public void DialogChrome_UsesSharedSemanticTokens()
    {
        var tokens = File.ReadAllText(TestRepositoryPaths.CoreSource("LauncherDesignTokens.cs"));
        var app = File.ReadAllText(TestRepositoryPaths.AppCodeBehind);
        var commandManagement = File.ReadAllText(TestRepositoryPaths.View("CommandManagementWindow.axaml"));

        Assert.Contains("DialogTitleFontSize = 20", tokens, StringComparison.Ordinal);
        Assert.Contains("DialogOuterPadding = 24", tokens, StringComparison.Ordinal);
        Assert.Contains("DialogFooterTopMargin = 14", tokens, StringComparison.Ordinal);
        Assert.Contains("Resources[\"DialogTitleFontSize\"]", app, StringComparison.Ordinal);
        Assert.Contains("Resources[\"DialogOuterPadding\"]", app, StringComparison.Ordinal);
        Assert.Contains("Resources[\"DialogFooterMargin\"]", app, StringComparison.Ordinal);
        Assert.Contains("Padding=\"{StaticResource DialogOuterPadding}\"", commandManagement, StringComparison.Ordinal);
        Assert.Contains("Classes=\"dialog-title\"", commandManagement, StringComparison.Ordinal);
        Assert.Contains("Margin=\"{StaticResource DialogFooterMargin}\"", commandManagement, StringComparison.Ordinal);
    }

    [Fact]
    public void DestructiveDialogs_DefaultAndFocusCancel()
    {
        foreach (var baseName in new[] { "ConfirmEntryDeleteWindow", "ConfirmPresetDeleteWindow" })
        {
            var xaml = File.ReadAllText(TestRepositoryPaths.View(baseName + ".axaml"));
            var codeBehind = File.ReadAllText(TestRepositoryPaths.View(baseName + ".axaml.cs"));

            Assert.Contains("IsDefault=\"True\"", xaml, StringComparison.Ordinal);
            Assert.Contains("IsCancel=\"True\"", xaml, StringComparison.Ordinal);
            Assert.Contains("Classes=\"destructive\"", xaml, StringComparison.Ordinal);
            Assert.Contains("CancelButton.Focus()", codeBehind, StringComparison.Ordinal);
        }
    }
}
