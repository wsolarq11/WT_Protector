namespace WTLauncher.App.ViewModels;

/// <summary>Projection rebuild scope. VisibleOnly is the filter/sort/group path;
/// Mutation and FullReload also refresh selection-derived projections.</summary>
public enum ProjectionSyncMode
{
    VisibleOnly,
    Mutation,
    FullReload,
}

/// <summary>Single entry point for rebuilding entry-visible, grouping, and
/// selection-derived projections.</summary>
public interface IProjectionSynchronizer
{
    public void SynchronizeProjections(ProjectionSyncMode mode);
}
