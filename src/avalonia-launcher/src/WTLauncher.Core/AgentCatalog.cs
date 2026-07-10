namespace WTLauncher.Core;

/// <summary>
/// Static catalog of AI agent CLIs known to the launcher. Adding a new
/// agent is a one-line edit here — no plugin machinery, no JSON file to
/// maintain. Agents not in this catalog can still be added manually via
/// the existing add-entry dialog; the catalog only drives auto-discovery.
/// </summary>
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
        },
        new AgentCatalogEntry
        {
            ExecName = "codex",
            DisplayName = "Codex CLI",
            Vendor = "OpenAI",
            VersionArgs = "--version",
            Category = AgentCategory.TerminalNative,
            HomePageUrl = "https://github.com/openai/codex",
        },
        new AgentCatalogEntry
        {
            ExecName = "cursor",
            DisplayName = "Cursor CLI",
            Vendor = "Cursor",
            VersionArgs = "--version",
            Category = AgentCategory.TerminalNative,
            HomePageUrl = "https://cursor.com",
        },
        new AgentCatalogEntry
        {
            ExecName = "antigravity",
            DisplayName = "Antigravity CLI",
            Vendor = "Google",
            VersionArgs = "--version",
            Category = AgentCategory.TerminalNative,
            HomePageUrl = "https://antigravity.google",
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
        },
        new AgentCatalogEntry
        {
            ExecName = "opencode",
            DisplayName = "opencode",
            Vendor = "community",
            VersionArgs = "--version",
            Category = AgentCategory.TerminalNative,
            HomePageUrl = "https://github.com/sst/opencode",
        },
        new AgentCatalogEntry
        {
            ExecName = "aider",
            DisplayName = "Aider",
            Vendor = "community",
            VersionArgs = "--version",
            Category = AgentCategory.TerminalNative,
            HomePageUrl = "https://aider.chat",
        },
        new AgentCatalogEntry
        {
            ExecName = "qwen-code",
            DisplayName = "Qwen Code",
            Vendor = "Alibaba",
            VersionArgs = "--version",
            Category = AgentCategory.TerminalNative,
            HomePageUrl = "https://github.com/QwenLM/qwen-code",
        },
    };
}
