using System.Text.RegularExpressions;

namespace WTLauncher.Tests;

public sealed class DialogLayoutContractTests
{
    [Fact]
    public void PresetDetailsWindow_DoesNotPlaceControlsOutsideDeclaredGridRows()
    {
        var source = File.ReadAllText(TestRepositoryPaths.View("PresetDetailsWindow.cs"));
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
        var source = File.ReadAllText(TestRepositoryPaths.View(fileName));

        Assert.Contains(
            fileName.Contains("Delete", StringComparison.Ordinal)
                ? "var cancel = LauncherDialogButtons.SafeDefaultCancel();"
                : "var cancel = LauncherDialogButtons.Cancel",
            source);
        var dialogButtons = GetDialogButtonsSource();
        Assert.Contains("isCancel: true", MethodSource(dialogButtons, "Cancel"));
        Assert.Contains("Cancel(minWidth, isDefault: true)", MethodSource(dialogButtons, "SafeDefaultCancel"));
        Assert.Contains("IsCancel = isCancel", MethodSource(dialogButtons, "Create"));
    }

    [Theory]
    [InlineData("ConfirmBatchLaunchWindow.cs", "launch")]
    [InlineData("ConfirmLaunchWindow.cs", "launch")]
    [InlineData("EntryDetailsWindow.cs", "save")]
    [InlineData("PresetDetailsWindow.cs", "save")]
    public void NonDestructiveDialogPrimaryActionsBindEnterAsDefault(string fileName, string buttonName)
    {
        var source = File.ReadAllText(TestRepositoryPaths.View(fileName));
        var dialogButtons = GetDialogButtonsSource();

        Assert.Contains($"var {buttonName} = LauncherDialogButtons.Primary", source);
        Assert.Contains("isDefault: true", MethodSource(dialogButtons, "Primary"));
    }

    [Fact]
    public void BatchLaunchConfirmationShowsPreflightedCommandLines()
    {
        var source = File.ReadAllText(TestRepositoryPaths.View("ConfirmBatchLaunchWindow.cs"));
        var formatter = File.ReadAllText(TestRepositoryPaths.ViewModel("LauncherBatchLaunchPlanText.cs"));

        Assert.Contains("LauncherBatchLaunchConfirmation confirmation", source);
        Assert.Contains("var items = confirmation.Items;", source);
        Assert.Contains("LauncherBatchLaunchPlanText.Format(items)", source);
        Assert.Contains("item.Plan.CommandLine", formatter);
        Assert.Contains("请确认实际 wt.exe 命令", source);
        Assert.Contains("confirmation.TargetLabel", source);
        Assert.Contains("Text = plans", source);
        Assert.Contains("IsReadOnly = true", source);
        Assert.DoesNotContain("IReadOnlyList<LaunchEntry>", source);
        Assert.DoesNotContain("个已勾选项", source);
    }

    [Theory]
    [InlineData("ConfirmEntryDeleteWindow.cs")]
    [InlineData("ConfirmPresetDeleteWindow.cs")]
    public void DestructiveDeleteDialogsDefaultEnterToCancel(string fileName)
    {
        var source = File.ReadAllText(TestRepositoryPaths.View(fileName));
        var dialogButtons = GetDialogButtonsSource();

        Assert.Contains("var cancel = LauncherDialogButtons.SafeDefaultCancel();", source);
        Assert.Contains("var delete = LauncherDialogButtons.Destructive", source);
        Assert.Contains("isDefault: true", MethodSource(dialogButtons, "SafeDefaultCancel"));
        Assert.DoesNotContain("isDefault: true", MethodSource(dialogButtons, "Destructive"));
    }

    [Fact]
    public void EntryDeleteDialogUsesSnapshotTarget()
    {
        var source = File.ReadAllText(TestRepositoryPaths.View("ConfirmEntryDeleteWindow.cs"));

        Assert.Contains("ConfirmEntryDeleteWindow(LauncherEntryDeleteTarget target)", source);
        Assert.Contains("ArgumentNullException.ThrowIfNull(target);", source);
        Assert.Contains("target.EntryName", source);
        Assert.DoesNotContain("LauncherEntryViewModel entry", source);
        Assert.DoesNotContain("entry.Name", source);
    }

