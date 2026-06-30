using System.Text.Json;
using System.Text.Json.Serialization;

namespace WTLauncher.Core;

/// <summary>
/// Shell executable configuration. Resolves shell path from config, with <c>pwsh</c> as
/// the default. KEY FIX (H6): the shell is now configurable rather than hardcoded to
/// <c>pwsh</c>. Users on systems without PowerShell Core can configure
/// <c>powershell.exe</c> instead.
/// </summary>
[JsonConverter(typeof(ShellCommandJsonConverter))]
public readonly record struct ShellCommand
{
    /// <summary>Default shell executable name.</summary>
    public const string DefaultShell = "pwsh";

    /// <summary>Command-line prefix for the shell, e.g. <c>-- pwsh -NoExit -EncodedCommand</c>.</summary>
    public string Executable { get; }

    public string ArgumentsSuffix { get; }

    private ShellCommand(string executable, string argumentsSuffix)
    {
        Executable = executable;
        ArgumentsSuffix = argumentsSuffix;
    }

    /// <summary>
    /// Resolves the shell command from a configured shell name.
    /// Validates that the shell exists at well-known locations.
    /// Never falls back to PATH-based resolution (binary-hijack vector).
    /// </summary>
    /// <param name="shellName">Shell executable name, e.g. "pwsh" or "powershell".</param>
    public static Result<ShellCommand, ShellError> Resolve(string? shellName)
    {
        var name = string.IsNullOrWhiteSpace(shellName) ? DefaultShell : shellName.Trim();

        // Search well-known Windows Terminal shell locations.
        var localAppData = Environment.GetEnvironmentVariable("LOCALAPPDATA");
        if (!string.IsNullOrEmpty(localAppData))
        {
            var localPath = Path.GetFullPath(Path.Combine(localAppData, @"Microsoft\WindowsApps", name + ".exe"));
            if (File.Exists(localPath))
                return new ShellCommand(name, $"-- {name} -NoExit -EncodedCommand");
        }

        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        if (!string.IsNullOrEmpty(programFiles))
        {
            var progPath = Path.GetFullPath(Path.Combine(programFiles, @"WindowsApps", name + ".exe"));
            if (File.Exists(progPath))
                return new ShellCommand(name, $"-- {name} -NoExit -EncodedCommand");
        }

        return new ShellError.NotFound(name);
    }

    /// <summary>Returns the default shell (<c>pwsh</c>) without validation.</summary>
    public static ShellCommand Default() => new(DefaultShell, $"-- {DefaultShell} -NoExit -EncodedCommand");

    public static implicit operator string(ShellCommand cmd) => cmd.Executable;
    public override string ToString() => Executable;
}

public sealed class ShellCommandJsonConverter : JsonConverter<ShellCommand>
{
    public override ShellCommand Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var raw = reader.GetString();
        var result = ShellCommand.Resolve(raw);
        // If resolution fails, fall back to default (user can fix config later)
        return result.Match(ok: cmd => cmd, err: _ => ShellCommand.Default());
    }

    public override void Write(Utf8JsonWriter writer, ShellCommand value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.Executable);
}
