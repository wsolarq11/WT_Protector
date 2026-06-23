namespace WTLauncher.App.ViewModels;

internal static class ExceptionFilters
{
    internal static bool IsRecoverable(Exception ex) =>
        ex is not (OutOfMemoryException or StackOverflowException);
}
