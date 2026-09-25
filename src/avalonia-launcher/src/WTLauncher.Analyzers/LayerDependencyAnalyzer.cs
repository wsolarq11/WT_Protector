namespace WTLauncher.Analyzers;

// WT1008: Layer/type dependency boundaries enforced at compile time.
// Replaces the hand-written ArchUnitNET rules (R4, R7-R14) in
// ArchitectureRules.cs. Dependency direction is a silent, expensive drift:
// a wrong reference compiles and runs but rots the layer boundary. Checking
// it with a semantic analyzer makes it fail at compile time, zero hand-written
// rule duplication.
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class LayerDependencyAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "WT1008";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        title: "Layer dependency boundary violated",
        messageFormat: "'{0}' must not depend on '{1}'",
        category: "Architecture",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "The reported type crosses a documented layer boundary. Route the dependency through the allowed boundary port instead.",
        customTags: ["CompilationEnd"]);

    private sealed class DependencyRule
    {
        public DependencyRule(string from, bool fromIsType, string to, bool toIsType, string[] exempt)
        {
            From = from;
            FromIsType = fromIsType;
            To = to;
            ToIsType = toIsType;
            Exempt = exempt;
        }

        /// <summary>Source scope: a namespace prefix, or a type full name when FromIsType.</summary>
        public string From { get; }

        public bool FromIsType { get; }

        /// <summary>Forbidden target: a namespace prefix, or a type full name when ToIsType.</summary>
        public string To { get; }

        public bool ToIsType { get; }

        /// <summary>Source type full names that are allowed to cross this boundary.</summary>
        public string[] Exempt { get; }
    }

    private static readonly DependencyRule[] Rules =
    [
        // R4: ViewModels must not touch System.IO.File (persistence goes through LauncherJson.Save).
        new("WTLauncher.App.ViewModels", false, "System.IO.File", true, []),
        // R7/R8: Presets / Entries services own workflows, not pixels.
        new("WTLauncher.App.Features.Presets", false, "WTLauncher.App.Views", false, []),
        new("WTLauncher.App.Features.Entries", false, "WTLauncher.App.Views", false, []),
        // R9/R10: Presets / Entries must go through the single store boundary.
        new("WTLauncher.App.Features.Presets", false, "System.IO.File", true, []),
        new("WTLauncher.App.Features.Entries", false, "System.IO.File", true, []),
        // R11: Infrastructure sits below ViewModels.
        new("WTLauncher.App.Infrastructure", false, "WTLauncher.App.ViewModels", false, []),
        // R12: Dialog service must resolve views through injection, not construct them.
        new("WTLauncher.App.Features.Dialogs", false, "WTLauncher.App.Views", false, []),
        // R13: Core must not do direct file IO; File.Exists probes are exempt.
        new("WTLauncher.Core", false, "System.IO.File", true,
            ["WTLauncher.Core.LaunchArgumentBuilder", "WTLauncher.Core.AgentDiscoveryScanner"]),
        // R14: StoreGrain uses IStorePersistence, not LauncherStore statics.
        new("WTLauncher.App.Infrastructure.StoreGrain", true, "WTLauncher.Core.LauncherStore", true, []),
        // ServiceContract: business services must not depend on the UI framework.
        new("WTLauncher.App.Features.Entries", false, "Avalonia.Controls", false, []),
        new("WTLauncher.App.Features.Presets", false, "Avalonia.Controls", false, []),
        // ServiceContract: business services must not reference the root view model.
        new("WTLauncher.App.Features.Entries", false, "WTLauncher.App.ViewModels.MainWindowViewModel", true, []),
        new("WTLauncher.App.Features.Presets", false, "WTLauncher.App.ViewModels.MainWindowViewModel", true, []),
        new("WTLauncher.App.Features.Dialogs", false, "WTLauncher.App.ViewModels.MainWindowViewModel", true, []),
    ];

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(CheckType,
            SyntaxKind.ClassDeclaration,
            SyntaxKind.StructDeclaration,
            SyntaxKind.InterfaceDeclaration,
            SyntaxKind.EnumDeclaration,
            SyntaxKind.RecordDeclaration,
            SyntaxKind.RecordStructDeclaration);
    }

    private static void CheckType(SyntaxNodeAnalysisContext context)
    {
        var typeDecl = (BaseTypeDeclarationSyntax)context.Node;
        var typeSymbol = context.SemanticModel.GetDeclaredSymbol(typeDecl, context.CancellationToken);
        if (typeSymbol is null)
            return;

        var typeFullName = typeSymbol.ToDisplayString();
        var typeNamespace = typeSymbol.ContainingNamespace?.ToDisplayString() ?? string.Empty;

        var referencedTypes = CollectReferencedTypes(typeDecl, context.SemanticModel, context.CancellationToken);
        if (referencedTypes.Count == 0)
            return;

        foreach (var rule in Rules)
        {
            var fromMatches = rule.FromIsType
                ? typeFullName == rule.From
                : typeNamespace.StartsWith(rule.From, StringComparison.Ordinal);
            if (!fromMatches || rule.Exempt.Contains(typeFullName, StringComparer.Ordinal))
                continue;

            var hit = rule.ToIsType
                ? referencedTypes.Any(rt => rt.ToDisplayString() == rule.To)
                : referencedTypes.Any(rt =>
                    (rt.ContainingNamespace?.ToDisplayString() ?? string.Empty)
                    .StartsWith(rule.To, StringComparison.Ordinal));

            if (hit)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Rule, typeDecl.GetLocation(), typeFullName, rule.To));
            }
        }
    }

    /// <summary>Collects the distinct types referenced anywhere inside a type
    /// declaration — base lists, fields, properties, method bodies, attributes,
    /// generic constraints — resolved via the semantic model so short names
    /// (e.g. <c>File</c> after <c>using System.IO;</c>) are captured too.</summary>
    private static HashSet<INamedTypeSymbol> CollectReferencedTypes(
        BaseTypeDeclarationSyntax typeDecl,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        var result = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
        foreach (var node in typeDecl.DescendantNodes())
        {
            if (node is not (IdentifierNameSyntax or GenericNameSyntax
                or QualifiedNameSyntax or MemberAccessExpressionSyntax
                or ObjectCreationExpressionSyntax))
            {
                continue;
            }

            var symbolInfo = semanticModel.GetSymbolInfo(node, cancellationToken);
            var symbol = symbolInfo.Symbol ?? symbolInfo.CandidateSymbols.FirstOrDefault();
            if (symbol is null)
                continue;

            var referencedType = symbol switch
            {
                INamedTypeSymbol t => t,
                IMethodSymbol m => m.ContainingType,
                IFieldSymbol f => f.ContainingType,
                IPropertySymbol p => p.ContainingType,
                IEventSymbol e => e.ContainingType,
                _ => null,
            };

            if (referencedType is not null)
                result.Add(referencedType);
        }
        return result;
    }
}
