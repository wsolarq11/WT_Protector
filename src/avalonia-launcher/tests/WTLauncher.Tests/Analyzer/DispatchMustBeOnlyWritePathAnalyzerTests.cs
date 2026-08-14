using WTLauncher.Analyzers;

// ponytail: inline arrays in test assertions — perf-irrelevant in tests.
#pragma warning disable CA1861

using WTLauncher.Tests;
namespace WTLauncher.Tests.Analyzer;

/// <summary>
/// Tests for <see cref="DispatchMustBeOnlyWritePathAnalyzer"/> (WT1003).
///
/// These tests use shim types that mirror the real persistence types to
/// simulate the App-layer scope (the analyzer's scope guard only activates
/// for "WTLauncher.App" compilation).
/// </summary>
[Trait("Category", TestCategories.Analyzer)]
public sealed class DispatchMustBeOnlyWritePathAnalyzerTests
{
    private const string ShimStoreGrain = @"
namespace WTLauncher.App.Infrastructure
{
    public class StoreGrain
    {
        public void Dispatch(System.Func<object, object> reducer) { }
        public void DispatchErrorOr(System.Func<object, object> reducer) { }
        internal void LazyInit() { }
    }
}
";

    private const string ShimLauncherStore = @"
namespace WTLauncher.Core
{
    public static class LauncherStore
    {
        public static void Save(object store, string path, bool backup) { }
    }
}
";

    private const string ShimIStorePersistence = @"
namespace WTLauncher.Core
{
    public interface IStorePersistence
    {
        void Save(object store, string path, bool backup);
    }
}
";

    private const string ShimILauncherJsonIO = @"
namespace WTLauncher.Core
{
    public interface ILauncherJsonIO
    {
        void Save<T>(T value, string path, bool backup) where T : notnull;
    }
}
";

    private const string ShimFileLauncherJsonIO = @"
namespace WTLauncher.App.Infrastructure
{
    public sealed class FileLauncherJsonIO
    {
        public void Save<T>(T value, string path, bool backup) where T : notnull { }
    }
}
";

    [Fact]
    public void DirectLauncherStoreSave_ReportsDiagnostic()
    {
        const string source = @"
using WTLauncher.Core;

public class C {
    public void M() {
        LauncherStore.Save(new object(), ""path"", false);
    }
}
";
        var diags = AnalyzerTestHelper.Run<DispatchMustBeOnlyWritePathAnalyzer>(
            source, assemblyName: "WTLauncher.App",
            ShimIStorePersistence, ShimLauncherStore, ShimILauncherJsonIO, ShimFileLauncherJsonIO);
        var wt1003 = Assert.Single(diags, d => d.Id == DispatchMustBeOnlyWritePathAnalyzer.DiagnosticId);
        Assert.Contains("Save", wt1003.GetMessage(), StringComparison.Ordinal);
    }

    [Fact]
    public void DirectIStorePersistenceSave_ReportsDiagnostic()
    {
        const string source = @"
using WTLauncher.Core;

public class C {
    public void M(IStorePersistence p) {
        p.Save(new object(), ""path"", false);
    }
}
";
        var diags = AnalyzerTestHelper.Run<DispatchMustBeOnlyWritePathAnalyzer>(
            source, assemblyName: "WTLauncher.App",
            ShimIStorePersistence, ShimLauncherStore, ShimILauncherJsonIO, ShimFileLauncherJsonIO);
        var wt1003 = Assert.Single(diags, d => d.Id == DispatchMustBeOnlyWritePathAnalyzer.DiagnosticId);
        Assert.Contains("Save", wt1003.GetMessage(), StringComparison.Ordinal);
    }

    [Fact]
    public void SaveInsideFileStorePersistence_NoDiagnostic()
    {
        // FileStorePersistence.Save calling _io.Save() is an allowed pattern
        // (the persistence implementation calling the lower-level I/O layer).
        const string source = @"
using WTLauncher.Core;
using WTLauncher.App.Infrastructure;

public class FileStorePersistence
{
    private readonly FileLauncherJsonIO _io = new();

    public void Save(object store, string path, bool backup) {
        _io.Save(store, path, backup);
    }
}
";
        var diags = AnalyzerTestHelper.Run<DispatchMustBeOnlyWritePathAnalyzer>(
            source, assemblyName: "WTLauncher.App",
            ShimIStorePersistence, ShimLauncherStore, ShimILauncherJsonIO, ShimFileLauncherJsonIO);
        Assert.DoesNotContain(diags,
            d => d.Id == DispatchMustBeOnlyWritePathAnalyzer.DiagnosticId);
    }

    [Fact]
    public void NotAppAssembly_NoDiagnostic()
    {
        const string source = @"
using WTLauncher.Core;

public class C {
    public void M() {
        LauncherStore.Save(new object(), ""path"", false);
    }
}
";
        var diags = AnalyzerTestHelper.Run<DispatchMustBeOnlyWritePathAnalyzer>(
            source, assemblyName: "WTLauncher.Tests",
            ShimIStorePersistence, ShimLauncherStore);
        Assert.DoesNotContain(diags,
            d => d.Id == DispatchMustBeOnlyWritePathAnalyzer.DiagnosticId);
    }
}
