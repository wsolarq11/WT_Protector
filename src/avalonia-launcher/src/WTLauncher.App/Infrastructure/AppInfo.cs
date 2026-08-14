using System.Reflection;

namespace WTLauncher.App.Infrastructure;

public static class AppInfo
{
    public static string Version { get; } = ReadVersion();

    private static string ReadVersion()
    {
        var assembly = Assembly.GetEntryAssembly();
        var informational = assembly
            ?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion;
        if (!string.IsNullOrWhiteSpace(informational))
        {
            var withoutBuild = informational.Split('+')[0];
            if (!string.IsNullOrWhiteSpace(withoutBuild))
                return withoutBuild;
        }

        var version = assembly?.GetName().Version;
        return version?.ToString() ?? "0.1.0-dev";
    }
}