    [Fact]
    public void EditableDialogTextInputsUseSharedDialogInputStyle()
    {
        var entryDetails = File.ReadAllText(TestRepositoryPaths.View("EntryDetailsWindow.cs"));
        var presetDetails = File.ReadAllText(TestRepositoryPaths.View("PresetDetailsWindow.cs"));
        var dialogText = File.ReadAllText(TestRepositoryPaths.View("LauncherDialogText.cs"));

        Assert.Equal(4, Count(entryDetails, "LauncherDialogText.Input("));
        Assert.Equal(1, Count(presetDetails, "LauncherDialogText.Input("));
        Assert.DoesNotContain("new TextBox", entryDetails);
        Assert.DoesNotContain("new TextBox", presetDetails);
        Assert.Contains("public static TextBox Input(string text)", dialogText);
        Assert.Contains("FontSize = 14", MethodSource(dialogText, "Input"));
        Assert.Contains("Padding = new Thickness(10, 8)", MethodSource(dialogText, "Input"));
    }

    [Fact]
    public void EntryDetailsDialogRejectsNullTextFallbacks()
    {
        var source = File.ReadAllText(TestRepositoryPaths.View("EntryDetailsWindow.cs"));

        Assert.Contains("_idBox = LauncherDialogText.Input(RequireInitialEntryId(entry));", source);
        Assert.Contains("_nameBox = LauncherDialogText.Input(RequireInitialEntryText(entry, static item => item.Name, \"name\"));", source);
        Assert.Contains("_directoryBox = LauncherDialogText.Input(RequireInitialEntryText(entry, static item => item.Directory, \"directory\"));", source);
        Assert.Contains("_commandBox = LauncherDialogText.Input(RequireInitialEntryText(entry, static item => item.Command, \"command\"));", source);

        Assert.Contains("RequireSubmittedText(_idBox, \"id\")", source);
        Assert.Contains("RequireSubmittedText(_nameBox, \"name\")", source);
        Assert.Contains("RequireSubmittedText(_directoryBox, \"directory\")", source);
        Assert.Contains("RequireSubmittedText(_commandBox, \"command\")", source);
        Assert.Contains("throw new InvalidDataException", source);

        Assert.DoesNotContain("_idBox.Text ?? string.Empty", source);
        Assert.DoesNotContain("_nameBox.Text ?? string.Empty", source);
        Assert.DoesNotContain("_directoryBox.Text ?? string.Empty", source);
        Assert.DoesNotContain("_commandBox.Text ?? string.Empty", source);
        Assert.DoesNotContain("entry?.Id ?? \"\"", source);
        Assert.DoesNotContain("entry?.Name ?? \"\"", source);
        Assert.DoesNotContain("entry?.Directory ?? \"\"", source);
        Assert.DoesNotContain("entry?.Command ?? \"\"", source);
    }

    private static string MethodSource(string source, string methodName)
    {
        var start = source.IndexOf($"static Button {methodName}(", StringComparison.Ordinal);
        if (start < 0)
            start = source.IndexOf($"static TextBox {methodName}(", StringComparison.Ordinal);
        Assert.True(start >= 0, $"Expected {methodName} method declaration.");

        var expressionEnd = source.IndexOf(";\n", start, StringComparison.Ordinal);
        var blockStart = source.IndexOf('{', start);
        if (expressionEnd > start && (blockStart < 0 || expressionEnd < blockStart))
            return source[start..(expressionEnd + 1)];

        var end = source.IndexOf("\n    }\n", start, StringComparison.Ordinal);
        Assert.True(end > start, $"Expected {methodName} method to end.");
        return source[start..(end + 2)];
    }

    private static string GetDialogButtonsSource()
    {
        return File.ReadAllText(TestRepositoryPaths.View("LauncherDialogButtons.cs"));
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
}
