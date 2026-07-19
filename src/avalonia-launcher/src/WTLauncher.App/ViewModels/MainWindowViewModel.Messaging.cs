using CommunityToolkit.Mvvm.Messaging;
using WTLauncher.Core.Messaging;

namespace WTLauncher.App.ViewModels;

// ponytail: WeakReferenceMessenger subscriptions replace the eventual
// IEntryListHost/IEntryCardHost callback bags. As each feature ViewModel is
// extracted, the shell receives feature messages here instead of holding
// direct references. Minimal subscription set for now — expanded as
// migration progresses.
public sealed partial class MainWindowViewModel
{
    private void SubscribeToMessages()
    {
        WeakReferenceMessenger.Default.Register<EntryAddedMessage>(this, static (r, m) =>
            ((MainWindowViewModel)r).OnEntryAdded(m));

        WeakReferenceMessenger.Default.Register<EntryDeletedMessage>(this, static (r, m) =>
            ((MainWindowViewModel)r).OnEntryDeleted(m));
    }

    private void UnsubscribeFromMessages()
    {
        WeakReferenceMessenger.Default.Unregister<EntryAddedMessage>(this);
        WeakReferenceMessenger.Default.Unregister<EntryDeletedMessage>(this);
    }

    private void OnEntryAdded(EntryAddedMessage message)
    {
        RefreshAllProjections();
    }

    private void OnEntryDeleted(EntryDeletedMessage message)
    {
        RefreshAllProjections();
    }
}
