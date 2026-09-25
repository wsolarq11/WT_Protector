using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.Loader;
using ArchUnitNET.xUnitV3;
using WTLauncher.App.ViewModels;
using WTLauncher.Core;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

using WTLauncher.Tests;
namespace WTLauncher.Tests;

/// <summary>
/// Architecture invariants enforced at test time. Translates prose rules from
/// .trellis/spec/core/quality-guidelines.md into executable checks.
///
/// Layer boundaries, namespace ownership, and dependency direction are encoded here so
/// violations fail `dotnet test` immediately, not after human review.
/// </summary>
[Trait("Category", TestCategories.Architecture)]
public sealed class ArchitectureRules
{
    private static readonly ArchUnitNET.Domain.Architecture Architecture = new ArchLoader()
        .LoadAssemblies(typeof(LauncherStore).Assembly, typeof(MainWindowViewModel).Assembly)
        .Build();

    private static readonly IObjectProvider<IType> CoreLayer =
        Types().That().ResideInNamespace("WTLauncher.Core").As("Core layer");

    private static readonly IObjectProvider<IType> AppLayer =
        Types().That().ResideInNamespace("WTLauncher.App").As("App layer");

    private static readonly IObjectProvider<IType> ViewModelLayer =
        Types().That().ResideInNamespace("WTLauncher.App.ViewModels").As("ViewModel layer");

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

    // ── R7: PresetServices must not reference Views ──────────────────────
    //
    // Source: layer-separation invariant (Features cannot see pixels).
    // Assembly-based with non-empty guard: if Presets assembly contains no types
    // (e.g. namespace was renamed), the rule fails rather than silently passing.

    [Fact]
    public void R7_PresetServices_MustNotReference_Views()
    {
        var presetTypes = Types().That().ResideInAssembly(typeof(WTLauncher.App.Features.Presets.PresetService).Assembly)
            .And().ResideInNamespace("WTLauncher.App.Features.Presets");
        Assert.NotEmpty(presetTypes.GetObjects(Architecture));

        IArchRule rule = presetTypes
            .Should().NotDependOnAnyTypesThat().ResideInNamespace("WTLauncher.App.Views")
            .Because("preset services own workflows and contracts, while Views own pixels and window lifecycle only.");
        rule.Check(Architecture);
    }

    // ── R8: EntryServices must not reference Views ───────────────────────

    [Fact]
    public void R8_EntryServices_MustNotReference_Views()
    {
        var entryTypes = Types().That().ResideInAssembly(typeof(WTLauncher.App.Features.Entries.EntryService).Assembly)
            .And().ResideInNamespace("WTLauncher.App.Features.Entries");
        Assert.NotEmpty(entryTypes.GetObjects(Architecture));

        IArchRule rule = entryTypes
            .Should().NotDependOnAnyTypesThat().ResideInNamespace("WTLauncher.App.Views")
            .Because("entry services own workflows and contracts, while Views own pixels and window lifecycle only.");
        rule.Check(Architecture);
    }

    // ── R9: PresetServices must not depend on System.IO.File ─────────────

    [Fact]
    public void R9_PresetServices_MustNot_DependOn_SystemIoFile()
    {
        var presetTypes = Types().That().ResideInAssembly(typeof(WTLauncher.App.Features.Presets.PresetService).Assembly)
            .And().ResideInNamespace("WTLauncher.App.Features.Presets");
        Assert.NotEmpty(presetTypes.GetObjects(Architecture));

        IArchRule rule = presetTypes
            .Should().NotDependOnAnyTypesThat().HaveFullName("System.IO.File")
            .Because("preset services must use the single LauncherStore persistence boundary rather than direct file access.");
        rule.Check(Architecture);
    }

    // ── R10: EntryServices must not depend on System.IO.File ────────────

    [Fact]
    public void R10_EntryServices_MustNot_DependOn_SystemIoFile()
    {
        var entryTypes = Types().That().ResideInAssembly(typeof(WTLauncher.App.Features.Entries.EntryService).Assembly)
            .And().ResideInNamespace("WTLauncher.App.Features.Entries");
        Assert.NotEmpty(entryTypes.GetObjects(Architecture));

        IArchRule rule = entryTypes
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

    // ── R13: Core must not reference System.IO.File ───────────────────
    //
    // Path/Environment are acceptable in Core; File.WriteAllText etc. are not.
    // LaunchArgumentBuilder (wt.exe path probe) and AgentDiscoveryScanner
    // (PATH probe for AI CLI) are exempted for read-only File.Exists calls.

    [Fact]
    public void R13_Core_MustNotReference_SystemIoFile()
    {
        IArchRule rule = Types().That().Are(CoreLayer)
            .And().DoNotHaveFullName("WTLauncher.Core.LaunchArgumentBuilder")
            .And().DoNotHaveFullName("WTLauncher.Core.AgentDiscoveryScanner")
            .Should().NotDependOnAnyTypesThat().HaveFullName("System.IO.File")
            .Because("Core (Zone 0) must not directly read/write files; LaunchArgumentBuilder and AgentDiscoveryScanner are exempted for File.Exists probes.");
        rule.Check(Architecture);
    }

    // ── R14: StoreGrain must not call LauncherStore directly ──────────
    //
    // CFA Step 2: StoreGrain uses IStorePersistence injected boundary.

    [Fact]
    public void R14_StoreGrain_MustNotCall_LauncherStore()
    {
        IArchRule rule = Types().That().HaveFullName("WTLauncher.App.Infrastructure.StoreGrain")
            .Should().NotDependOnAnyTypesThat().HaveFullName("WTLauncher.Core.LauncherStore")
            .Because("StoreGrain uses IStorePersistence (injected) instead of LauncherStore statics (CFA Step 2).");
        rule.Check(Architecture);
    }
}
