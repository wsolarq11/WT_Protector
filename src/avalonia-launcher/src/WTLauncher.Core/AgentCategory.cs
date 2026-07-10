namespace WTLauncher.Core;

/// <summary>
/// Coarse classification for catalog agents. Drives filtering and (future)
/// theming decisions. v2.1 ships only TerminalNative entries; the other
/// variants are reserved for future expansion (IDE-extensions like Cline,
/// desktop apps like Windsurf).
/// </summary>
public enum AgentCategory
{
    /// <summary>Terminal-native CLI agent (claude, codex, opencode, ...).</summary>
    TerminalNative,

    /// <summary>VS Code / JetBrains extension with a CLI surface (cline, kilo).</summary>
    IDEExtension,

    /// <summary>Desktop application with optional CLI (windsurf, cursor-app).</summary>
    DesktopApp,
}
