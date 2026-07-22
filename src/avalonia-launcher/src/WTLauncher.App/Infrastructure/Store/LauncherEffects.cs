using Fluxor;
using WTLauncher.Core;

namespace WTLauncher.App.Infrastructure.Store;

public class LauncherEffects
{
    private readonly string _storePath;

    public LauncherEffects(string storePath) => _storePath = storePath;

    [EffectMethod]
    public async Task OnLoadStore(LoadStoreAction action, IDispatcher dispatcher)
    {
        var result = await Task.Run(() => LauncherStore.LoadErrorOr(_storePath));
        result.Switch(
            store => dispatcher.Dispatch(new LoadStoreResultAction(store)),
            errors => dispatcher.Dispatch(new LoadStoreFailedAction(errors.First().Description)));
    }

    [EffectMethod]
    public async Task OnSaveStore(SaveStoreAction action, IDispatcher dispatcher, IState<LauncherState> state)
    {
        if (state.Value.StoreFile is null)
        {
            dispatcher.Dispatch(new SaveStoreFailedAction("No store file to save."));
            return;
        }

        try
        {
            await Task.Run(() => LauncherStore.Save(state.Value.StoreFile, _storePath, backup: true));
            dispatcher.Dispatch(new SaveStoreResultAction());
        }
        catch (Exception ex) when (LauncherErrors.IsRecoverable(ex))
        {
            dispatcher.Dispatch(new SaveStoreFailedAction(ex.Message));
        }
    }
}
