using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using WTLauncher.Analyzers;
using WTLauncher.Tests;

namespace WTLauncher.Tests.Analyzer;

[Trait("Category", TestCategories.Analyzer)]
public sealed class ResourceKeyAnalyzerTests
{
    [Fact]
    public void UndefinedStaticResource_ReportsDiagnostic()
    {
        var source = """
            public static class AppResources
            {
                public static void Inject()
                {
                    Resources["PrimaryBrush"] = "#000000";
                }
            }
            """;
        var xaml = """
            <ResourceDictionary>
              <SolidColorBrush x:Key="InlineBrush" Color="Red"/>
            </ResourceDictionary>
            <TextBlock Background="{StaticResource PrimaryBrush}"/>
            <TextBlock Foreground="{StaticResource MissingBrush}"/>
            """;

        var diagnostics = AnalyzerTestHelper.RunWithAdditionalTexts<ResourceKeyAnalyzer>(
            source,
            [new InMemoryAdditionalText("App.axaml", xaml)]);

        var wt1005 = Assert.Single(diagnostics, d => d.Id == ResourceKeyAnalyzer.DiagnosticId);
        Assert.Contains("MissingBrush", wt1005.GetMessage(), StringComparison.Ordinal);
    }

    [Fact]
    public void ExternalThemeResourceKey_DoesNotReport()
    {
        var source = """
            public static class AppResources
            {
                public static void Inject()
                {
                    Resources["PrimaryBrush"] = "#000000";
                }
            }
            """;
        var xaml = """
            <Button Theme="{StaticResource BorderlessButton}"/>
            <TextBlock Foreground="{StaticResource PrimaryBrush}"/>
            """;

        var diagnostics = AnalyzerTestHelper.RunWithAdditionalTexts<ResourceKeyAnalyzer>(
            source,
            [new InMemoryAdditionalText("App.axaml", xaml)]);

        Assert.DoesNotContain(diagnostics, d => d.Id == ResourceKeyAnalyzer.DiagnosticId);
    }

    private sealed class InMemoryAdditionalText(string path, string text) : AdditionalText
    {
        public override string Path => path;

        public override SourceText? GetText(CancellationToken cancellationToken = default) =>
            SourceText.From(text);
    }
}
