namespace WTLauncher.Tests;

public sealed class MainWindowXamlContractTests
{
    [Fact]
    public void MainWindow_UsesSingleFourBoxControlGrid()
    {
        var xaml = File.ReadAllText(GetMainWindowXamlPath());
        var gridStart = xaml.IndexOf("x:Name=\"ControlBoxesGrid\"", StringComparison.Ordinal);
        Assert.True(gridStart >= 0, "MainWindow must keep search/sort/view/preset controls in one named parent Grid.");

        var nextRowStart = xaml.IndexOf("<StackPanel Grid.Row=\"3\"", gridStart, StringComparison.Ordinal);
        Assert.True(nextRowStart > gridStart, "ControlBoxesGrid should be the complete row-2 control band before action row 3.");

        var controlGrid = xaml[gridStart..nextRowStart];
        Assert.Contains("ColumnDefinitions=\"2.2*,1.1*,1.1*,2.2*\"", controlGrid);
        Assert.Equal(4, Count(controlGrid, "Classes=\"control-box\""));
        AssertInOrder(controlGrid, "Text=\"搜索\"", "Text=\"排序\"", "Text=\"视图\"", "Text=\"启动组\"");
        Assert.Contains("Text=\"{Binding FilterText, Mode=TwoWay}\"", controlGrid);
        Assert.Contains("ItemsSource=\"{Binding SortModes}\"", controlGrid);
        Assert.Contains("ItemsSource=\"{Binding DisplayModes}\"", controlGrid);
        Assert.Contains("ItemsSource=\"{Binding Presets}\"", controlGrid);
        Assert.Contains("Content=\"{Binding ApplyPresetButtonText}\"", controlGrid);
    }

    [Fact]
    public void MainWindow_DoesNotRenderUpdatedTimeInEntryViews()
    {
        var xaml = File.ReadAllText(GetMainWindowXamlPath());

        Assert.DoesNotContain("UpdatedLabel", xaml);
        Assert.DoesNotContain("ShowUpdatedLabel", xaml);
        Assert.DoesNotContain("更新时间", xaml);
    }

    private static int Count(string text, string value)
    {
        var count = 0;
        var index = 0;
        while ((index = text.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }

        return count;
    }

    private static void AssertInOrder(string text, params string[] values)
    {
        var last = -1;
        foreach (var value in values)
        {
            var index = text.IndexOf(value, last + 1, StringComparison.Ordinal);
            Assert.True(index > last, $"Expected '{value}' after index {last}.");
            last = index;
        }
    }

    private static string GetMainWindowXamlPath()
    {
        var root = FindRepositoryRoot();
        return Path.Combine(root, "src", "avalonia-launcher", "src", "WTLauncher.App", "Views", "MainWindow.axaml");
    }

    private static string FindRepositoryRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "config", "config.json"))
                && File.Exists(Path.Combine(dir.FullName, "README.md"))
                && Directory.Exists(Path.Combine(dir.FullName, "src", "avalonia-launcher")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate WT_Protector repository root from test output directory.");
    }
}
