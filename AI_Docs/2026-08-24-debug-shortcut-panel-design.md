# Debug Shortcut Panel Design

## Goal

Add a global keyboard shortcut panel that lets developers check test and convenience shortcuts in one place. The panel opens and closes with the backquote key. Quest progress reset is added as a keyboard shortcut instead of an in-panel button.

## Design

- Add `DebugShortcutHelpPanel` as a runtime-created, persistent MonoBehaviour.
- Create it from `Bootstrap` after save/load initialization so it is available across scenes.
- Use `KeyCode.BackQuote` to toggle the help panel.
- Use `Ctrl + Backspace` to reset only lobby quest/tutorial progress.
- Move `RuntimeDataDebugKey` from `BackQuote` to `F12` to avoid conflicting with the help panel toggle.
- Show shortcut entries grouped by category:
  - Global debug
  - Lobby
  - Battle
  - VFX/debug workbench
- Reset quest progress by setting `LobbyRuntimeData.TutorialProgress` to `NotStarted`, saving current progress, and refreshing `LobbyQuestManager`.

## Non-Goals

- Do not reset all save data, inventory, rewards, character state, or combat state.
- Do not change combat logic.
- Do not convert every existing shortcut implementation to a central dispatcher in this pass.
