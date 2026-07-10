# Implement — AI Agent CLI Discovery Feature

Parent: `07-10-v2-agent-discovery-and-hygiene`
PRD: `prd.md` · Design: `design.md`

## Execution Order

Phases must land in order; each phase ends with a runnable check. If any phase fails, stop and fix before proceeding.

### Phase 1 — Core types & catalog (no Avalonia, no I/O)

**Files**:
- `src/WTLauncher.Core/AgentCategory.cs` — enum
- `src/WTLauncher.Core/AgentCatalogEntry.cs` — record
- `src/WTLauncher.Core/DiscoveredAgent.cs` — record
- `src/WTLauncher.Core/AgentDiscoveryResult.cs` — record + `AgentDiscoveryFailure`
- `src/WTLauncher.Core/AgentCatalog.cs` — static list with ≥8 agents

**Catalog content (initial)**:

| ExecName | DisplayName | Vendor | VersionArgs | RecommendedArgs | Category |
|---|---|---|---|---|---|
| `claude` | Claude Code | Anthropic | `--version` | (empty) | TerminalNative |
| `codex` | Codex CLI | OpenAI | `--version` | (empty) | TerminalNative |
| `cursor` | Cursor CLI | Cursor | `--version` | (empty) | TerminalNative |
| `antigravity` | Antigravity CLI | Google | `--version` | (empty) | TerminalNative |
| `gemini` | Gemini CLI (retired) | Google | `--version` | (empty) | TerminalNative |
| `opencode` | opencode | community | `--version` | (empty) | TerminalNative |
| `aider` | Aider | community | `--version` | (empty) | TerminalNative |
| `qwen-code` | Qwen Code | Alibaba | `--version` | (empty) | TerminalNative |

Leave `RecommendedArgs` empty in v2.1 — user sets per-entry. (Avoids "what's the right default for claude" debate.)

**Check**:
```bash
dotnet build src/avalonia-launcher/WTLauncher.Avalonia.sln --no-restore -warnaserror
```

### Phase 2 — Scanner (Core, pure function)

**File**: `src/WTLauncher.Core/AgentDiscoveryScanner.cs`

**Public API**:
```csharp
public static class AgentDiscoveryScanner
{
    /// <summary>Scans PATH for catalog agents. Continues on per-agent failure.</summary>
    public static AgentDiscoveryResult Scan(
        IEnumerable<AgentCatalogEntry> catalog,
        string? pathOverride = null,
        CancellationToken cancellationToken = default);
}
```

**Implementation notes**:
- `pathOverride ??= Environment.GetEnvironmentVariable("PATH")`
- Split PATH on `;` (Windows). For each catalog entry, try `${dir}/${ExecName}`, `${dir}/${ExecName}.exe`, `${dir}/${ExecName}.cmd`
- First hit wins; `File.Exists` check (allowed in Core per ArchUnitNET R5 exemption for `LaunchArgumentBuilder` / `LauncherStore` patterns — but scanner does **not** depend on `System.IO.File` statically, uses `File.Exists` per call. Re-check: scanner needs to be exempted or use a probe interface. **Resolution**: add scanner to R5 exemption list with comment — same pattern as LaunchArgumentBuilder which only does File.Exists reads.)
- For version: `Process.Start(new ProcessStartInfo { FileName = resolvedPath, Arguments = versionArgs, RedirectStandardOutput = true, UseShellExecute = false })`. Read stdout, take first line, regex `\d+\.\d+(?:\.\d+)?`. Timeout 2s via `CancellationToken`.
- Wrap each agent's scan in try/catch (narrow: `Win32Exception`, `InvalidOperationException`, `SystemException` for timeout). Append to `Failures` on error, do not throw.

**Tests**: `tests/WTLauncher.Tests/AgentDiscoveryScannerTests.cs` — `[Theory]` × 12-15 covering:
- Empty PATH
- Single agent found at first dir
- Agent not on PATH
- `.exe` vs `.cmd` resolution
- Version parsing: clean semver, `v1.2.3`, multiline, no version string, timeout

