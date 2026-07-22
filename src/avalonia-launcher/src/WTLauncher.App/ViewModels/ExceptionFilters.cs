using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

/// <summary>
/// Returns <c>true</c> when the exception indicates the application can continue
/// running safely. Delegates to <see cref="LauncherErrors.IsRecoverable"/> for the
/// canonical implementation shared with ErrorOr-based code in WTLauncher.Core.
/// </summary>
internal static class ExceptionFilters
{
    internal static bool IsRecoverable(Exception ex)
    {
#if DEBUG
        // In DEBUG builds, rethrow logic errors so they crash the process during development.
        // Check before delegation so the crash is thrown from the App layer.
        var unwrapped = UnwrapAggregates(ex);
        if (unwrapped is NullReferenceException or IndexOutOfRangeException or ArgumentNullException)
            throw new InvalidOperationException("Logic crash (DEBUG): " + unwrapped.Message, unwrapped);
#endif
        return LauncherErrors.IsRecoverable(ex);
    }

    /// <summary>Unwrap TypeInitializationException and AggregateException wrappers.</summary>
    private static Exception UnwrapAggregates(Exception ex)
    {
        for (var i = 0; i < 16 && ex is TypeInitializationException or AggregateException; i++)
        {
            if (ex.InnerException is null) break;
            ex = ex.InnerException;
            if (ex is AggregateException agg)
                ex = agg.Flatten().InnerExceptions.FirstOrDefault() ?? ex;
        }
        return ex;
    }
}
