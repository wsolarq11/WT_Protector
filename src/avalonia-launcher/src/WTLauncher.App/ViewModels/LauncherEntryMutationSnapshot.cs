using System.Collections.Immutable;
using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

public sealed class LauncherEntryMutationSnapshot
{
    private readonly List<LaunchEntry> _entries;
    private readonly LauncherPresetFile _presetFile;
    private readonly LauncherSettings _settings;

    private LauncherEntryMutationSnapshot(
        List<LaunchEntry> entries,
        LauncherPresetFile presetFile,
        LauncherSettings settings,
        string? selectedPresetId)
    {
        _entries = entries;
        _presetFile = presetFile;
        _settings = settings;
        SelectedPresetId = selectedPresetId;
    }

    private IReadOnlyList<LaunchEntry>? _cachedEntries;

    public IReadOnlyList<LaunchEntry> Entries => _cachedEntries ??= CloneEntries().AsReadOnly();

    public LauncherPresetFile PresetFile => ClonePresetFile();

    public IReadOnlyList<string> CustomEntryOrder => _settings.CustomEntryOrder;

    public string? SelectedPresetId { get; }

    public static LauncherEntryMutationSnapshot Capture(
        IEnumerable<LaunchEntry> entries,
        LauncherPresetFile presetFile,
        LauncherSettings settings,
        string? selectedPresetId)
    {
        return new LauncherEntryMutationSnapshot(
            [.. entries.Select(entry => entry with { })],
            LauncherPresetValidator.Validate(presetFile),
            settings,
            selectedPresetId);
    }

    public List<LaunchEntry> CloneEntries()
    {
        return [.. _entries.Select(entry => entry with { })];
    }

    public LauncherPresetFile ClonePresetFile()
    {
        return LauncherPresetValidator.Validate(_presetFile);
    }

    public LauncherSettings CloneSettings()
    {
        return _settings;
    }

    public ImmutableList<string> GetCustomEntryOrder()
    {
        return _settings.CustomEntryOrder;
    }
}