**Check**:
```bash
dotnet test src/avalonia-launcher/WTLauncher.Avalonia.sln -c Release --filter "FullyQualifiedName~AgentDiscoveryScanner"
dotnet build src/avalonia-launcher/WTLauncher.Avalonia.sln --no-restore -warnaserror
```

### Phase 3 — Recommendation factory (Core, pure)

**File**: `src/WTLauncher.Core/AgentRecommendationFactory.cs`

**Public API**:
```csharp
public static class AgentRecommendationFactory
{
    /// <summary>Builds a LaunchEntryDraft from a discovered agent.</summary>
    public static LaunchEntryDraft ToDraft(DiscoveredAgent agent, string defaultDirectory);
}
```

**Behavior**:
- `Name` = `agent.Catalog.DisplayName`
- `Directory` = `defaultDirectory` (caller passes `%USERPROFILE%`)
- `Command` = `agent.Catalog.ExecName` (raw; user can swap for `$key` template later if they customize args)
- `Selected` = false

**Tests**: extend `LauncherEntryDraftsTests.cs` or add `AgentRecommendationFactoryTests.cs` × 4-5.

**Check**:
```bash
dotnet test src/avalonia-launcher/WTLauncher.Avalonia.sln -c Release --filter "FullyQualifiedName~AgentRecommendationFactory"
```

### Phase 4 — Discovery dialog ViewModel

**File**: `src/WTLauncher.App/ViewModels/DiscoverAgentsViewModel.cs`

**Public surface**:
- `[ObservableProperty] bool IsScanning`
- `[ObservableProperty] IReadOnlyList<DiscoveredAgentRow> Rows` (one per found agent + greyed rows for already-added)
- `[ObservableProperty] string StatusText`
- `[RelayCommand] async Task ScanAsync()`
- `[RelayCommand] void ToggleSelect(DiscoveredAgentRow row)`
- `[RelayCommand] IReadOnlyList<DiscoveredAgent> GetSelected()`

`DiscoveredAgentRow` is a VM-side view-model wrapper holding `DiscoveredAgent` + `IsSelected` + `IsAlreadyAdded`.

**Tests**: `DiscoverAgentsViewModelTests.cs` × 5-7.

**Check**:
```bash
dotnet test src/avalonia-launcher/WTLauncher.Avalonia.sln -c Release --filter "FullyQualifiedName~DiscoverAgentsViewModel"
```

### Phase 5 — Dialog XAML + code-behind

**Files**:
- `src/WTLauncher.App/Views/DiscoverAgentsWindow.axaml`
- `src/WTLauncher.App/Views/DiscoverAgentsWindow.axaml.cs`

**XAML structure**:
- Window with title "🔍 发现已安装的 AI Agent"
- Header: title + status text + "重新扫描" button (top-right)
- Body: `ItemsControl` of `DiscoveredAgentCard` user-style (use `DataTemplate` inline)
- Each card: checkbox + DisplayName + Vendor badge + Version + truncated ResolvedPath + ℹ️ button (tooltip with HomePageUrl)
- Already-added: card greyed (`Opacity="0.5"`), checkbox disabled, "已添加" label
- Footer: "添加选中 (N)" primary button + "取消" secondary button
- All visuals via `{StaticResource …}` — no hardcoded colors/sizes

**Code-behind**:
- ctor takes `DiscoverAgentsViewModel`
- Window lifetime management matches `ConfirmBatchLaunchWindow.cs` pattern
- "ℹ️" button opens homepage in default browser via `Process.Start("https://...")` (use `UseShellExecute=true`)
- No business logic in code-behind

**Check** (manual):
```bash
dotnet build src/avalonia-launcher/WTLauncher.Avalonia.sln --no-restore -warnaserror
```

### Phase 6 — Wire into MainWindowViewModel

**Files**:
- `src/WTLauncher.App/ViewModels/MainWindowViewModel.Discovery.cs` (new partial)
- `src/WTLauncher.App/ViewModels/MainWindowViewModel.cs` (touch ctor to add `Func<DiscoverAgentsViewModel?>` injection)
- `src/WTLauncher.App/Views/MainWindow.axaml` (add "🔍 发现" button)
- `src/WTLauncher.App/Views/MainWindow.axaml.cs` (wire Func injection)

