using System.Text.RegularExpressions;
using Avalonia.Controls;
using WTLauncher.App.Views;

using WTLauncher.Tests;
namespace WTLauncher.Tests.UiContract;

/// <summary>
/// 锁定卡片级手势的三域合一等式：
///   CursorRegion == DragRegion == DoubleTapLaunchRegion == card − ExcludedControlTypes
///
/// 这是触发范围的唯一"公式"——它是一组集合恒等式，不是数值。命中区由
/// <c>Border.card</c> 单一元素承担（视觉与命中天然重合），hover 上浮 / selected
/// 加边框都作用于同一元素，故无需数值补偿。唯一需要数值精确的是 click/drag
/// 分界阈值（见 MainWindowDragThresholdTests），这里是域相等契约。
/// </summary>
[Trait("Category", TestCategories.UiContract)]
public sealed class MainWindowCardGestureTests
{
    // ── 手势判定（纯 C#，headless） ──────────────────────────

    [Fact]
    public void IsGestureEligible_AllowsPlainCardArea()
    {
        var row = new Border();
        var source = new TextBlock();
        row.Child = source;

        Assert.True(MainWindowCardGesture.IsGestureEligible(source));
    }

    [Fact]
    public void IsGestureEligible_BlocksInteractiveControls()
    {
        Assert.False(MainWindowCardGesture.IsGestureEligible(new Button()));
        Assert.False(MainWindowCardGesture.IsGestureEligible(new CheckBox()));
    }

    [Fact]
    public void ExcludedControlTypes_AreExactlyButtonAndCheckBox()
    {
        // 排除集是单一源：任何增删都会同时改变手势域与光标域，必须显式、
        // 且有对应 Cursor=Arrow 样式与断言。锁死防静默扩大/缩小。
        Assert.Equal(
            [typeof(Button), typeof(CheckBox)],
            MainWindowCardGesture.ExcludedControlTypes);
    }

    // ── cursor 域 == 手势域 契约（读真实 XAML） ──────────────

    [Fact]
    public void CursorRegion_EqualsGestureRegion()
    {
        var xaml = File.ReadAllText(TestRepositoryPaths.MainWindowXaml);

        // card 主体承担 SizeAll（拖拽光标域非空锚点）。
        var cardBlock = StyleBlock(xaml, "Border.card");
        Assert.NotNull(cardBlock);
        Assert.Contains("SizeAll", cardBlock);

        // 每个排除控件必须有 Cursor=Arrow 样式，否则它从 card 继承 SizeAll，
        // 光标域 ≠ 手势域（光标说可拖、手势说不拖）。
        foreach (var type in MainWindowCardGesture.ExcludedControlTypes)
        {
            var block = StyleBlock(xaml, $"Border.card {type.Name}");
            Assert.True(block is not null,
                $"MainWindow.axaml 缺少 Selector=\"Border.card {type.Name}\" 的 Cursor=Arrow 样式，光标会继承 SizeAll。");
            Assert.Contains("Cursor", block);
            Assert.Contains("Arrow", block);
        }
    }

    private static string? StyleBlock(string xaml, string selector)
    {
        var pattern = $@"<Style Selector=""{Regex.Escape(selector)}"">(.*?)</Style>";
        var match = Regex.Match(xaml, pattern, RegexOptions.Singleline);
        return match.Success ? match.Groups[1].Value : null;
    }
}
