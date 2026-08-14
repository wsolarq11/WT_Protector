using WTLauncher.Analyzers;

// ponytail: inline arrays in test assertions — perf-irrelevant in tests.
#pragma warning disable CA1861

using WTLauncher.Tests;
namespace WTLauncher.Tests.Analyzer;

/// <summary>Tests for <see cref="CatchExceptionMustHaveWhenAnalyzer"/> (WT1004).</summary>
[Trait("Category", TestCategories.Analyzer)]
public sealed class CatchExceptionAnalyzerTests
{
    private const string ShimExceptionFilters = @"
namespace WTLauncher.App.ViewModels
{
    internal static class ExceptionFilters
    {
        internal static bool IsRecoverable(System.Exception ex) => true;
    }
}
";

    private const string ShimLauncherErrors = @"
namespace WTLauncher.Core
{
    internal static class LauncherErrors
    {
        internal static bool IsRecoverable(System.Exception ex) => true;
    }
}
";

    private const string ValidFilter = @"
using System;
using WTLauncher.App.ViewModels;
public class C {
    public void M() {
        try { }
        catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex)) { }
    }
}
";

    private const string ValidCoreFilter = @"
using System;
using WTLauncher.Core;
public class C {
    public void M() {
        try { }
        catch (Exception ex) when (LauncherErrors.IsRecoverable(ex)) { }
    }
}
";

    [Fact]
    public void BareCatchException_ReportsDiagnostic()
    {
        const string source = @"
using System;
public class C {
    public void M() {
        try { }
        catch (Exception ex) { }
    }
}
";
        var diags = AnalyzerTestHelper.Run<CatchExceptionMustHaveWhenAnalyzer>(source);
        var wt1004 = Assert.Single(diags, d => d.Id == CatchExceptionMustHaveWhenAnalyzer.DiagnosticId);
        Assert.Contains("Exception", wt1004.GetMessage(), StringComparison.Ordinal);
    }

    [Fact]
    public void CatchExceptionWithInvalidWhen_ReportsDiagnostic()
    {
        const string source = @"
using System;
public class C {
    public void M() {
        try { }
        catch (Exception ex) when (true) { }
    }
}
";
        var diags = AnalyzerTestHelper.Run<CatchExceptionMustHaveWhenAnalyzer>(source);
        var wt1004 = Assert.Single(diags, d => d.Id == CatchExceptionMustHaveWhenAnalyzer.DiagnosticId);
        Assert.Contains("Exception", wt1004.GetMessage(), StringComparison.Ordinal);
    }

    [Fact]
    public void CatchExceptionWithApprovedAppFilter_NoDiagnostic()
    {
        var diags = AnalyzerTestHelper.Run<CatchExceptionMustHaveWhenAnalyzer>(
            ValidFilter, assemblyName: null, ShimExceptionFilters, ShimLauncherErrors);
        Assert.DoesNotContain(diags,
            d => d.Id == CatchExceptionMustHaveWhenAnalyzer.DiagnosticId);
    }

    [Fact]
    public void CatchExceptionWithApprovedCoreFilter_NoDiagnostic()
    {
        var diags = AnalyzerTestHelper.Run<CatchExceptionMustHaveWhenAnalyzer>(
            ValidCoreFilter, assemblyName: null, ShimExceptionFilters, ShimLauncherErrors);
        Assert.DoesNotContain(diags,
            d => d.Id == CatchExceptionMustHaveWhenAnalyzer.DiagnosticId);
    }

    [Fact]
    public void CatchExceptionWithNarrowTypeFilter_NoDiagnostic()
    {
        // The rule message advertises "or narrow to specific exception types" —
        // a when-filter that is-patterns specific types is as safe as (stricter
        // than) the approved IsRecoverable filters. Regression: real App code
        // (ErrorLog, Program, LegacyMigration) relies on this acceptance.
        const string source = @"
using System;
using System.IO;
public class C {
    public void M() {
        try { }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
    }
}
";
        var diags = AnalyzerTestHelper.Run<CatchExceptionMustHaveWhenAnalyzer>(source);
        Assert.DoesNotContain(diags,
            d => d.Id == CatchExceptionMustHaveWhenAnalyzer.DiagnosticId);
    }

    [Fact]
    public void CatchExceptionWithNonNullFilter_ReportsDiagnostic()
    {
        // `when (ex is not null)` is a constant pattern, not type narrowing.
        const string source = @"
using System;
public class C {
    public void M() {
        try { }
        catch (Exception ex) when (ex is not null) { }
    }
}
";
        var diags = AnalyzerTestHelper.Run<CatchExceptionMustHaveWhenAnalyzer>(source);
        Assert.Single(diags, d => d.Id == CatchExceptionMustHaveWhenAnalyzer.DiagnosticId);
    }

    [Fact]
    public void CatchSpecificException_NoDiagnostic()
    {
        const string source = @"
using System;
public class C {
    public void M() {
        try { }
        catch (InvalidOperationException ex) { }
    }
}
";
        var diags = AnalyzerTestHelper.Run<CatchExceptionMustHaveWhenAnalyzer>(source);
        Assert.DoesNotContain(diags,
            d => d.Id == CatchExceptionMustHaveWhenAnalyzer.DiagnosticId);
    }

    [Fact]
    public void BareCatchWithoutType_ReportsDiagnostic()
    {
        const string source = @"
using System;
public class C {
    public void M() {
        try { }
        catch { }
    }
}
";
        var diags = AnalyzerTestHelper.Run<CatchExceptionMustHaveWhenAnalyzer>(source);
        var wt1004 = Assert.Single(diags, d => d.Id == CatchExceptionMustHaveWhenAnalyzer.DiagnosticId);
        Assert.Contains("catch", wt1004.GetMessage(), StringComparison.Ordinal);
    }
}
