using System.Linq;

namespace WTLauncher.App.ViewModels;

internal static class ExceptionFilters
{
    /// <summary>
    /// Returns <c>true</c> when the exception indicates the application can continue
    /// running safely. "Recoverable" means the app survives — it does NOT mean the
    /// failed operation can be retried successfully. The filter passes through
    /// <see cref="TypeInitializationException"/> (use <see cref="Exception.InnerException"/>
    /// to inspect the real cause) and <see cref="AggregateException"/> (use
    /// <see cref="AggregateException.Flatten"/> to unwrap). Callers should still
    /// handle these gracefully — a <see cref="TypeInitializationException"/> may
    /// indicate an unrecoverable static constructor failure that will break
    /// subsequent use of the type.
    /// </summary>
    internal static bool IsRecoverable(Exception ex)
    {
        // Unwrap TypeInitializationException and AggregateException to inspect
        // the real cause before making the recoverability decision.
        // Max 16 iterations prevents infinite loops from self-referencing or
        // null-InnerException wrappers.
        for (var i = 0; i < 16 && ex is TypeInitializationException or AggregateException; i++)
        {
            ex = ex.InnerException ?? ex;
            if (ex is AggregateException agg)
                ex = agg.Flatten().InnerExceptions.FirstOrDefault() ?? ex;
        }

        return ex is not (OutOfMemoryException or StackOverflowException
            or AccessViolationException or System.Runtime.InteropServices.SEHException);
    }
}
