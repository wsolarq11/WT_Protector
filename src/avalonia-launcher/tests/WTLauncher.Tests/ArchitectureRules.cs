using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.Loader;
using ArchUnitNET.xUnitV3;
using WTLauncher.App.ViewModels;
using WTLauncher.Core;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace WTLauncher.Tests;

/// <summary>
/// Architecture invariants enforced at test time. Translates prose rules from
/// .trellis/spec/core/quality-guidelines.md into executable checks.
///
/// Layer boundaries, namespace ownership, and dependency direction are encoded here so
/// violations fail `dotnet test` immediately, not after human review.
/// </summary>
public sealed class ArchitectureRules
{
    private static readonly Architecture Architecture = new ArchLoader()
        .LoadAssemblies(typeof(LauncherStore).Assembly, typeof(MainWindowViewModel).Assembly)
        .Build();

    private static readonly IObjectProvider<IType> CoreLayer =
        Types().That().ResideInNamespace("WTLauncher.Core").As("Core layer");

    private static readonly IObjectProvider<IType> AppLayer =
        Types().That().ResideInNamespace("WTLauncher.App").As("App layer");

    private static readonly IObjectProvider<IType> ViewModelLayer =
        Types().That().ResideInNamespace("WTLauncher.App.ViewModels").As("ViewModel layer");

    // ── R1: Core must not reference Avalonia ─────────────────────────────
    //
    // Source: quality-guidelines.md invariant #1 ("Core never references Avalonia").
    // Core.csproj already enforces this at compile time (zero PackageReference);
    // this rule is a second line of defense against a future csproj slip.

    [Fact]
    public void R1_Core_MustNotReference_Avalonia()
    {
        IArchRule rule = Types().That().Are(CoreLayer)
            .Should().NotDependOnAnyTypesThat().ResideInNamespace("Avalonia")
            .Because("Core must stay UI-framework agnostic to remain unit-testable without Avalonia.");
        rule.Check(Architecture);
    }

    // ── R2: Core must not reference CommunityToolkit.Mvvm ────────────────
    //
    // Source: quality-guidelines.md invariant #2.

    [Fact]
    public void R2_Core_MustNotReference_CommunityToolkitMvvm()
    {
        IArchRule rule = Types().That().Are(CoreLayer)
            .Should().NotDependOnAnyTypesThat().ResideInNamespace("CommunityToolkit")
            .Because("Core must not depend on MVVM framework source-generator types.");
        rule.Check(Architecture);
    }

    // ── R3: Core must not depend on App ──────────────────────────────────
    //
    // Source: dependency-direction invariant (Layer 0 cannot reference Layer 1-4).

    [Fact]
    public void R3_Core_MustNotDependOn_App()
    {
        IArchRule rule = Types().That().Are(CoreLayer)
            .Should().NotDependOnAnyTypesThat().ResideInNamespace("WTLauncher.App")
            .Because("Layer 0 cannot reference Layer 1-4 (dependency direction).");
        rule.Check(Architecture);
    }

    // ── R4: ViewModels must not depend on System.IO.File ────────────────
    //
    // Source: quality-guidelines.md "forbidden patterns" — "Direct `File.WriteAllText`
    // outside `LauncherJson`". All persistence must go through the atomic-write wrapper
    // in Core so the temp-file + File.Replace contract is preserved.
    //
    // ponytail: type-level dependency check, not method-level. ArchUnitNET only puts
    // methods defined in loaded assemblies into MethodMembers — external framework
    // methods like System.IO.File.WriteAllText appear only as MethodCallDependency
    // targets, never as MethodMembers. A `NotCallAny(MethodMembers().HaveFullName(...))`
    // rule therefore matches an empty set and is vacuously true (no-op). The type-level
    // `NotDependOnAnyTypesThat().HaveFullName("System.IO.File")` formulation resolves
    // correctly via arch.Types dependency edges and actually catches violations.

    [Fact]
    public void R4_ViewModels_MustNot_DependOn_SystemIoFile()
    {
        IArchRule rule = Types().That().Are(ViewModelLayer)
            .Should().NotDependOnAnyTypesThat().HaveFullName("System.IO.File")
            .Because("persistence must go through LauncherJson.Save for atomic-write guarantees; ViewModels must not touch System.IO.File directly.");
        rule.Check(Architecture);
    }

    // ── R5: Only LauncherJson (writer) and known readers may depend on System.IO.File ──
    //
    // Single-writer invariant: atomic-write contract is upheld by centralizing writes
    // in LauncherJson.Save (which does temp-file + File.Replace). Other Core types
    // calling File.WriteAllText would bypass the contract.
    //
    // ponytail: type-level rule, so it also catches read-only File.Exists callers.
    // ArchUnitNET's method-level `NotCallAny(MethodMembers().HaveFullName("System.IO.File.WriteAllText"))`
    // is a no-op: external framework methods appear only as MethodCallDependency targets,
    // never in MethodMembers (which is restricted to methods defined in loaded assemblies).
    // Type-level is the coarsest check that actually has teeth. LaunchArgumentBuilder
    // (wt.exe path resolution), LauncherStore (store-path resolution), and
    // AgentDiscoveryScanner (PATH probe for AI agent CLIs) are exempted because their
    // File dependency is File.Exists reads, not writes.

