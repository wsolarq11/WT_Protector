using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

public sealed class LauncherEntryMutationSnapshot
{
    private readonly List<LaunchEntry> _entries;
    private readonly LauncherPresetFile _presetFile;
    private readonly List<string> _customEntryOrder;

    private LauncherEntryMutationSnapshot(
        List<LaunchEntry> entries,
        LauncherPresetFile presetFile,
        List<string> customEntryOrder,
        string? selectedPresetId)
    {
        _entries = entries;
        _presetFile = presetFile;
        _customEntryOrder = customEntryOrder;
        SelectedPresetId = selectedPresetId;
    }

    public IReadOnlyList<LaunchEntry> Entries => CloneEntries().AsReadOnly();

    public LauncherPresetFile PresetFile => ClonePresetFile();

    public IReadOnlyList<string> CustomEntryOrder => CloneCustomEntryOrder().AsReadOnly();

    public string? SelectedPresetId { get; }

    public static LauncherEntryMutationSnapshot Capture(
        IEnumerable<LaunchEntry> entries,
        LauncherPresetFile presetFile,
        IEnumerable<string> customEntryOrder,
        string? selectedPresetId)
    {
        return new LauncherEntryMutationSnapshot(
            [.. entries.Select(entry => entry.Clone())],
            LauncherPresetFiles.Clone(presetFile),
            [.. customEntryOrder],
            selectedPresetId);
    }

    public List<LaunchEntry> CloneEntries()
    {
        return [.. _entries.Select(entry => entry.Clone())];
    }

    public LauncherPresetFile ClonePresetFile()
    {
        return LauncherPresetFiles.Clone(_presetFile);
    }

    public List<string> CloneCustomEntryOrder()
    {
        return [.. _customEntryOrder];
    }
}
