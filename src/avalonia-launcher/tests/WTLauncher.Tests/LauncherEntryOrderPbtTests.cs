using System.Collections.Immutable;
using CsCheck;
using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed class LauncherEntryOrderPbtTests
{
    /// <summary>Generates unique entry IDs without path separators (EntryId constraint).</summary>
    private static Gen<string[]> IdArrays => Gen.Int[0, 99999]
        .Select(i => $"id{i}")
        .Array[3, 8]
        .Select(ids => ids.Distinct().ToArray());

    [Fact]
    public void SyncWithEntries_PreservesAllKnownIds()
    {
        IdArrays
            .Sample(assert: ids =>
            {
                var customOrder = ImmutableList<string>.Empty;
                var result = LauncherEntryOrder.SyncWithEntries(customOrder, ids);
                Assert.True(ids.All(id => result.Contains(id)));
                Assert.Equal(ids.Length, result.Count);
            });
    }

    [Fact]
    public void SyncWithEntries_NoCustomOrder_FollowsInputOrder()
    {
        IdArrays
            .Sample(assert: ids =>
            {
                var result = LauncherEntryOrder.SyncWithEntries(
                    ImmutableList<string>.Empty, ids);
                Assert.Equal(ids.Length, result.Count);
                for (int i = 0; i < result.Count; i++)
                    Assert.Equal(ids[i], result[i]);
            });
    }

    [Fact]
    public void MoveRelative_PreservesAllEntries()
    {
        IdArrays
            .Select(ids => ids.ToImmutableList())
            .Sample(assert: ids =>
            {
                var (changed, updated) = LauncherEntryOrder.MoveRelative(
                    ids, ids[0], ids[^1], insertAfter: true);
                Assert.True(changed);
                Assert.Equal(ids.Count, updated.Count);
                Assert.True(ids.All(id => updated.Contains(id)));
            });
    }

    [Fact]
    public void MoveRelative_SameSourceAndTarget_NoChange()
    {
        IdArrays
            .Select(ids => ids.ToImmutableList())
            .Sample(assert: ids =>
            {
                var (changed, updated) = LauncherEntryOrder.MoveRelative(
                    ids, ids[0], ids[0], insertAfter: true);
                Assert.False(changed);
                Assert.True(updated.SequenceEqual(ids));
            });
    }

    [Fact]
    public void BuildIndex_CoversAllInputIds()
    {
        IdArrays
            .Select(ids => ids.ToImmutableList())
            .Sample(assert: ids =>
            {
                var index = LauncherEntryOrder.BuildIndex(ids);
                Assert.Equal(ids.Count, index.Count);
                foreach (var id in ids)
                    Assert.True(index.ContainsKey(id));
                var values = index.Values.ToHashSet();
                Assert.Equal(ids.Count, values.Count);
                Assert.True(values.All(v => v >= 0 && v < ids.Count));
            });
    }

    [Fact]
    public void NormalizeSortModeId_IsIdempotent()
    {
        var knownModes = new[] { "custom", "recent", "updated", "alphabetical" };
        foreach (var mode in knownModes)
        {
            var first = LauncherEntryQuery.NormalizeSortModeId(mode);
            var second = LauncherEntryQuery.NormalizeSortModeId(first);
            Assert.Equal(first, second);
        }
    }
}