    [Fact]
    public void R5_Only_LauncherJson_AndKnownReaders_MayDependOn_SystemIoFile_In_Core()
    {
        IArchRule rule = Types().That().Are(CoreLayer)
            .And().DoNotHaveFullName("WTLauncher.Core.LauncherJson")
            .And().DoNotHaveFullName("WTLauncher.Core.LaunchArgumentBuilder")
            .And().DoNotHaveFullName("WTLauncher.Core.LauncherStore")
            .And().DoNotHaveFullName("WTLauncher.Core.AgentDiscoveryScanner")
            .Should().NotDependOnAnyTypesThat().HaveFullName("System.IO.File")
            .Because("LauncherJson is the single atomic-write gateway; LaunchArgumentBuilder, LauncherStore and AgentDiscoveryScanner only do File.Exists reads.");
        rule.Check(Architecture);
    }

    // ── R6: Core namespace should not depend on App's ViewModels ────────
    //
    // Belt-and-suspenders to R3: catches accidental Core→ViewModels coupling
    // even if a future refactor introduces a new App sub-namespace.

    [Fact]
    public void R6_Core_MustNotReference_AppViewModels()
    {
        IArchRule rule = Types().That().Are(CoreLayer)
            .Should().NotDependOnAnyTypesThat().ResideInNamespace("WTLauncher.App.ViewModels")
            .Because("Core is Layer 0; ViewModels are Layer 1-3.");
        rule.Check(Architecture);
    }

    [Fact]
    public void R7_PresetServices_MustNotReference_Views()
    {
        IArchRule rule = Types().That().ResideInNamespace("WTLauncher.App.Features.Presets")
            .Should().NotDependOnAnyTypesThat().ResideInNamespace("WTLauncher.App.Views")
            .Because("preset services own workflows and contracts, while Views own pixels and window lifecycle only.");
        rule.Check(Architecture);
    }

    [Fact]
    public void R8_EntryServices_MustNotReference_Views()
    {
        IArchRule rule = Types().That().ResideInNamespace("WTLauncher.App.Features.Entries")
            .Should().NotDependOnAnyTypesThat().ResideInNamespace("WTLauncher.App.Views")
            .Because("entry services own workflows and contracts, while Views own pixels and window lifecycle only.");
        rule.Check(Architecture);
    }

    [Fact]
    public void R9_PresetServices_MustNot_DependOn_SystemIoFile()
    {
        IArchRule rule = Types().That().ResideInNamespace("WTLauncher.App.Features.Presets")
            .Should().NotDependOnAnyTypesThat().HaveFullName("System.IO.File")
            .Because("preset services must use the single LauncherStore persistence boundary rather than direct file access.");
        rule.Check(Architecture);
    }

    [Fact]
    public void R10_EntryServices_MustNot_DependOn_SystemIoFile()
    {
        IArchRule rule = Types().That().ResideInNamespace("WTLauncher.App.Features.Entries")
            .Should().NotDependOnAnyTypesThat().HaveFullName("System.IO.File")
            .Because("entry services must use the single LauncherStore persistence boundary rather than direct file access.");
        rule.Check(Architecture);
    }

    // ── R11: Infrastructure must not depend on ViewModels ──────────────
    //
    // Infrastructure (AppStore, ErrorLog, ExceptionBoundary) is the
    // persistence/error-handling foundation. Referencing ViewModel types
    // from Infrastructure creates a cycle that breaks layer architecture.
    [Fact]
    public void R11_Infrastructure_MustNotDependOn_ViewModels()
    {
        IArchRule rule = Types().That().ResideInNamespace("WTLauncher.App.Infrastructure")
            .Should().NotDependOnAnyTypesThat().ResideInNamespace("WTLauncher.App.ViewModels")
            .Because("Infrastructure is the persistence/error-handling foundation below ViewModels.");
        rule.Check(Architecture);
    }

    // ── R12: Features.Dialogs must not reference Views ────────────────
    //
    // DialogService in Features.Dialogs directly creates Window instances
    // (ConfirmLaunchWindow, EntryDetailsWindow, etc.). Moving View creation
    // to the View layer keeps Features layer-pure.
    [Fact]
    public void R12_FeaturesDialogs_MustNotReference_Views()
    {
        IArchRule rule = Types().That().ResideInNamespace("WTLauncher.App.Features.Dialogs")
            .Should().NotDependOnAnyTypesThat().ResideInNamespace("WTLauncher.App.Views")
            .Because("Dialog interface should be resolved through injection, not by directly constructing View types.");
        rule.Check(Architecture);
    }
}
