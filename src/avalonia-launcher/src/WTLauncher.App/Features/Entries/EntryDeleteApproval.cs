using WTLauncher.App.ViewModels;

namespace WTLauncher.App.Features.Entries;

public sealed record EntryDeleteApproval(
    LauncherEntryDeleteTarget Target,
    string? ClearKeyboardFocusId);
