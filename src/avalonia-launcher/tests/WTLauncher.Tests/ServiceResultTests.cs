using WTLauncher.App.Features;
using WTLauncher.App.ViewModels;

namespace WTLauncher.Tests;

public sealed class ServiceResultTests
{
    [Fact]
    public void SuccessRejectsNullPayload() =>
        Assert.Throws<ArgumentNullException>(() => new ServiceSuccess<string>(null!));

    [Fact]
    public void NonSuccessRejectsMissingStatus()
    {
        Assert.Throws<ArgumentNullException>(() => new ServiceCanceled<string>(null!));
        Assert.Throws<ArgumentNullException>(() => new ServiceFailure<string>(null!));
    }

    [Fact]
    public void NonSuccessPreservesTypedStatus()
    {
        var canceled = LauncherStatusContent.Warning("已取消");
        var failed = LauncherStatusContent.Error("失败");

        Assert.Same(canceled, new ServiceCanceled<string>(canceled).Status);
        Assert.Same(failed, new ServiceFailure<string>(failed).Status);
    }
}
