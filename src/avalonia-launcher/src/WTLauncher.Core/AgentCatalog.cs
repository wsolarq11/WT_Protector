namespace WTLauncher.Core;

/// <summary>
/// Static catalog of AI agent CLIs known to the launcher. Adding a new
/// agent is a one-line edit here — no plugin machinery, no JSON file to
/// maintain. Agents not in this catalog can still be added manually via
/// the existing add-entry dialog; the catalog only drives auto-discovery.
/// </summary>
/// <remarks>
/// Brand colors (<see cref="AgentCatalogEntry.BrandColorHex"/>) are approximate
/// visual signals chosen for pairwise distinctness and white-text contrast —
/// they are NOT vendor-official assets. Users who want exact brand fidelity
/// can edit the hexes locally.
/// </remarks>
public static class AgentCatalog
{
    /// <summary>All agents the scanner probes for. Order = display order.</summary>
    public static readonly IReadOnlyList<AgentCatalogEntry> Entries = new[]
    {
        new AgentCatalogEntry
        {
            ExecName = "claude",
            DisplayName = "Claude Code",
            Vendor = "Anthropic",
            VersionArgs = "--version",
            Category = AgentCategory.TerminalNative,
            HomePageUrl = "https://claude.ai/code",
            BrandColorHex = "#D97757",
        },
        new AgentCatalogEntry
        {
            ExecName = "codex",
            DisplayName = "Codex CLI",
            Vendor = "OpenAI",
            VersionArgs = "--version",
            Category = AgentCategory.TerminalNative,
            HomePageUrl = "https://github.com/openai/codex",
            BrandColorHex = "#10A37F",
        },
        new AgentCatalogEntry
        {
            ExecName = "cursor",
            DisplayName = "Cursor CLI",
            Vendor = "Cursor",
            VersionArgs = "--version",
            Category = AgentCategory.TerminalNative,
            HomePageUrl = "https://cursor.com",
            BrandColorHex = "#5B5BD6",
        },
        new AgentCatalogEntry
        {
            ExecName = "antigravity",
            DisplayName = "Antigravity CLI",
            Vendor = "Google",
            VersionArgs = "--version",
            Category = AgentCategory.TerminalNative,
            HomePageUrl = "https://antigravity.google",
            BrandColorHex = "#1A73E8",
        },
        new AgentCatalogEntry
        {
            // Retired by Google on 2026-05-19, replaced by Antigravity CLI.
            // Kept in catalog because users may still have it installed.
            ExecName = "gemini",
            DisplayName = "Gemini CLI (retired)",
            Vendor = "Google",
            VersionArgs = "--version",
            Category = AgentCategory.TerminalNative,
            HomePageUrl = "https://github.com/google-gemini/gemini-cli",
            BrandColorHex = "#8B5CF6",
        },
        new AgentCatalogEntry
        {
            ExecName = "opencode",
            DisplayName = "opencode",
            Vendor = "community",
            VersionArgs = "--version",
            Category = AgentCategory.TerminalNative,
            HomePageUrl = "https://github.com/sst/opencode",
            BrandColorHex = "#EA580C",
        },
        new AgentCatalogEntry
        {
            ExecName = "aider",
            DisplayName = "Aider",
            Vendor = "community",
            VersionArgs = "--version",
            Category = AgentCategory.TerminalNative,
            HomePageUrl = "https://aider.chat",
            BrandColorHex = "#B91C1C",
        },
        new AgentCatalogEntry
        {
            ExecName = "qwen-code",
            DisplayName = "Qwen Code",
            Vendor = "Alibaba",
            VersionArgs = "--version",
            Category = AgentCategory.TerminalNative,
            HomePageUrl = "https://github.com/QwenLM/qwen-code",
            BrandColorHex = "#6D28D9",
        },
    };
}
