using System.Diagnostics;
using System.Runtime.CompilerServices;
using OneOf;
using WTLauncher.Core.Errors;

namespace WTLauncher.App.Infrastructure;

/// <summary>
/// Supervisor hierarchy root for error handling.
///
/// Every subsystem gets its own <see cref="ErrorSupervisor"/> instance with
/// an independent circuit breaker (max retries, cooldown).  The supervisor
/// wraps fallible operations so that:
///
///   1. The caller always receives a typed <c>OneOf&lt;T, AppError&gt;</c>
///      (never an unhandled exception).
///   2. Repeated failures trip a circuit breaker that short-circuits
///      subsequent calls, preventing cascade failures.
///   3. The supervisor logs via a best-effort fallback logger that
///      itself never throws.
///
/// Example
/// -------
/// <code>
/// var storeSup = new ErrorSupervisor("Store", maxRetries: 3);
/// var result = storeSup.Guard(() => {
///     var store = LauncherStore.Load(path);
///     return (OneOf&lt;LauncherStoreFile, AppError&gt;)store;
/// });
/// </code>
/// </summary>
public sealed class ErrorSupervisor
{
    private readonly string _name;
    private readonly int _maxRetries;
    private readonly TimeSpan _cooldown;
    private int _failureCount;
    private DateTime _circuitOpenedAt;
    private bool _isOpen;

    /// <summary>Best-effort fallback logger. Never throws — if writing fails,
    /// the error is silently dropped (last-resort swallow).</summary>
    public static readonly Action<string> FallbackLogger = msg =>
    {
        try { Debug.WriteLine(msg); Trace.WriteLine(msg); }
        catch (ObjectDisposedException) { /* last-resort swallow — logging must not crash */ }
    };

    public ErrorSupervisor(string name, int maxRetries = 3, int cooldownMs = 30_000)
    {
        _name = name;
        _maxRetries = maxRetries;
        _cooldown = TimeSpan.FromMilliseconds(cooldownMs);
    }

    /// <summary>Guard a fallible operation. Returns a typed OneOf result.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public OneOf<T, AppError> Guard<T>(Func<OneOf<T, AppError>> operation,
        [CallerMemberName] string context = "")
    {
        // Circuit breaker: if open, short-circuit
        if (_isOpen)
        {
            if (DateTime.UtcNow - _circuitOpenedAt > _cooldown)
            {
                _isOpen = false; // half-open: allow one probe
            }
            else
            {
                FallbackLogger($"[Supervisor:{_name}] Circuit breaker open for '{context}'");
                return new AppError.IoFailed("CircuitBreaker",
                    _name, $"Supervisor '{_name}' circuit breaker is open for '{context}'");
            }
        }

        try
        {
            var result = operation();

            // Success resets the failure count
            if (result.Value is not AppError)
                _failureCount = 0;

            return result;
        }
        catch (Exception ex) when (ex is not (OutOfMemoryException or StackOverflowException
            or AccessViolationException or System.Runtime.InteropServices.SEHException))
        {
            _failureCount++;
            FallbackLogger($"[Supervisor:{_name}] Failure #{_failureCount} in '{context}': {ex.Message}");

            if (_failureCount >= _maxRetries)
            {
                _isOpen = true;
                _circuitOpenedAt = DateTime.UtcNow;
                FallbackLogger($"[Supervisor:{_name}] Circuit breaker OPENED for '{context}' after {_failureCount} failures");
            }

            return new AppError.IoFailed(context, _name, ex.Message);
        }
    }

    /// <summary>Async variant of Guard.</summary>
    public async Task<OneOf<T, AppError>> GuardAsync<T>(
        Func<Task<OneOf<T, AppError>>> operation,
        [CallerMemberName] string context = "")
    {
        if (_isOpen)
        {
            if (DateTime.UtcNow - _circuitOpenedAt > _cooldown)
                _isOpen = false;
            else
                return new AppError.IoFailed("CircuitBreaker",
                    _name, $"Supervisor '{_name}' circuit breaker is open for '{context}'");
        }

        try
        {
            var result = await operation().ConfigureAwait(false);
            if (result.Value is not AppError)
                _failureCount = 0;
            return result;
        }
        catch (Exception ex) when (ex is not (OutOfMemoryException or StackOverflowException
            or AccessViolationException or System.Runtime.InteropServices.SEHException))
        {
            _failureCount++;
            FallbackLogger($"[Supervisor:{_name}] Failure #{_failureCount} in '{context}': {ex.Message}");
            if (_failureCount >= _maxRetries)
            {
                _isOpen = true;
                _circuitOpenedAt = DateTime.UtcNow;
            }
            return new AppError.IoFailed(context, _name, ex.Message);
        }
    }

    /// <summary>Reset the circuit breaker manually.</summary>
    public void Reset()
    {
        _failureCount = 0;
        _isOpen = false;
    }
}
