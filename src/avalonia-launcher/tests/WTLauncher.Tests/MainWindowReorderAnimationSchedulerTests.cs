using Avalonia;
using WTLauncher.App.Views;

namespace WTLauncher.Tests;

public sealed class MainWindowReorderAnimationSchedulerTests
{
    [Fact]
    public void ScheduleIgnoresEmptySnapshotWithoutTouchingSubscriptions()
    {
        var scheduler = new MainWindowReorderAnimationScheduler();
        var calls = new List<string>();

        var scheduled = scheduler.Schedule(
            [],
            Handler,
            _ => calls.Add("unsubscribe"),
            _ => calls.Add("subscribe"));

        Assert.False(scheduled);
        Assert.Empty(calls);
        Assert.Null(scheduler.Consume(Handler, _ => calls.Add("consume-unsubscribe")));
        Assert.Equal(["consume-unsubscribe"], calls);
    }

    [Fact]
    public void ScheduleStoresSnapshotAndReattachesBeforeSubscribing()
    {
        var scheduler = new MainWindowReorderAnimationScheduler();
        var calls = new List<string>();
        var snapshot = Snapshot("one");

        var scheduled = scheduler.Schedule(
            snapshot,
            Handler,
            _ => calls.Add("unsubscribe"),
            _ => calls.Add("subscribe"));

        Assert.True(scheduled);
        Assert.Equal(["unsubscribe", "subscribe"], calls);
        Assert.Same(snapshot, scheduler.Consume(Handler, _ => calls.Add("consume-unsubscribe")));
        Assert.Equal(["unsubscribe", "subscribe", "consume-unsubscribe"], calls);
        Assert.Null(scheduler.Consume(Handler, _ => calls.Add("second-consume-unsubscribe")));
    }

    [Fact]
    public void ScheduleReplacesPendingSnapshot()
    {
        var scheduler = new MainWindowReorderAnimationScheduler();
        var first = Snapshot("one");
        var second = Snapshot("two");

        scheduler.Schedule(first, Handler, _ => { }, _ => { });
        scheduler.Schedule(second, Handler, _ => { }, _ => { });

        Assert.Same(second, scheduler.Consume(Handler, _ => { }));
    }

    private static Dictionary<string, Rect> Snapshot(string id)
    {
        return new Dictionary<string, Rect>(StringComparer.Ordinal)
        {
            [id] = new Rect(1, 2, 3, 4),
        };
    }

    private static void Handler(object? sender, EventArgs e)
    {
    }
}
