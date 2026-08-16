param(
    [switch]$Apply
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$root = Join-Path $PSScriptRoot '..\src\avalonia-launcher\tests\WTLauncher.Tests'
$category = @{}

foreach ($name in @(
    'AgentBrandMatcherTests.cs',
    'AgentCatalogTests.cs',
    'AgentRecommendationFactoryTests.cs',
    'EntryDirectoryTests.cs',
    'ErrorLogTests.cs',
    'LaunchArgumentBuilderTests.cs',
    'LauncherBatchLaunchConfirmationTests.cs',
    'LauncherBatchLaunchPlanTextTests.cs',
    'LauncherBatchLaunchReceiptTextTests.cs',
    'LauncherBatchLaunchReportTests.cs',
    'LauncherCommandTemplateResolverTests.cs',
    'LauncherConfigExchangeTests.cs',
    'LauncherDirectoryHealthTests.cs',
    'LauncherDragGhostLayoutCalculatorTests.cs',
    'LauncherDragReorderSessionTests.cs',
    'LauncherDragReorderWorkflowTests.cs',
    'LauncherEntryDeleteTargetTests.cs',
    'LauncherEntryDraftsTests.cs',
    'LauncherEntryGrouperTests.cs',
    'LauncherEntryIdsTests.cs',
    'LauncherEntryLayoutCalculatorTests.cs',
    'LauncherEntryLayoutPresentationTests.cs',
    'LauncherEntryOrderTests.cs',
    'LauncherEntryQueryFrecencyTests.cs',
    'LauncherEntryQueryTests.cs',
    'LauncherEntryReferencesTests.cs',
    'LauncherEntryViewModeTests.cs',
    'LauncherHotkeyTests.cs',
    'LauncherPresetDeleteTargetTests.cs',
    'LauncherPresetIdsTests.cs',
    'LauncherPresetSaveTargetTests.cs',
    'LauncherPresetSelectionPreviewTests.cs',
    'LauncherPresetWorkflowTests.cs',
    'LauncherQuickMatchTests.cs',
    'LauncherRunHistoryTests.cs',
    'LauncherSearchHistoryTests.cs',
    'LauncherSingleLaunchConfirmationTests.cs',
    'LauncherSingleLaunchReceiptTextTests.cs',
    'LauncherStartupArgumentsTests.cs',
    'LauncherStatusPresentationTests.cs',
    'LauncherUpdateManifestTests.cs',
    'LaunchPlanTests.cs',
    'ServiceResultTests.cs'
)) { $category[$name] = 'CoreUnit' }

foreach ($name in @(
    'AppStoreTests.cs',
    'ConcurrencyStressTests.cs',
    'FileLauncherJsonIOTests.cs',
    'LauncherJsonContextRegistrationTests.cs',
    'LauncherStoreLoadFailureTests.cs'
)) { $category[$name] = 'Persistence' }

foreach ($name in @(
    'AgentDiscoveryScannerTests.cs',
    'CommandManagementViewModelTests.cs',
    'DiscoverAgentsViewModelDirectoryModeTests.cs',
    'DiscoverAgentsViewModelTests.cs',
    'EntryServiceTests.cs',
    'LauncherCardGroupViewModelTests.cs',
    'LauncherLaunchWorkflowTests.cs',
    'LauncherPresetViewModelTests.cs',
    'PresetServiceTests.cs',
    'UpdateCheckServiceTests.cs'
)) { $category[$name] = 'ViewModelBehavior' }

foreach ($name in @(
    'CatchExceptionAnalyzerTests.cs',
    'CommandUsageAnalyzerTests.cs',
    'DirectoryUsageAnalyzerTests.cs',
    'DispatchMustBeOnlyWritePathAnalyzerTests.cs',
    'JsonRegistrationAnalyzerTests.cs',
    'LayerViolationAnalyzerTests.cs'
)) { $category[$name] = 'Analyzer' }

foreach ($name in @('ArchitectureRules.cs', 'ServiceContractTests.cs')) { $category[$name] = 'Architecture' }

foreach ($name in @(
    'CommandManagementWindowXamlContractTests.cs',
    'DialogConsistencyContractTests.cs',
    'EntryDetailsWindowResolveAutoIdTests.cs',
    'FastWrapPanelTests.cs',
    'LauncherControlBarDeclutterContractTests.cs',
    'MainWindowDragInsertDecisionTests.cs',
    'MainWindowEntryCardContextMenuTests.cs',
    'MainWindowEntryCardResolverTests.cs',
    'MainWindowEntryRowDoubleTapTests.cs',
    'MainWindowStatusPresentationContractTests.cs',
    'MainWindowXamlModularityContractTests.cs',
    'StatusTextContractTests.cs',
    'VisualTokenContractTests.cs'
)) { $category[$name] = 'UiContract' }

foreach ($name in @(
    'CsCheckPropertyTests.cs',
    'LauncherEntryOrderPbtTests.cs',
    'LauncherJsonSerializerPbtTests.cs',
    'LauncherLayoutPbtTests.cs',
    'LauncherStoreValidatorPbtTests.cs'
)) { $category[$name] = 'PropertyTest' }

foreach ($file in Get-ChildItem -LiteralPath $root -Filter 'MainWindowViewModelTests.*.cs') {
    $category[$file.Name] = 'ViewModelBehavior'
}

$pattern = 'public\s+sealed(?:\s+partial)?\s+class\s+\w+|public\s+class\s+\w+'
$changed = 0
foreach ($file in Get-ChildItem -LiteralPath $root -Filter '*.cs') {
    $text = [System.IO.File]::ReadAllText($file.FullName)
    if (-not ($text.Contains('[Fact]') -or $text.Contains('[Theory]'))) { continue }
    if ($file.Name -notin $category.Keys) { throw "Missing category mapping for $($file.Name)" }
    if ($text.Contains('[Trait("Category"')) { continue }
    $match = [regex]::Match($text, $pattern)
    if (-not $match.Success) { throw "No top-level class found in $($file.Name)" }
    $trait = '[Trait("Category", TestCategories.' + $category[$file.Name] + ')]' + "`n"
    $text = $text.Insert($match.Index, $trait)
    [System.IO.File]::WriteAllText($file.FullName, $text, [System.Text.UTF8Encoding]::new($false))
    Write-Output "$($file.Name) -> $($category[$file.Name])"
    $changed++
}
Write-Output "Trait files changed: $changed"
