using WTLauncher.Core;

namespace WTLauncher.App.Infrastructure;

/// <summary>
/// One-mutation save transaction: a caller mutates ViewModel state, then commits
/// a single LauncherStoreFile through StoreGrain. On save failure or disposal
/// without commit, the provided restore callback is invoked.
/// </summary>
public sealed class StoreMutationSession : IDisposable
{
    private readonly Func<LauncherStoreFile, string?> _save;
    private readonly Action _restore;
    private bool _committed;
    private bool _disposed;

    public StoreMutationSession(Func<LauncherStoreFile, string?> save, Action restore)
    {
        _save = save ?? throw new ArgumentNullException(nameof(save));
        _restore = restore ?? throw new ArgumentNullException(nameof(restore));
    }

    public string? Commit(LauncherStoreFile next)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_committed) throw new InvalidOperationException("Store mutation session can only commit once.");

        _committed = true;
        var failure = _save(next);
        if (failure is not null)
            _restore();
        return failure;
    }

    public void Dispose()
    {
        if (_disposed) return;
        if (!_committed)
            _restore();
        _disposed = true;
    }
}