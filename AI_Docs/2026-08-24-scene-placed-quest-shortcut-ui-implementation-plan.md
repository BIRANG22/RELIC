# Scene-Placed Quest And Shortcut UI Implementation Plan

1. Update source tests so Bootstrap/runtime code must not create quest or shortcut UI.
2. Remove runtime creation methods from quest and shortcut panel scripts.
3. Update `LobbyQuestGate` to use the scene-placed `LobbyQuestManager.Instance`.
4. Add scene-placed manager, canvas, panel, and text objects to `Bootstrap.unity`.
5. Verify source checks, compile with MSBuild, and check workspace diff.
