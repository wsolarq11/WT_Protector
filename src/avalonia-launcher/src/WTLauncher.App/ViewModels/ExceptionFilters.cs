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
        // LauncherErrors.IsRecoverable (Core) has a bounded for-i<16 unwrap guard.
        // For the DEBUG FailFast check, we use the same bounded unwrap pattern.
        var inner = ex;
        for (var i = 0; i < 16 && inner is TypeInitializationException or AggregateException; i++)
        {
            if (inner.InnerException is null) break;
            inner = inner.InnerException;
            if (inner is AggregateException agg)
                inner = agg.Flatten().InnerExceptions.FirstOrDefault() ?? inner;
        }
        if (inner is NullReferenceException or IndexOutOfRangeException or ArgumentNullException)
            Environment.FailFast("Logic crash (DEBUG): " + inner.Message, inner);
#endif
        return LauncherErrors.IsRecoverable(ex);
    }
}
