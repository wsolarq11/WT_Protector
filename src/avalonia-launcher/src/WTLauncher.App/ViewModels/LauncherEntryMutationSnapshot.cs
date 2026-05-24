using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

public sealed class LauncherEntryMutationSnapshot
{
    private LauncherEntryMutationSnapshot(
        List<LaunchEntry> entries,
        LauncherPresetFile presetFile,
        List<string> customEntryOrder,
        string? selectedPresetId)
    {
        Entries = entries;
        PresetFile = presetFile;
        CustomEntryOrder = customEntryOrder;
        SelectedPresetId = selectedPresetId;
    }

    public IReadOnlyList<LaunchEntry> Entries { get; }

    public LauncherPresetFile PresetFile { get; }

    public IReadOnlyList<string> CustomEntryOrder { get; }

    public string? SelectedPresetId { get; }

    public static LauncherEntryMutationSnapshot Capture(
        IEnumerable<LaunchEntry> entries,
        LauncherPresetFile presetFile,
        IEnumerable<string> customEntryOrder,
        string? selectedPresetId)
    {
        return new LauncherEntryMutationSnapshot(
            LauncherEntries.Clone(entries),
            LauncherPresetFiles.Clone(presetFile),
            customEntryOrder.ToList(),
            selectedPresetId);
    }

    public List<LaunchEntry> CloneEntries()
    {
        return LauncherEntries.Clone(Entries);
    }

    public LauncherPresetFile ClonePresetFile()
    {
        return LauncherPresetFiles.Clone(PresetFile);
    }

    public List<string> CloneCustomEntryOrder()
    {
        return new List<string>(CustomEntryOrder);
    }
}
