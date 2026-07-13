# Codex platform Trellis setup audit

Date: 2026-07-13

## Outcome

The project uses the current stable Trellis release and now has both required Codex lifecycle entry points wired:

- `SessionStart` loads the compact Trellis session/task/spec overview.
- `UserPromptSubmit` loads the current workflow-state breadcrumb.

## Evidence

| Check | Result |
| --- | --- |
| Global package | `@mindfoldhq/trellis@0.6.6` |
| npm latest | `0.6.6` |
| Project template | `0.6.6` |
| Codex CLI | `0.144.1` |
| Codex hooks feature | `stable`, enabled in user config |
| Project trust | `d:\_ai_\_projects\wt_protector = trusted` |
| Trellis drift | `trellis update --dry-run` reports only the intentional local `.codex/hooks.json` customization |
| Sidecar conflicts | no `*.new` files |
| SessionStart script | returns JSON with `hookEventName=SessionStart` |
| Per-turn script | returns JSON with `hookEventName=UserPromptSubmit` |
| Task runtime | developer, git state, active task tree, journal and spec indexes resolve successfully |

## Intentional local customization

Trellis 0.6.6 generated `.codex/hooks/session-start.py` but did not register it in `.codex/hooks.json`. The project adds the missing `SessionStart` registration and otherwise retains the generated files unchanged.

Codex treats project-local hooks as trusted-project configuration. After this file changes, a fresh interactive Codex session may require one `/hooks` review before the new hook runs. That trust decision is intentionally left to Codex's official hook-trust UI rather than hand-editing internal trusted hashes.

## Maintenance commands

```powershell
trellis --version
trellis update --dry-run
python ./.trellis/scripts/get_context.py
'{"cwd":"D:/<PROJECT_ROOT>/WT_Protector"}' | python -X utf8 .codex/hooks/session-start.py
'{"cwd":"D:/<PROJECT_ROOT>/WT_Protector"}' | python -X utf8 .codex/hooks/inject-workflow-state.py
```

Do not use `trellis update --force`: `.codex/hooks.json` is now an intentional local customization. Review future upstream changes and merge them deliberately.
