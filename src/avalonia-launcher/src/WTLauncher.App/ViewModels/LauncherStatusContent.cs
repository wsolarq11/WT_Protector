namespace WTLauncher.App.ViewModels;

public sealed record LauncherStatusContent
{
    public LauncherStatusContent(string text, LauncherStatusTone tone)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        Text = text;
        Tone = tone;
    }

    public string Text { get; }

    public LauncherStatusTone Tone { get; }

    public static LauncherStatusContent Neutral(string text) => new(text, LauncherStatusTone.Neutral);

    public static LauncherStatusContent Success(string text) => new(text, LauncherStatusTone.Success);

    public static LauncherStatusContent Warning(string text) => new(text, LauncherStatusTone.Warning);

    public static LauncherStatusContent Error(string text) => new(text, LauncherStatusTone.Error);
}
