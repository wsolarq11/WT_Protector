namespace WTLauncher.Tests;

public sealed class CommandManagementWindowXamlContractTests
{
    [Fact]
    public void Xaml_OwnsStableLayoutCollectionsAndAutomationStatus()
    {
        var xaml = File.ReadAllText(TestRepositoryPaths.View("CommandManagementWindow.axaml"));
        var codeBehind = File.ReadAllText(TestRepositoryPaths.View("CommandManagementWindow.axaml.cs"));

        Assert.Contains("x:DataType=\"vm:CommandManagementViewModel\"", xaml, StringComparison.Ordinal);
        Assert.Contains("ItemsSource=\"{Binding CommandRows}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("ItemsSource=\"{Binding TemplateRows}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:DataType=\"core:CommandRow\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:DataType=\"core:TemplateRow\"", xaml, StringComparison.Ordinal);
        Assert.Contains("SelectedItem=\"{Binding SelectedCommandRow, Mode=TwoWay}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("SelectedItem=\"{Binding SelectedTemplateRow, Mode=TwoWay}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.Name=\"命令管理状态\"", xaml, StringComparison.Ordinal);

        Assert.DoesNotContain("RenderCommandRows", codeBehind, StringComparison.Ordinal);
        Assert.DoesNotContain("RenderTemplateRows", codeBehind, StringComparison.Ordinal);
        Assert.DoesNotContain("BuildCommandRow", codeBehind, StringComparison.Ordinal);
        Assert.DoesNotContain("BuildTemplateRow", codeBehind, StringComparison.Ordinal);
        Assert.DoesNotContain("PropertyChanged +=", codeBehind, StringComparison.Ordinal);
    }
}
