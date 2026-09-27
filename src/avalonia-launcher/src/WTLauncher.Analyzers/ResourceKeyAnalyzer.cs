// WT1005: StaticResource key must be defined in InjectDesignTokens or inline XAML
// Reports diagnostic at the end of compilation when a XAML StaticResource
// key reference has no matching Resources["Key"] assignment in
// App.axaml.cs::InjectDesignTokens() or inline x:Key definition in .axaml.

using System.Collections.Immutable;
using System.Text.RegularExpressions;

namespace WTLauncher.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ResourceKeyAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "WT1005";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        title: "StaticResource key must be defined in InjectDesignTokens or inline XAML",
        messageFormat: "StaticResource key '{0}' is used in XAML but not defined in InjectDesignTokens or via x:Key in any .axaml file",
        category: "Correctness",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Every StaticResource key used in XAML must have a matching Resources[key] assignment or inline x:Key definition.",
        customTags: ["CompilationEnd"]);

    /// <summary>Regex to extract StaticResource key references from XAML.</summary>
    private static readonly Regex StaticResourceRegex = new(
        @"\{StaticResource\s+(\w+)\}",
        RegexOptions.Compiled);

    /// <summary>Regex to extract inline x:Key definitions from XAML.</summary>
    private static readonly Regex XamlKeyRegex = new(
        @"\bx:Key\s*=\s*""([^""]+)""",
        RegexOptions.Compiled);

    /// <summary>
    /// StaticResource keys provided by external theme libraries (Semi.Avalonia).
    /// These live inside a NuGet package, so the analyzer cannot see their x:Key
    /// definitions and must treat them as known-valid.
    /// </summary>
    private static readonly HashSet<string> ExternalThemeResourceKeys = new(StringComparer.Ordinal)
    {
        // Semi.Avalonia button ControlTheme variants (Semi Controls/Button.axaml)
        "SolidButton",
        "OutlineButton",
        "BorderlessButton",
    };

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        // Use RegisterCompilationAction for full-compilation cross-check
        context.RegisterCompilationAction(compilationContext =>
        {
            // Collect all defined resource keys: C# Resources["Key"] + XAML x:Key="Key"
            var definedKeys = CollectDefinedKeys(compilationContext.Compilation);
            foreach (var key in CollectXamlDefinedKeys(compilationContext))
                definedKeys.Add(key);

            // Collect all StaticResource references from XAML
            var referencedKeys = CollectXamlReferences(compilationContext);

            foreach (var key in referencedKeys)
            {
                if (!definedKeys.Contains(key) && !ExternalThemeResourceKeys.Contains(key))
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

    /// <summary>
    /// Collects StaticResource key references from .axaml AdditionalTexts
    /// (e.g., <c>{StaticResource ActiveBorderBrush}</c>).
    /// </summary>
    private static HashSet<string> CollectXamlReferences(CompilationAnalysisContext compilationContext)
    {
        var keys = new HashSet<string>(StringComparer.Ordinal);

        foreach (var additionalText in compilationContext.Options.AdditionalFiles)
        {
            if (!additionalText.Path.EndsWith(".axaml", StringComparison.OrdinalIgnoreCase))
                continue;

            var sourceText = additionalText.GetText(default);
            if (sourceText == null) continue;

            foreach (Match match in StaticResourceRegex.Matches(sourceText.ToString()))
            {
                keys.Add(match.Groups[1].Value);
            }
        }

        return keys;
    }

    /// <summary>
    /// Collects inline-defined resource keys from .axaml AdditionalTexts
    /// (e.g., <c>&lt;StreamGeometry x:Key="IconChevronDown"&gt;...&lt;/StreamGeometry&gt;</c>).
    /// </summary>
    private static HashSet<string> CollectXamlDefinedKeys(CompilationAnalysisContext compilationContext)
    {
        var keys = new HashSet<string>(StringComparer.Ordinal);

        foreach (var additionalText in compilationContext.Options.AdditionalFiles)
        {
            if (!additionalText.Path.EndsWith(".axaml", StringComparison.OrdinalIgnoreCase))
                continue;

            var sourceText = additionalText.GetText(default);
            if (sourceText == null) continue;

            foreach (Match match in XamlKeyRegex.Matches(sourceText.ToString()))
            {
                keys.Add(match.Groups[1].Value);
            }
        }

        return keys;
    }
}
