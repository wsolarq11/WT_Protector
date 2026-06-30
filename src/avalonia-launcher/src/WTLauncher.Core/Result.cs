namespace WTLauncher.Core;

/// <summary>
/// A type-safe result that is either a success (<typeparamref name="T"/>) or an error (<typeparamref name="E"/>).
/// Callers MUST handle both cases — the compiler enforces exhaustive matching through <see cref="Match"/>.
/// </summary>
public readonly record struct Result<T, E> where E : notnull
{
    private readonly T? _value;
    private readonly E? _error;
    private readonly bool _ok;

    private Result(T value) { _value = value; _ok = true; _error = default; }
    private Result(E error) { _error = error; _ok = false; _value = default; }

    public static Result<T, E> Ok(T value) => new(value);
    public static Result<T, E> Err(E error) => new(error);

    public bool IsOk => _ok;
    public bool IsErr => !_ok;

    public TResult Match<TResult>(Func<T, TResult> ok, Func<E, TResult> err)
        => _ok ? ok(_value!) : err(_error!);

    /// <summary>Implicit conversion from success value. Enables <c>return value</c>
    /// in functions returning <c>Result&lt;T,E&gt;</c> without wrapping in Ok().</summary>
    public static implicit operator Result<T, E>(T value) => Ok(value);

    /// <summary>Implicit conversion from error. Enables <c>return error</c>
    /// in functions returning <c>Result&lt;T,E&gt;</c> without wrapping in Err().
    /// Safe because T (value type) and E (reference type) never collide in this codebase.</summary>
    public static implicit operator Result<T, E>(E error) => Err(error);
}

/// <summary>Unit type for <c>Result&lt;Unit, E&gt;</c> — represents a void success.
/// Use <c>Result&lt;Unit, SaveError&gt;</c> for operations that have no return value on success.</summary>
public readonly record struct Unit
{
    public static readonly Unit Value = default;
}
