using System.Collections.Immutable;
using WTLauncher.Core;

namespace WTLauncher.App.Features.Entries;

/// <summary>Snapshot of shell state (settings + presets) needed to roll back
/// an entry mutation. Entry collection state is managed separately by the
/// entry ViewModel.</summary>
public sealed record ShellSnapshot(
    LauncherSettings Settings,
    LauncherPresetFile PresetFile,
    string? SelectedPresetId);
