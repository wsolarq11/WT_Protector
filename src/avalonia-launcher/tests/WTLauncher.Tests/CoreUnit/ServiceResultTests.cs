using WTLauncher.App.Features;
using WTLauncher.App.ViewModels;

using WTLauncher.Tests;
namespace WTLauncher.Tests.CoreUnit;

[Trait("Category", TestCategories.CoreUnit)]
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

}
