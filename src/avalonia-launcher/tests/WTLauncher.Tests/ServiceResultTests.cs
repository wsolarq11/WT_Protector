using WTLauncher.App.Features;

namespace WTLauncher.Tests;

public sealed class ServiceResultTests
{
    [Fact]
    public void SuccessRejectsNullPayload() =>
        Assert.Throws<ArgumentNullException>(() => new ServiceSuccess<string>(null!));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void NonSuccessRejectsMissingStatus(string? status)
    {
        Assert.ThrowsAny<ArgumentException>(() => new ServiceCanceled<string>(status!));
        Assert.ThrowsAny<ArgumentException>(() => new ServiceFailure<string>(status!));
    }
}
