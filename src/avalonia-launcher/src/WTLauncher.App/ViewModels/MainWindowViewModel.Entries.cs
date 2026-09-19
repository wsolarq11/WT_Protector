using CommunityToolkit.Mvvm.Input;
using WTLauncher.App.Localization;

namespace WTLauncher.App.ViewModels;

// Entry Add/Clone/Edit/Delete command surface. The actual add/clone/edit/delete
// flows live in EntryListViewModel; collection persistence lives in
// LauncherEntryCommandWorkflow.

public sealed partial class MainWindowViewModel
{
    [RelayCommand]
    private Task AddEntryAsync() => _list.AddEntryAsync();

    internal Task CloneEntryAsync(LauncherEntryViewModel item) =>
        _list.CloneEntryAsync(item);

    internal Task EditEntryAsync(LauncherEntryViewModel item) =>
        _list.EditEntryAsync(item);

    internal Task DeleteEntryAsync(LauncherEntryViewModel item) =>
        _list.DeleteEntryAsync(item);

    private bool SaveEntryCollectionChanges() => _entryWorkflow.SaveEntryCollectionChanges();

#pragma warning disable CA1822
    public string DetailNameLabel => LauncherLocalization.Format("view.detailName");
    public string DetailDirectoryLabel => LauncherLocalization.Format("view.detailDirectory");
    public string DetailCommandLabel => LauncherLocalization.Format("view.detailCommand");
    public string NoMatchText => LauncherLocalization.Format("view.noMatch");
    public string NoMatchAutomation => LauncherLocalization.Format("view.noMatchAutomation");
    public string WelcomeTitle => LauncherLocalization.Format("view.welcomeTitle");
    public string WelcomeBody => LauncherLocalization.Format("view.welcomeBody");
    public string AddFirstLabel => LauncherLocalization.Format("view.addFirst");
    public string AddFirstAutomation => LauncherLocalization.Format("view.addFirst");
    public string DiscoverToolsLabel => LauncherLocalization.Format("view.discoverTools");
    public string DiscoverToolsAutomation => LauncherLocalization.Format("view.discoverTools");
#pragma warning restore CA1822
}
