using System.Text.RegularExpressions;

using WTLauncher.Tests;
namespace WTLauncher.Tests.UiContract;

[Trait("Category", TestCategories.UiContract)]
public sealed class DialogConsistencyContractTests
{
    [Fact]
    public void DialogChrome_UsesSharedSemanticTokens()
    {
        var tokens = File.ReadAllText(TestRepositoryPaths.CoreSource("LauncherDesignTokens.cs"));
        var app = File.ReadAllText(TestRepositoryPaths.AppCodeBehind);
        var commandManagement = XamlContractAssertions.Load(TestRepositoryPaths.View("CommandManagementWindow.axaml"));

        Assert.Contains("DialogTitleFontSize = 20", tokens, StringComparison.Ordinal);
        Assert.Contains("DialogOuterPadding = 24", tokens, StringComparison.Ordinal);
        Assert.Contains("DialogFooterTopMargin = 14", tokens, StringComparison.Ordinal);
        Assert.Contains("Resources[\"DialogTitleFontSize\"]", app, StringComparison.Ordinal);
        Assert.Contains("Resources[\"DialogOuterPadding\"]", app, StringComparison.Ordinal);
        Assert.Contains("Resources[\"DialogFooterMargin\"]", app, StringComparison.Ordinal);
        XamlContractAssertions.AssertAttribute(commandManagement, "Padding", "{StaticResource DialogOuterPadding}");
        XamlContractAssertions.AssertElement(commandManagement, "TextBlock", ("Classes", "dialog-title"));
        XamlContractAssertions.AssertAttribute(commandManagement, "Margin", "{StaticResource DialogFooterMargin}");
    }

    [Fact]
    public void DestructiveDialogs_DefaultAndFocusCancel()
    {
        foreach (var baseName in new[] { "ConfirmEntryDeleteWindow", "ConfirmPresetDeleteWindow" })
        {
            var xaml = XamlContractAssertions.Load(TestRepositoryPaths.View(baseName + ".axaml"));
            var codeBehind = File.ReadAllText(TestRepositoryPaths.View(baseName + ".axaml.cs"));

            XamlContractAssertions.AssertAttribute(xaml, "IsDefault", "True");
            XamlContractAssertions.AssertAttribute(xaml, "IsCancel", "True");
            XamlContractAssertions.AssertElement(xaml, "Button", ("Classes", "destructive"));
            Assert.Contains("CancelButton.Focus()", codeBehind, StringComparison.Ordinal);
        }
    }
}
