using System.Text.RegularExpressions;

namespace WTLauncher.Analyzers;

// WT1007: LauncherLocalization.Format key must exist in zh-CN.json.
// LauncherLocalization.Format falls back to the raw key when a key is missing,
// so a typo renders the literal key (e.g. "receipt.launchTitel") instead of the
// Chinese text — a silent drift at runtime. Enforce key/language-pack
// consistency at compile time, matching ResourceKeyAnalyzer (WT1005).
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class LocalizationKeyAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "WT1007";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        title: "Localization key must be defined in zh-CN.json",
        messageFormat: "Localization key '{0}' is passed to LauncherLocalization.Format but is not defined in zh-CN.json",
        category: "Correctness",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Every literal key passed to LauncherLocalization.Format must have a matching entry in the zh-CN.json language pack.",
        customTags: ["CompilationEnd"]);

    /// <summary>Extracts top-level keys from the flat zh-CN.json language pack.</summary>
    private static readonly Regex JsonKeyRegex = new(
        @"^\s*""([^""]+)""\s*:",
        RegexOptions.Compiled | RegexOptions.Multiline);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationAction(compilationContext =>
        {
            var definedKeys = CollectJsonKeys(compilationContext);
            if (definedKeys.Count == 0)
                return; // zh-CN.json not wired into AdditionalFiles — nothing to cross-check.

            foreach (var entry in CollectFormatKeys(compilationContext.Compilation))
            {
                if (!definedKeys.Contains(entry.Key))
                {
                    compilationContext.ReportDiagnostic(Diagnostic.Create(
                        Rule, entry.Value, entry.Key));
                }
            }
        });
    }

    private static HashSet<string> CollectJsonKeys(CompilationAnalysisContext context)
    {
        var keys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var additionalText in context.Options.AdditionalFiles)
        {
            if (!additionalText.Path.EndsWith("zh-CN.json", StringComparison.OrdinalIgnoreCase))
                continue;

            var sourceText = additionalText.GetText(default);
            if (sourceText is null)
                continue;

            foreach (Match match in JsonKeyRegex.Matches(sourceText.ToString()))
                keys.Add(match.Groups[1].Value);
        }
        return keys;
    }

    /// <summary>Collects literal keys passed to LauncherLocalization.Format, deduped
    /// to the first reference location so one missing key reports once with a pointer.</summary>
    private static Dictionary<string, Location> CollectFormatKeys(Compilation compilation)
    {
        var keys = new Dictionary<string, Location>(StringComparer.Ordinal);
        foreach (var syntaxTree in compilation.SyntaxTrees)
        {
            var root = syntaxTree.GetRoot();
            foreach (var invocation in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                if (invocation.Expression is not MemberAccessExpressionSyntax member)
                    continue;
                if (member.Name.Identifier.ValueText != "Format")
                    continue;
                // Short form LauncherLocalization.Format or fully-qualified
                // WTLauncher.App.Localization.LauncherLocalization.Format.
                if (!member.Expression.ToString().EndsWith("LauncherLocalization", StringComparison.Ordinal))
                    continue;
                if (invocation.ArgumentList.Arguments.Count == 0)
                    continue;
                if (invocation.ArgumentList.Arguments[0].Expression is LiteralExpressionSyntax literal
                    && literal.IsKind(SyntaxKind.StringLiteralExpression))
                {
                    var key = literal.Token.ValueText;
                    if (!keys.ContainsKey(key))
                        keys.Add(key, literal.GetLocation());
                }
            }
        }
        return keys;
    }
}
