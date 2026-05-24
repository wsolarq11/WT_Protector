using System.Text.RegularExpressions;

namespace WTLauncher.Tests;

public sealed class DialogLayoutContractTests
{
    [Fact]
    public void PresetDetailsWindow_DoesNotPlaceControlsOutsideDeclaredGridRows()
    {
        var source = File.ReadAllText(GetViewPath("PresetDetailsWindow.cs"));
        var rowDefinitionsMatch = Regex.Match(
            source,
            "RowDefinitions\\s*=\\s*new RowDefinitions\\(\"(?<rows>[^\"]+)\"\\)");
        Assert.True(rowDefinitionsMatch.Success, "PresetDetailsWindow must declare explicit grid rows.");

        var declaredRowCount = rowDefinitionsMatch.Groups["rows"].Value
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Length;
        var placedRows = Regex.Matches(source, "Place\\([^;]*?,\\s*(?<row>\\d+)\\)", RegexOptions.Singleline)
            .Select(match => int.Parse(match.Groups["row"].Value))
            .ToArray();

        Assert.NotEmpty(placedRows);
        Assert.All(placedRows, row => Assert.InRange(row, 0, declaredRowCount - 1));
        Assert.Contains(5, placedRows);
    }

    private static string GetViewPath(string fileName)
    {
        return Path.Combine(
            FindRepositoryRoot(),
            "src",
            "avalonia-launcher",
            "src",
            "WTLauncher.App",
            "Views",
            fileName);
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
