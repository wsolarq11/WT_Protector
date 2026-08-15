# Post-rewrite gate-protected content quick check (run BEFORE full tools/verify.ps1)
# ASCII-only: PS 5.1 reads BOM-less scripts as ANSI; non-ASCII chars corrupt parsing.
$ErrorActionPreference = 'Stop'
$root = (Get-Location).Path
$fail = 0
function Assert-Contains([string]$path, [string]$needle, [string]$what) {
    $text = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $root $path)
    if (-not $text.Contains($needle)) { Write-Host "FAIL $what in $path : missing [$needle]"; $script:fail++ }
    else { Write-Host "ok   $what in $path" }
}

# README: 6 sections + 4 boundary phrases
foreach ($s in @('Configuration schema','Build and verification','Run the mainline app','WinForms retirement','Safety notes','Current implementation status')) { Assert-Contains 'README.md' $s "README section [$s]" }
foreach ($p in @('all 22 active feature mappings as narrow-domain north-star','portable Windows Terminal workspace-launcher scope only','full absolute objective remains not complete as written','unimprovable-product, stronger-than-every-GitHub-similar, and absolute zero-side-effect claims')) { Assert-Contains 'README.md' $p "README boundary phrase" }

# Audit structure (regex backslashes must be literal in this file)
$fa = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $root 'docs/feature-reality-north-star-audit.md')
$h = [regex]::Matches($fa, '(?m)^### \d+\. ').Count
$v = [regex]::Matches($fa, '(?m)^- Verdict level: narrow-north-star$').Count
if ($h -ne 22) { Write-Host "FAIL audit headings: $h (need 22)"; $fail++ } else { Write-Host "ok   audit headings: $h" }
if ($v -ne 22) { Write-Host "FAIL audit verdicts: $v (need 22)"; $fail++ } else { Write-Host "ok   audit verdicts: $v" }
$led = $fa.IndexOf('## Claim boundary ledger')
$fa2 = $fa.IndexOf('## Final answer')
if ($led -lt 0 -or $fa2 -le $led) { Write-Host "FAIL audit ledger before final answer"; $fail++ } else { Write-Host "ok   audit ledger order" }
$ledText = $fa.Substring($led, $fa2 - $led)
$allowed = [regex]::Matches($ledText, '(?m)^\s+- Claim status: allowed$').Count
$forbidden = [regex]::Matches($ledText, '(?m)^\s+- Claim status: forbidden$').Count
if ($allowed -ne 4) { Write-Host "FAIL audit allowed=$allowed (need 4)"; $fail++ } else { Write-Host "ok   audit allowed: 4" }
if ($forbidden -ne 5) { Write-Host "FAIL audit forbidden=$forbidden (need 5)"; $fail++ } else { Write-Host "ok   audit forbidden: 5" }
Assert-Contains 'docs/feature-reality-north-star-audit.md' 'Full objective result: not-complete-as-written' 'audit full-objective phrase'

# winforms: 4 sections
foreach ($s in @('Retired WinForms launcher design','Portable config discovery','Launch safety boundary','Successor directive')) { Assert-Contains 'retired/design/winforms-launcher.md' $s "winforms [$s]" }

# AGENTS.md structure-baseline: 10 rows
$baselineRows = @(
    '| WTLauncher.Core C# files | 53 |','| ViewModels C# files | 54 |','| MainWindowViewModel partials | 15 |',
    '| Features C# files | 16 |','| Views C# files | 24 |','| Views AXAML files | 20 |',
    '| WTLauncher.Analyzers C# files | 7 |','| Tests C# files | 121 |','| Test attributes [Fact]+[Theory] | 737 |','| Gate checks (Add-Check) | 40 |')
$agents = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $root 'AGENTS.md')
$rowsOk = $true
foreach ($r in $baselineRows) { if (-not $agents.Contains($r)) { Write-Host "FAIL AGENTS.md baseline row: $r"; $rowsOk = $false; $fail++ } }
if ($rowsOk) { Write-Host "ok   AGENTS.md baseline: all 10 rows" }

# no-stale-three-file-refs on activeDocs (exact gate rule: negative lookbehind for retired[/\])
$activeDocs = @('README.md','AGENTS.md','DESIGN.md','docs/feature-reality-north-star-audit.md','docs/feature-first-class-upgrade-audit.md','docs/giants-shoulders/launcher-first-class-patterns.md')
foreach ($d in $activeDocs) {
    $text = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $root $d)
    $stale = [regex]::Matches($text, '(?<!retired[/\\])config[/\\](?:config|presets|settings)\.json')
    if ($stale.Count -gt 0) { Write-Host "FAIL no-stale-three-file-refs: $d -> $($stale[0].Value)"; $fail++ }
}
Write-Host "---"
if ($fail -eq 0) { Write-Host "POST-REWRITE QUICK CHECK: ALL PASS" } else { Write-Host "POST-REWRITE QUICK CHECK: $fail FAILURES"; exit 1 }
