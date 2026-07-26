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
        // Unwrap aggregates to find the real cause; LauncherErrors.IsRecoverable handles
        // unwrapping for production behavior.
        var inner = ex;
        while (inner is TypeInitializationException or AggregateException)
            inner = inner.InnerException ?? inner;
        if (inner is NullReferenceException or IndexOutOfRangeException or ArgumentNullException)
            Environment.FailFast("Logic crash (DEBUG): " + inner.Message, inner);
#endif
        return LauncherErrors.IsRecoverable(ex);
    }
}
