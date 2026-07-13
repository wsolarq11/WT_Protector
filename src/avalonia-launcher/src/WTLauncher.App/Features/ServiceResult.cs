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
    public ServiceCanceled(string status)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(status);
        Status = status;
    }

    public string Status { get; }
}

public sealed record ServiceFailure<T> : ServiceResult<T>
{
    public ServiceFailure(string status)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(status);
        Status = status;
    }

    public string Status { get; }
}
