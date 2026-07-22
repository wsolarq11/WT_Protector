using ErrorOr;

namespace WTLauncher.Core;

/// <summary>Common error types for the launcher domain.</summary>
public static class LauncherErrors
{
    public static Error ConfigLoadFailed(string details) =>
        Error.Failure("Config.LoadFailed", details);
    public static Error ConfigSaveFailed(string details) =>
        Error.Failure("Config.SaveFailed", details);
    public static Error EntryNotFound(string entryId) =>
        Error.NotFound("Entry.NotFound", $"Entry {entryId} not found");
    public static Error ValidationFailed(string details) =>
        Error.Validation("Validation.Failed", details);

    /// <summary>
    /// Returns <c>true</c> when the exception indicates the application can continue
    /// running safely. This is the canonical implementation shared by both Core and App.
    /// </summary>
    internal static bool IsRecoverable(Exception ex)
    {
        // Unwrap TypeInitializationException and AggregateException to inspect
        // the real cause before making the recoverability decision.
        for (var i = 0; i < 16 && ex is TypeInitializationException or AggregateException; i++)
        {
            if (ex.InnerException is null) break;
            ex = ex.InnerException;
            if (ex is AggregateException agg)
                ex = agg.Flatten().InnerExceptions.FirstOrDefault() ?? ex;
        }

        return ex is not (OutOfMemoryException or StackOverflowException
            or AccessViolationException or System.Runtime.InteropServices.SEHException);
    }
}

/// <summary>Extension methods for working with ErrorOr in the launcher context.</summary>
public static class ErrorOrExtensions
{
    /// <summary>Convert an exception-free value to ErrorOr.</summary>
    public static ErrorOr<T> ToErrorOr<T>(this T value) => value;

    /// <summary>Execute a fallible action and return ErrorOr.</summary>
    public static ErrorOr<T> Try<T>(Func<T> action, Func<Exception, Error> errorFactory)
    {
        try { return action(); }
        catch (Exception ex) when (LauncherErrors.IsRecoverable(ex))
        { return errorFactory(ex); }
    }

    /// <summary>Async version of Try.</summary>
    public static async Task<ErrorOr<T>> TryAsync<T>(Func<Task<T>> action, Func<Exception, Error> errorFactory)
    {
        try { return await action(); }
        catch (Exception ex) when (LauncherErrors.IsRecoverable(ex))
        { return errorFactory(ex); }
    }
}