**MainWindowViewModel.Discovery.cs content**:
```csharp
public partial class MainWindowViewModel
{
    private readonly Func<DiscoverAgentsViewModel?> _discoverAgentsDialogFactory;

    // Injected through MainWindowViewModel ctor alongside existing Func<> delegates
    // ...

    [RelayCommand]
    private async Task DiscoverAgentsAsync()
    {
        var vm = _discoverAgentsDialogFactory();
        if (vm is null) return;

        await vm.ScanAsync();
        // Open dialog (injected via MainWindow callback) ...
        var selected = await _showDiscoverAgentsDialogAsync(vm);
        if (selected.Count == 0) return;

        // For each selected, build draft via AgentRecommendationFactory.ToDraft
        // Reuse existing AddEntryFromDraft pipeline (snapshot + atomic save + rollback)
        int added = 0;
        foreach (var discovered in selected)
        {
            var draft = AgentRecommendationFactory.ToDraft(discovered, Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
            // Call existing AddEntry command logic — reuse snapshot pattern
            // ...
            added++;
        }
        StatusText = $"已添加 {added} 个 AI Agent 启动项";
    }
}
```

**MainWindow.axaml**: add next to "添加" button:
```xml
<Button Content="🔍 发现"
        Command="{Binding DiscoverAgentsCommand}"
        Classes="secondary"
        ToolTip.Tip="扫描本机已安装的 AI Agent CLI 并添加为启动项"
        Margin="0,0,8,0" />
```

**Check**:
```bash
dotnet build src/avalonia-launcher/WTLauncher.Avalonia.sln --no-restore -warnaserror
dotnet test src/avalonia-launcher/WTLauncher.Avalonia.sln -c Release
```

### Phase 7 — End-to-end tests

**File**: `tests/WTLauncher.Tests/MainWindowViewModelTests.DiscoveryTests.cs` × 4-6:
- Discovery with 0 agents found → dialog closes gracefully, no mutation
- Discovery with 3 agents found, 2 selected → 2 entries added via atomic save
- Discovery + simulated save failure → rollback, status shows error, no partial writes
- Discovery dedupes against existing entries by ExecName substring in `command`

**Check**:
```bash
dotnet test src/avalonia-launcher/WTLauncher.Avalonia.sln -c Release --filter "FullyQualifiedName~Discovery"
```

### Phase 8 — Documentation & catalog integrity test

**Files**:
- `docs/feature-reality-north-star-audit.md`: add feature #24 with star rating ⭐⭐⭐⭐⭐
- `AGENTS.md`: mention discovery feature in repo map "Scripts/Features" section
- `tests/WTLauncher.Tests/AgentCatalogTests.cs`: × 4-6 (no dupes, all required fields, Vendor matches catalog)

**Check**:
```bash
# Catalog integrity
dotnet test src/avalonia-launcher/WTLauncher.Avalonia.sln -c Release --filter "FullyQualifiedName~AgentCatalog"

# Docs gate
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\verify.ps1
```

## Review Gates

After each phase, before proceeding:
1. `dotnet build` zero warnings (TreatWarningsAsErrors=true)
2. Phase's new tests pass
3. No bare `catch (Exception)` (use narrow types)
4. No hardcoded color/size in XAML
5. No new NuGet package reference

## Rollback Points

- After Phase 1-3 (Core only): can revert by deleting 5 Core files; no UI impact.
- After Phase 4-5 (Dialog): can revert by deleting 2 dialog files + reverting ctor touch.
- After Phase 6-7 (Wired in): revert by removing "🔍 发现" button + the partial.
- After Phase 8 (Docs): revert docs alone (no code impact).

Each phase produces a valid, runnable binary.

## Validation Commands (final gate)

```bash
# 1. Clean restore + build
dotnet restore src/avalonia-launcher/WTLauncher.Avalonia.sln
dotnet build src/avalonia-launcher/WTLauncher.Avalonia.sln --no-restore -warnaserror

# 2. Full test suite (existing + ~25-34 new)
dotnet test src/avalonia-launcher/WTLauncher.Avalonia.sln -c Release

# 3. Repository gate (18 checks)
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\verify.ps1

# 4. Diff sanity
git diff --check
git status
```

All four must pass before `task.py finish`.
