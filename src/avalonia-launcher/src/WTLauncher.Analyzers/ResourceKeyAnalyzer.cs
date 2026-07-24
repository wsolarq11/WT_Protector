// WT1005: StaticResource key must be defined in InjectDesignTokens
// Reports diagnostic at the end of compilation when a XAML StaticResource
// key reference has no matching Resources["Key"] assignment in
// App.axaml.cs::InjectDesignTokens().

using System.Collections.Immutable;

namespace WTLauncher.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ResourceKeyAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "WT1005";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        title: "StaticResource key must be defined in InjectDesignTokens",
        messageFormat: "StaticResource key '{0}' is not assigned in InjectDesignTokens",
        category: "Correctness",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Every StaticResource key used in XAML must have a matching Resources[key] assignment.",
        customTags: ["CompilationEnd"]);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        // Use RegisterCompilationAction for full-compilation cross-check
        context.RegisterCompilationAction(compilationContext =>
        {
            var definedKeys = CollectDefinedKeys(compilationContext.Compilation);
            var xamlKeys = CollectXamlKeys(compilationContext.Compilation);

            foreach (var key in xamlKeys)
            {
                if (!definedKeys.Contains(key))
                {
                    compilationContext.ReportDiagnostic(Diagnostic.Create(
                        Rule, Location.None, key));
                }
            }
        });
    }

    private static HashSet<string> CollectDefinedKeys(Compilation compilation)
    {
        var keys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var syntaxTree in compilation.SyntaxTrees)
        {
            var root = syntaxTree.GetRoot();
            foreach (var assignment in root.DescendantNodes()
                .OfType<AssignmentExpressionSyntax>()
                .Where(a => a.Left is ElementAccessExpressionSyntax elem
                    && elem.ArgumentList.Arguments.Count == 1
                    && elem.Expression.ToString() is "Resources"))
            {
                if (((ElementAccessExpressionSyntax)assignment.Left)
                    .ArgumentList.Arguments[0].Expression is LiteralExpressionSyntax literal
                    && literal.IsKind(SyntaxKind.StringLiteralExpression))
                {
                    keys.Add(literal.Token.ValueText);
                }
            }
        }
        return keys;
    }

    private static HashSet<string> CollectXamlKeys(Compilation compilation)
    {
        // Phase 1: scaffolding — returns empty set.
        // Full implementation reads .axaml AdditionalTexts and extracts
        // {StaticResource KeyName} references via regex.
        return [];
    }
}
