namespace WTLauncher.App.ViewModels;

public sealed record EntryDraft(
    string Id,
    string Name,
    string Directory,
    string Command,
    bool Selected);
