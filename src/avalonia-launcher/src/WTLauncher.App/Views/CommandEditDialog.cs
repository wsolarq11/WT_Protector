using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using WTLauncher.Core;

namespace WTLauncher.App.Views;

/// <summary>
/// 命令管理的二级编辑 dialog。单值或双值 (key+value)。
/// ponytail: 单文件,基于 LauncherDialogText/Button helpers,无 XAML。
/// 双值模式 (key + value) 返回 (string Key, string Value) tuple;
/// 单值模式返回 string?。模板下拉提示 $key 形式。
/// </summary>
public sealed class CommandEditDialog : Window
{
    private const string MonospaceFontFamily = "Cascadia Code, Consolas, Courier New, monospace";

    private readonly TextBox _input1;
    private readonly TextBox? _input2;
    private readonly TextBlock _error;
    private readonly bool _isTwoField;
    private readonly IReadOnlySet<string> _reservedKeys;

    public CommandEditDialog(
        string title,
        string label,
        string initialValue,
        IReadOnlyList<string> templateKeys,
        string? secondLabel = null,
        bool isMultiline = false)
    {
        _isTwoField = secondLabel is not null;
        _reservedKeys = new HashSet<string>(templateKeys, StringComparer.OrdinalIgnoreCase);

        Title = title;
        Width = 540;
        MinWidth = 360;
        SizeToContent = SizeToContent.Height;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = Brush.Parse(LauncherDesignTokens.SurfaceColor);

        var titleLabel = LauncherDialogText.Title(title);
        titleLabel.Margin = new Thickness(0, 0, 0, 12);

        var label1 = LauncherDialogText.Label(label);
        label1.Margin = new Thickness(0, 0, 0, 4);
        _input1 = LauncherDialogText.Input(initialValue);
        _input1.FontFamily = new FontFamily(MonospaceFontFamily);
        if (isMultiline && !_isTwoField)
            _input1.AcceptsReturn = true;

        var fieldsStack = new StackPanel { Spacing = 8 };
        fieldsStack.Children.Add(label1);
        fieldsStack.Children.Add(_input1);

        if (_isTwoField)
        {
            var label2 = LauncherDialogText.Label(secondLabel!);
            label2.Margin = new Thickness(0, 4, 0, 0);
            _input2 = LauncherDialogText.Input(string.Empty);
            _input2.FontFamily = new FontFamily(MonospaceFontFamily);
            fieldsStack.Children.Add(label2);
            fieldsStack.Children.Add(_input2);
        }

        _error = LauncherDialogText.Error();
        _error.Margin = new Thickness(0, 8, 0, 0);

        var ok = LauncherDialogButtons.Primary("确定", minWidth: 96);
        ok.Click += (_, _) => OnOk();
        var cancel = LauncherDialogButtons.Cancel(minWidth: 96);
        cancel.Click += (_, _) => Close(null);
        var footer = LauncherDialogButtons.Footer(LauncherDesignTokens.DialogFooterTopMargin, cancel, ok);

        var grid = new Grid
        {
            RowDefinitions = new RowDefinitions("Auto,Auto,Auto"),
            Children =
            {
                titleLabel.WithGridRow(0),
                fieldsStack.WithGridRow(1),
                footer.WithGridRow(2),
            },
        };
        // _error row sits between fields and footer — append via wrapping grid.
        // ponytail: keep one container; insert _error before footer using StackPanel swap.
        // Simpler: place _error inside the fields stack.
        fieldsStack.Children.Add(_error);

        Content = new Border
        {
            Padding = new Thickness(LauncherDesignTokens.DialogOuterPadding),
            Child = grid,
        };

        Opened += (_, _) => _input1.Focus();
    }

    private void OnOk()
    {
        var v1 = _input1.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(v1))
        {
            ShowError("值不能为空。");
            return;
        }

        if (_isTwoField)
        {
            if (_reservedKeys.Contains(v1))
            {
                ShowError($"Key '{v1}' 已存在。");
                return;
            }
            var v2 = _input2!.Text ?? string.Empty;
            Close((v1, v2));
            return;
        }

        Close(v1);
    }

    private void ShowError(string message)
    {
        _error.Text = message;
        _error.IsVisible = true;
    }
}
