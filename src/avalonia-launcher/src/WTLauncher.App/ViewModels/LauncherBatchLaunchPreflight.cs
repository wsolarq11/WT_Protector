using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

public sealed record LauncherBatchLaunchItem(
    LaunchEntry Entry,
    LaunchPlan Plan);

public sealed record LauncherBatchLaunchPreflight(
    IReadOnlyList<LauncherBatchLaunchItem> Items,
    LauncherBatchLaunchReport Report);
