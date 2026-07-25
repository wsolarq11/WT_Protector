using System.Runtime.CompilerServices;
using WTLauncher.App.Infrastructure;
using WTLauncher.Core;

namespace WTLauncher.Tests;

/// <summary>
/// One-time test assembly initialization.
/// Sets up <see cref="LauncherJson.DefaultIO"/> before any test runs,
/// since the facade now requires an explicit I/O implementation.
/// </summary>
internal static class TestStartup
{
    [ModuleInitializer]
    public static void Initialize()
    {
        LauncherJson.DefaultIO = new FileLauncherJsonIO();
        LauncherStore.DefaultPersistence = new FileStorePersistence();
    }
}
