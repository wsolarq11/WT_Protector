using System.Text.Json;
using System.Text.Json.Serialization;

namespace WTLauncher.Core;

[JsonConverter(typeof(ShellCommandJsonConverter))]
public readonly record struct ShellCommand
{
    public const string DefaultShell = "pwsh";

    public string Executable { get; }

    public string ArgumentsSuffix { get; }

    private ShellCommand(string executable, string argumentsSuffix)
    {
        Executable = executable;
        ArgumentsSuffix = argumentsSuffix;
    }

    public static ShellCommand Resolve(string? shellName)
    {
        var name = string.IsNullOrWhiteSpace(shellName) ? DefaultShell : shellName.Trim();

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

        throw new FileNotFoundException($"未找到 shell 可执行文件: {name}。请安装 PowerShell Core (pwsh) 或配置其他 shell。");
    }

    public static ShellCommand Default() => new(DefaultShell, $"-- {DefaultShell} -NoExit -EncodedCommand");

    public static implicit operator string(ShellCommand cmd) => cmd.Executable;
    public override string ToString() => Executable;
}

public sealed class ShellCommandJsonConverter : JsonConverter<ShellCommand>
{
    public override ShellCommand Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var raw = reader.GetString();
        try { return ShellCommand.Resolve(raw); }
        catch (FileNotFoundException) { return ShellCommand.Default(); }
    }

    public override void Write(Utf8JsonWriter writer, ShellCommand value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.Executable);
}
