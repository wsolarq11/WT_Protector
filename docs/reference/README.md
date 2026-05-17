# Shortcut backup manifest

`WT_Shortcuts_Backup/` was a binary `.lnk` backup directory. It has been converted into `shortcut-backup-manifest.json` so the useful launch-target knowledge remains reviewable and diffable without keeping the backup tree alive.

Rules:

- Active launcher entries belong in `config/config.json`.
- Retired HTA material belongs in `retired/`.
- Do not recreate `WT_Shortcuts_Backup/`; add future shortcut-derived knowledge to this reference manifest or to `config/config.json` directly.
