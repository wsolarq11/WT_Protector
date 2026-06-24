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
    internal static bool IsRecoverable(Exception ex) =>
        ex is not (OutOfMemoryException or StackOverflowException
            or AccessViolationException or System.Runtime.InteropServices.SEHException);
}
