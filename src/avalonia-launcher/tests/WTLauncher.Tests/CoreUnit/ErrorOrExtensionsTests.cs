using ErrorOr;
using ErrorOrExtensions = WTLauncher.Core.ErrorOrExtensions;
using WTLauncher.Core;

using WTLauncher.Tests;
namespace WTLauncher.Tests.CoreUnit;

#pragma warning disable CA2201 // Intentionally exercising the reserved-exception non-recoverable path.

[Trait("Category", TestCategories.CoreUnit)]
public sealed class ErrorOrExtensionsTests
{
    private static Error ErrorFactory(Exception ex) => Error.Failure("Test.Failed", ex.Message);

    [Fact]
    public void Try_RecoverableException_ReturnsError()
    {
        var result = ErrorOrExtensions.Try<int>(
            () => throw new IOException("disk"),
            ErrorFactory);

        Assert.True(result.IsError);
        Assert.Contains("disk", result.FirstError.Description, StringComparison.Ordinal);
    }

    [Fact]
    public void Try_NonRecoverableException_Propagates()
    {
        Assert.Throws<OutOfMemoryException>(() =>
            ErrorOrExtensions.Try<int>(
                () => throw new OutOfMemoryException(),
                ErrorFactory));
    }

    [Fact]
    public async Task TryAsync_AggregateRecoverableException_ReturnsError()
    {
        var result = await ErrorOrExtensions.TryAsync<int>(
            () => Task.FromException<int>(new AggregateException(new IOException("aggregate"))),
            ErrorFactory);

        Assert.True(result.IsError);
        Assert.Contains("aggregate", result.FirstError.Description, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TryAsync_NonRecoverableException_Propagates()
    {
        await Assert.ThrowsAsync<StackOverflowException>(() =>
            ErrorOrExtensions.TryAsync<int>(
                () => Task.FromException<int>(new StackOverflowException()),
                ErrorFactory));
    }
}
