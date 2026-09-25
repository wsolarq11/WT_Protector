using WTLauncher.Core;
using WTLauncher.Tests;

namespace WTLauncher.Tests.CoreUnit;

[Trait("Category", TestCategories.CoreUnit)]
public sealed class HanziPinyinTests
{
    [Fact]
    public void GetPinyin_CommonHanzi_ReturnsToneStripped()
    {
        Assert.Equal("shang", HanziPinyin.GetPinyin('上'));
        Assert.Equal("hai", HanziPinyin.GetPinyin('海'));
        Assert.Equal("xiang", HanziPinyin.GetPinyin('项'));
        Assert.Equal("mu", HanziPinyin.GetPinyin('目'));
    }

    [Fact]
    public void GetPinyin_NonHanzi_ReturnsNull()
    {
        Assert.Null(HanziPinyin.GetPinyin('a'));
        Assert.Null(HanziPinyin.GetPinyin('Z'));
        Assert.Null(HanziPinyin.GetPinyin('1'));
    }

    [Fact]
    public void ToPinyin_Chinese_SeparatesBySpace()
    {
        Assert.Equal("shang hai", HanziPinyin.ToPinyin("上海"));
    }

    [Fact]
    public void ToPinyin_MixedText_KeepsAsciiAndSeparatesHanzi()
    {
        Assert.Equal("project- shang hai", HanziPinyin.ToPinyin("project-上海"));
    }

    [Fact]
    public void ToInitials_Chinese_YieldsInitials()
    {
        Assert.Equal("shxm", HanziPinyin.ToInitials("上海项目"));
        Assert.Equal("sh", HanziPinyin.ToInitials("上海"));
    }

    [Fact]
    public void ContainsHan_DetectsCjkOnly()
    {
        Assert.True(HanziPinyin.ContainsHan("上海项目"));
        Assert.False(HanziPinyin.ContainsHan("project-alpha"));
    }
}
