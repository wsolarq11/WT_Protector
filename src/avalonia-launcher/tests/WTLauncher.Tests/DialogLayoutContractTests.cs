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
        var placedRows = Regex.Matches(source, "WithGridRow\\((?<row>\\d+)\\)", RegexOptions.Singleline)
            .Select(match => int.Parse(match.Groups["row"].Value))
            .ToArray();

        Assert.NotEmpty(placedRows);
        Assert.All(placedRows, row => Assert.InRange(row, 0, declaredRowCount - 1));
        Assert.Contains(5, placedRows);
    }

    [Theory]
    [InlineData("ConfirmBatchLaunchWindow.cs")]
    [InlineData("ConfirmEntryDeleteWindow.cs")]
    [InlineData("ConfirmLaunchWindow.cs")]
    [InlineData("ConfirmPresetDeleteWindow.cs")]
    [InlineData("EntryDetailsWindow.cs")]
    [InlineData("PresetDetailsWindow.cs")]
    public void DialogCancelButtonsBindEscapeToCancel(string fileName)
    {
        var source = File.ReadAllText(GetViewPath(fileName));

        Assert.Contains(
            fileName.Contains("Delete", StringComparison.Ordinal)
                ? "var cancel = LauncherDialogButtons.SafeDefaultCancel();"
                : "var cancel = LauncherDialogButtons.Cancel",
            source);
        var dialogButtons = GetDialogButtonsSource();
        Assert.Contains("isCancel: true", MethodBlock(dialogButtons, "Cancel"));
        Assert.Contains("isCancel: true", MethodBlock(dialogButtons, "SafeDefaultCancel"));
        Assert.Contains("IsCancel = isCancel", MethodBlock(dialogButtons, "Create"));
    }

    [Theory]
    [InlineData("ConfirmBatchLaunchWindow.cs", "launch")]
    [InlineData("ConfirmLaunchWindow.cs", "launch")]
    [InlineData("EntryDetailsWindow.cs", "save")]
    [InlineData("PresetDetailsWindow.cs", "save")]
    public void NonDestructiveDialogPrimaryActionsBindEnterAsDefault(string fileName, string buttonName)
    {
        var source = File.ReadAllText(GetViewPath(fileName));

        Assert.Contains($"var {buttonName} = LauncherDialogButtons.Primary", source);
        Assert.Contains("isDefault: true", MethodBlock(GetDialogButtonsSource(), "Primary"));
    }

    [Theory]
    [InlineData("ConfirmEntryDeleteWindow.cs")]
    [InlineData("ConfirmPresetDeleteWindow.cs")]
    public void DestructiveDeleteDialogsDefaultEnterToCancel(string fileName)
    {
        var source = File.ReadAllText(GetViewPath(fileName));

        Assert.Contains("var cancel = LauncherDialogButtons.SafeDefaultCancel();", source);
        Assert.Contains("var delete = LauncherDialogButtons.Destructive", source);
        Assert.Contains("isDefault: true", MethodBlock(GetDialogButtonsSource(), "SafeDefaultCancel"));
        Assert.DoesNotContain("isDefault: true", MethodBlock(GetDialogButtonsSource(), "Destructive"));
    }

    private static string MethodBlock(string source, string methodName)
    {
        var start = source.IndexOf($"static Button {methodName}(", StringComparison.Ordinal);
        Assert.True(start >= 0, $"Expected {methodName} method declaration.");
        var end = source.IndexOf("\n    }\n", start, StringComparison.Ordinal);
        Assert.True(end > start, $"Expected {methodName} method to end.");
        return source[start..(end + 2)];
    }

    private static string GetDialogButtonsSource()
    {
        return File.ReadAllText(GetViewPath("LauncherDialogButtons.cs"));
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
