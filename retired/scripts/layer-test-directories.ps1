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

$support = @(
    'AnalyzerTestHelper.cs',
    'AssemblyInfo.cs',
    'LauncherPersistenceTestFixtures.cs',
    'MainWindowEntryCardTestFactory.cs',
    'StubDialogService.cs',
    'TempDirectory.cs',
    'TestCategories.cs',
    'TestRepositoryPaths.cs',
    'TestStartup.cs'
)

New-Item -ItemType Directory -Force -Path (Join-Path $root 'Support') | Out-Null
foreach ($folder in @('CoreUnit','Persistence','ViewModelBehavior','Analyzer','Architecture','UiContract','PropertyTest')) {
    New-Item -ItemType Directory -Force -Path (Join-Path $root $folder) | Out-Null
}

foreach ($name in $support) {
    $source = Join-Path $root $name
    if (Test-Path -LiteralPath $source) {
        Move-Item -LiteralPath $source -Destination (Join-Path (Join-Path $root 'Support') $name) -Force
    }
}

foreach ($entry in $category.GetEnumerator()) {
    $source = Join-Path $root $entry.Key
    if (-not (Test-Path -LiteralPath $source)) { continue }
    $target = Join-Path (Join-Path $root $entry.Value) $entry.Key
    Move-Item -LiteralPath $source -Destination $target -Force
}

foreach ($entry in $category.GetEnumerator()) {
    $file = Join-Path (Join-Path $root $entry.Value) $entry.Key
    if (-not (Test-Path -LiteralPath $file)) { continue }
    $text = [System.IO.File]::ReadAllText($file)
    if ($text.Contains('namespace WTLauncher.Tests;')) {
        $using = if ($text.Contains('using WTLauncher.Tests;')) { '' } else { "using WTLauncher.Tests;`n" }
        $text = $text.Replace('namespace WTLauncher.Tests;', $using + "namespace WTLauncher.Tests.$($entry.Value);")
        [System.IO.File]::WriteAllText($file, $text, [System.Text.UTF8Encoding]::new($false))
    }
}

Write-Output 'Test directory layering complete.'
