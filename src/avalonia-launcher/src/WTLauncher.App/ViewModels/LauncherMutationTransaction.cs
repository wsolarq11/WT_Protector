namespace WTLauncher.App.ViewModels;

internal static class LauncherMutationTransaction
{
    internal static string? Commit<TSnapshot>(
        TSnapshot snapshot,
        Func<string?> persist,
        Action<TSnapshot> restore)
    {
        var failure = persist();
        if (failure is not null)
            restore(snapshot);

        return failure;
    }
}
