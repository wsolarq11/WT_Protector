using WTLauncher.Core;

namespace WTLauncher.App.Infrastructure.Store;

public sealed record LoadStoreAction;

public sealed record LoadStoreResultAction(LauncherStoreFile StoreFile);

public sealed record LoadStoreFailedAction(string Error);

public sealed record SaveStoreAction;

public sealed record SaveStoreResultAction;

public sealed record SaveStoreFailedAction(string Error);

public sealed record MutateStoreAction(Func<LauncherStoreFile, LauncherStoreFile> Transform);
