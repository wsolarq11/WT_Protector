using WTLauncher.App.Features;
using WTLauncher.App.ViewModels;

namespace WTLauncher.App.Infrastructure;

/// <summary>Standardizes the catch-log-return pattern across the codebase.
/// Reduces boilerplate and ensures consistent exception handling.</summary>
internal static class ExceptionBoundary
{
    /// <summary>Synchronously executes an operation with standardized
    /// catch-log-return. Returns the operation's result on success, or a
    /// <see cref="ServiceFailure{T}"/> on recoverable failure.</summary>
    public static ServiceResult<T> Run<T>(
        Func<ServiceResult<T>> operation,
        string context) where T : notnull
    {
        try { return operation(); }
        catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
        {
            ErrorLog.Write(ex, context);
            return new ServiceFailure<T>(LauncherStatusContent.Error($"操作失败: {context}"));
        }
    }

    /// <summary>Asynchronously executes an operation with standardized
    /// catch-log-return. Returns the operation's result on success, or a
    /// <see cref="ServiceFailure{T}"/> on recoverable failure.</summary>
    public static async Task<ServiceResult<T>> RunAsync<T>(
        Func<Task<ServiceResult<T>>> operation,
        string context) where T : notnull
    {
        try { return await operation().ConfigureAwait(false); }
        catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
        {
            ErrorLog.Write(ex, context);
            return new ServiceFailure<T>(LauncherStatusContent.Error($"操作失败: {context}"));
        }
    }

    /// <summary>Synchronously executes an action with standardized
    /// catch-log. Returns the exception context on failure, null on success.</summary>
    public static string? RunAction(
        Action operation,
        string context)
    {
        try { operation(); return null; }
        catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
        {
            ErrorLog.Write(ex, context);
            return $"操作失败: {context}";
        }
    }

    /// <summary>Asynchronously executes an action with standardized
    /// catch-log. Returns the exception context on failure, null on success.</summary>
    public static async Task<string?> RunActionAsync(
        Func<Task> operation,
        string context)
    {
        try { await operation().ConfigureAwait(false); return null; }
        catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
        {
            ErrorLog.Write(ex, context);
            return $"操作失败: {context}";
        }
    }
}
