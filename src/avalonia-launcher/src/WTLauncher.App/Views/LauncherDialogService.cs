using Avalonia.Controls;
using WTLauncher.App.ViewModels;
using WTLauncher.Core;

namespace WTLauncher.App.Views;

internal static class LauncherDialogService
{
    public static Task<bool> ConfirmLaunchAsync(Window owner, LaunchEntry entry, LaunchPlan plan)
    {
        return new ConfirmLaunchWindow(entry, plan).ShowDialog<bool>(owner);
    }

    public static Task<bool> ConfirmBatchLaunchAsync(Window owner, IReadOnlyList<LaunchEntry> entries)
    {
        return new ConfirmBatchLaunchWindow(entries).ShowDialog<bool>(owner);
    }

    public static Task<string?> RequestNewPresetAsync(Window owner, IReadOnlyList<string> selectedEntryIds)
    {
        return new PresetDetailsWindow(selectedEntryIds).ShowDialog<string?>(owner);
    }

    public static Task<bool> ConfirmDeletePresetAsync(Window owner, LauncherPresetViewModel preset)
    {
        return new ConfirmPresetDeleteWindow(preset).ShowDialog<bool>(owner);
    }

    public static Task<EntryDraft?> RequestEntryDraftAsync(Window owner, LaunchEntry? entry)
    {
        return new EntryDetailsWindow(entry).ShowDialog<EntryDraft?>(owner);
    }

    public static Task<bool> ConfirmDeleteEntryAsync(Window owner, LauncherEntryViewModel entry)
    {
        return new ConfirmEntryDeleteWindow(entry).ShowDialog<bool>(owner);
    }
}
