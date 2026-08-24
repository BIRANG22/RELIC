# Debug Shortcut Panel Implementation Plan

1. Add source tests under `Assets/Tests/EditMode~/` for the shortcut panel bootstrap, toggle key, quest reset key, runtime data debug key, and shortcut list content.
2. Implement `DebugShortcutHelpPanel` as a runtime-created persistent UI with a backquote toggle and Ctrl + Backspace quest reset.
3. Wire the panel from `Bootstrap`.
4. Change `RuntimeDataDebugKey` default key to F12.
5. Verify source checks, build compilation, and existing shortcut references.
