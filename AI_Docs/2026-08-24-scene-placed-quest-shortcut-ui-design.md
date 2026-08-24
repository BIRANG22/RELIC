# Scene-Placed Quest And Shortcut UI Design

## Goal

Move the lobby quest manager/panel and debug shortcut help panel away from runtime GameObject creation. Both systems should be placed in `Assets/Project/Scenes/YDM/Bootstrap.unity` and referenced through serialized scene fields.

## Design

- `Bootstrap` no longer calls `LobbyQuestManager.EnsureInstance()` or `DebugShortcutHelpPanel.EnsureInstance()`.
- `LobbyQuestManager` remains a singleton only for lookup, but it does not create itself or its UI.
- `LobbyQuestPanel` no longer creates its own default UI. It only applies quest state to a scene-assigned text component.
- `DebugShortcutHelpPanel` no longer creates Canvas, panel, or text GameObjects. It toggles a scene-assigned panel root and writes shortcut text to a scene-assigned TMP text.
- `Bootstrap.unity` owns the placed objects:
  - `LobbyQuestManager`
  - `LobbyQuestCanvas`
  - `LobbyQuestPanel`
  - `LobbyQuestText`
  - `DebugShortcutHelpPanel`
  - `DebugShortcutHelpCanvas`
  - `DebugShortcutHelpContent`

## Behavior

- Quest display and gate logic stay the same.
- `Ctrl + Backspace` still resets only lobby quest/tutorial progress.
- Backquote still toggles the shortcut help panel.
- Missing scene references log warnings instead of creating fallback objects.
