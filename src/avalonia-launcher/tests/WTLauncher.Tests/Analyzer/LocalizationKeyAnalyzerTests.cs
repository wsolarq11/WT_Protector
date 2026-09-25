using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using WTLauncher.Analyzers;

namespace WTLauncher.Tests.Analyzer;

public sealed class LocalizationKeyAnalyzerTests
{
    private const string Shim = """
        public static class LauncherLocalization
        {
            public static string Format(string key, params object?[] args) => key;
        }
        """;

    private const string Json = """
        {
          "present.key": "存在"
        }
        """;

    [Fact]
    public void MissingKey_ReportsDiagnostic()
    {
        var source = Shim + """
            public static class C
            {
                public static string M() => LauncherLocalization.Format("missing.key");
            }
            """;

        var diagnostics = AnalyzerTestHelper.RunWithAdditionalTexts<LocalizationKeyAnalyzer>(
            source,
            [new JsonText("zh-CN.json", Json)]);

        var wt1006 = Assert.Single(diagnostics, d => d.Id == LocalizationKeyAnalyzer.DiagnosticId);
        Assert.Contains("missing.key", wt1006.GetMessage(), StringComparison.Ordinal);
    }

    [Fact]
    public void DefinedKey_ReportsNothing()
    {
        var source = Shim + """
            public static class C
            {
                public static string M() => LauncherLocalization.Format("present.key");
            }
            """;

        var diagnostics = AnalyzerTestHelper.RunWithAdditionalTexts<LocalizationKeyAnalyzer>(
            source,
            [new JsonText("zh-CN.json", Json)]);

        Assert.DoesNotContain(diagnostics, d => d.Id == LocalizationKeyAnalyzer.DiagnosticId);
    }

    [Fact]
    public void NonLiteralKey_ReportsNothing()
    {
        var source = Shim + """
            public static class C
            {
                public static string M(string key) => LauncherLocalization.Format(key);
            }
            """;

        var diagnostics = AnalyzerTestHelper.RunWithAdditionalTexts<LocalizationKeyAnalyzer>(
            source,
            [new JsonText("zh-CN.json", Json)]);

        Assert.DoesNotContain(diagnostics, d => d.Id == LocalizationKeyAnalyzer.DiagnosticId);
    }

    private sealed class JsonText(string path, string text) : AdditionalText
    {
        public override string Path => path;

        public override SourceText? GetText(CancellationToken cancellationToken = default) =>
            SourceText.From(text);
    }
}
