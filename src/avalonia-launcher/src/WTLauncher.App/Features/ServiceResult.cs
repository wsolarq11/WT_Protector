using WTLauncher.App.Features;

namespace WTLauncher.App.Features;

public abstract record ServiceResult<T>
{
    private protected ServiceResult()
    {
    }
}

public sealed record ServiceSuccess<T> : ServiceResult<T>
{
    public ServiceSuccess(T value)
    {
        ArgumentNullException.ThrowIfNull(value);
        Value = value;
    }

    public T Value { get; }
}

public sealed record ServiceCanceled<T> : ServiceResult<T>
{
    public ServiceCanceled(LauncherStatusContent status)
    {
        Status = status ?? throw new ArgumentNullException(nameof(status));
    }

    public LauncherStatusContent Status { get; }
}

public sealed record ServiceFailure<T> : ServiceResult<T>
{
    public ServiceFailure(LauncherStatusContent status)
    {
        Status = status ?? throw new ArgumentNullException(nameof(status));
    }

    public LauncherStatusContent Status { get; }
}
