using System.Collections.Immutable;
using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

public sealed class LauncherEntryMutationSnapshot
{
    private readonly List<LaunchEntry> _entries;
    private readonly LauncherPresetFile _presetFile;
    private readonly ImmutableList<string> _customEntryOrder;

    private LauncherEntryMutationSnapshot(
        List<LaunchEntry> entries,
        LauncherPresetFile presetFile,
        ImmutableList<string> customEntryOrder,
        string? selectedPresetId)
    {
        _entries = entries;
        _presetFile = presetFile;
        _customEntryOrder = customEntryOrder;
        SelectedPresetId = selectedPresetId;
    }

    public IReadOnlyList<LaunchEntry> Entries => CloneEntries().AsReadOnly();

    public LauncherPresetFile PresetFile => ClonePresetFile();

    public IReadOnlyList<string> CustomEntryOrder => GetCustomEntryOrder();

    public string? SelectedPresetId { get; }

    public static LauncherEntryMutationSnapshot Capture(
        IEnumerable<LaunchEntry> entries,
        LauncherPresetFile presetFile,
        IEnumerable<string> customEntryOrder,
        string? selectedPresetId)
    {
        return new LauncherEntryMutationSnapshot(
            [.. entries.Select(entry => entry with { })],
            LauncherPresetFiles.Clone(presetFile),
            customEntryOrder.ToImmutableList(),
            selectedPresetId);
    }

    public List<LaunchEntry> CloneEntries()
    {
        return [.. _entries.Select(entry => entry with { })];
    }

    public LauncherPresetFile ClonePresetFile()
    {
        return LauncherPresetFiles.Clone(_presetFile);
    }

    public ImmutableList<string> GetCustomEntryOrder()
    {
        return _customEntryOrder;
    }
}
