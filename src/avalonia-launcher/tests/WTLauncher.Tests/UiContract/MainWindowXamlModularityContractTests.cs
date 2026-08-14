using WTLauncher.Tests;
namespace WTLauncher.Tests.UiContract;

[Trait("Category", TestCategories.UiContract)]
public sealed class MainWindowXamlModularityContractTests
{
    [Fact]
    public void MainWindow_ComposesTypedControlBarAndEntriesModules()
    {
        var mainXaml = XamlContractAssertions.Load(TestRepositoryPaths.MainWindowXaml);
        var mainCodeBehind = File.ReadAllText(TestRepositoryPaths.View("MainWindow.axaml.cs"));
        var controlBarXaml = XamlContractAssertions.Load(TestRepositoryPaths.View("LauncherControlBar.axaml"));
        var controlBarCodeBehind = File.ReadAllText(TestRepositoryPaths.View("LauncherControlBar.axaml.cs"));
        var entriesXaml = XamlContractAssertions.Load(TestRepositoryPaths.View("LauncherEntriesView.axaml"));
        var entriesCodeBehind = File.ReadAllText(TestRepositoryPaths.View("LauncherEntriesView.axaml.cs"));

        XamlContractAssertions.AssertElement(mainXaml, "LauncherControlBar", ("Name", "ControlBar"));
        XamlContractAssertions.AssertElement(mainXaml, "LauncherEntriesView", ("Name", "EntriesView"));
        XamlContractAssertions.AssertAttribute(controlBarXaml, "DataType", "vm:MainWindowViewModel");
        XamlContractAssertions.AssertAttribute(entriesXaml, "DataType", "vm:MainWindowViewModel");

        XamlContractAssertions.AssertAttribute(controlBarXaml, "Name", "SearchTextBox");
        XamlContractAssertions.AssertAttribute(controlBarXaml, "Name", "DisplayModeComboBox");
        Assert.Contains("public AutoCompleteBox SearchBox => SearchTextBox;", controlBarCodeBehind, StringComparison.Ordinal);
        Assert.Contains("public ComboBox DisplayModeBox => DisplayModeComboBox;", controlBarCodeBehind, StringComparison.Ordinal);

        XamlContractAssertions.AssertAttribute(entriesXaml, "Name", "CardsScrollViewer");
        XamlContractAssertions.AssertAttribute(entriesXaml, "Name", "CardsEntriesItemsControl");
        XamlContractAssertions.AssertAttribute(entriesXaml, "Name", "DetailsEntriesItemsControl");
        XamlContractAssertions.AssertAttribute(entriesXaml, "Name", "NamesScrollViewer");
        XamlContractAssertions.AssertAttribute(entriesXaml, "Name", "NamesEntriesItemsControl");
        XamlContractAssertions.AssertAttribute(entriesXaml, "Click", "OnEntryMoreClicked");
        XamlContractAssertions.AssertAttribute(entriesXaml, "DoubleTapped", "OnEntryRowDoubleTapped");
        XamlContractAssertions.AssertAttribute(entriesXaml, "PointerPressed", "OnEntryDragPointerPressed");
        Assert.Contains("EntryMoreClicked?.Invoke(sender, e);", entriesCodeBehind, StringComparison.Ordinal);
        Assert.Contains("EntryRowDoubleTapped?.Invoke(sender, e);", entriesCodeBehind, StringComparison.Ordinal);
        Assert.Contains("EntryDragPointerPressed?.Invoke(sender, e);", entriesCodeBehind, StringComparison.Ordinal);

        XamlContractAssertions.AssertNoAttribute(mainXaml, "Name", "SearchTextBox");
        XamlContractAssertions.AssertNoAttribute(mainXaml, "Name", "CardsEntriesItemsControl");
        XamlContractAssertions.AssertNoElement(mainXaml, "DataTemplate");
        Assert.DoesNotContain("FindControl", mainCodeBehind, StringComparison.Ordinal);
    }
}
