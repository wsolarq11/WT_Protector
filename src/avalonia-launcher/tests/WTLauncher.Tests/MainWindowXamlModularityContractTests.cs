namespace WTLauncher.Tests;

public sealed class MainWindowXamlModularityContractTests
{
    [Fact]
    public void MainWindow_ComposesTypedControlBarAndEntriesModules()
    {
        var mainXaml = File.ReadAllText(TestRepositoryPaths.MainWindowXaml);
        var mainCodeBehind = File.ReadAllText(TestRepositoryPaths.View("MainWindow.axaml.cs"));
        var controlBarXaml = File.ReadAllText(TestRepositoryPaths.View("LauncherControlBar.axaml"));
        var controlBarCodeBehind = File.ReadAllText(TestRepositoryPaths.View("LauncherControlBar.axaml.cs"));
        var entriesXaml = File.ReadAllText(TestRepositoryPaths.View("LauncherEntriesView.axaml"));
        var entriesCodeBehind = File.ReadAllText(TestRepositoryPaths.View("LauncherEntriesView.axaml.cs"));

        Assert.Contains("<views:LauncherControlBar x:Name=\"ControlBar\"/>", mainXaml, StringComparison.Ordinal);
        Assert.Contains("<views:LauncherEntriesView x:Name=\"EntriesView\"", mainXaml, StringComparison.Ordinal);
        Assert.Contains("x:DataType=\"vm:MainWindowViewModel\"", controlBarXaml, StringComparison.Ordinal);
        Assert.Contains("x:DataType=\"vm:MainWindowViewModel\"", entriesXaml, StringComparison.Ordinal);

        Assert.Contains("x:Name=\"SearchTextBox\"", controlBarXaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"DisplayModeComboBox\"", controlBarXaml, StringComparison.Ordinal);
        Assert.Contains("public AutoCompleteBox SearchBox => SearchTextBox;", controlBarCodeBehind, StringComparison.Ordinal);
        Assert.Contains("public ComboBox DisplayModeBox => DisplayModeComboBox;", controlBarCodeBehind, StringComparison.Ordinal);

        Assert.Contains("x:Name=\"CardsScrollViewer\"", entriesXaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"CardsEntriesItemsControl\"", entriesXaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"DetailsEntriesItemsControl\"", entriesXaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"NamesScrollViewer\"", entriesXaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"NamesEntriesItemsControl\"", entriesXaml, StringComparison.Ordinal);
        Assert.Contains("Click=\"OnEntryMoreClicked\"", entriesXaml, StringComparison.Ordinal);
        Assert.Contains("DoubleTapped=\"OnEntryRowDoubleTapped\"", entriesXaml, StringComparison.Ordinal);
        Assert.Contains("PointerPressed=\"OnEntryDragPointerPressed\"", entriesXaml, StringComparison.Ordinal);
        Assert.Contains("EntryMoreClicked?.Invoke(sender, e);", entriesCodeBehind, StringComparison.Ordinal);
        Assert.Contains("EntryRowDoubleTapped?.Invoke(sender, e);", entriesCodeBehind, StringComparison.Ordinal);
        Assert.Contains("EntryDragPointerPressed?.Invoke(sender, e);", entriesCodeBehind, StringComparison.Ordinal);

        Assert.DoesNotContain("x:Name=\"SearchTextBox\"", mainXaml, StringComparison.Ordinal);
        Assert.DoesNotContain("x:Name=\"CardsEntriesItemsControl\"", mainXaml, StringComparison.Ordinal);
        Assert.DoesNotContain("<DataTemplate", mainXaml, StringComparison.Ordinal);
        Assert.DoesNotContain("FindControl", mainCodeBehind, StringComparison.Ordinal);
    }
}
