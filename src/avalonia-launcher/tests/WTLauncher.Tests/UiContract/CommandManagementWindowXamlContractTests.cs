using WTLauncher.Tests;
namespace WTLauncher.Tests.UiContract;

[Trait("Category", TestCategories.UiContract)]
public sealed class CommandManagementWindowXamlContractTests
{
    [Fact]
    public void Xaml_OwnsStableLayoutCollectionsAndAutomationStatus()
    {
        var xaml = XamlContractAssertions.Load(TestRepositoryPaths.View("CommandManagementWindow.axaml"));
        var codeBehind = File.ReadAllText(TestRepositoryPaths.View("CommandManagementWindow.axaml.cs"));

        XamlContractAssertions.AssertAttribute(xaml, "DataType", "vm:CommandManagementViewModel");
        XamlContractAssertions.AssertAttribute(xaml, "ItemsSource", "{Binding CommandRows}");
        XamlContractAssertions.AssertAttribute(xaml, "ItemsSource", "{Binding TemplateRows}");
        XamlContractAssertions.AssertAttribute(xaml, "DataType", "core:CommandRow");
        XamlContractAssertions.AssertAttribute(xaml, "DataType", "core:TemplateRow");
        XamlContractAssertions.AssertAttribute(xaml, "SelectedItem", "{Binding SelectedCommandRow, Mode=TwoWay}");
        XamlContractAssertions.AssertAttribute(xaml, "SelectedItem", "{Binding SelectedTemplateRow, Mode=TwoWay}");
        XamlContractAssertions.AssertAttribute(xaml, "AutomationProperties.Name", "命令管理状态");

        Assert.DoesNotContain("RenderCommandRows", codeBehind, StringComparison.Ordinal);
        Assert.DoesNotContain("RenderTemplateRows", codeBehind, StringComparison.Ordinal);
        Assert.DoesNotContain("BuildCommandRow", codeBehind, StringComparison.Ordinal);
        Assert.DoesNotContain("BuildTemplateRow", codeBehind, StringComparison.Ordinal);
        Assert.DoesNotContain("PropertyChanged +=", codeBehind, StringComparison.Ordinal);
    }
}
