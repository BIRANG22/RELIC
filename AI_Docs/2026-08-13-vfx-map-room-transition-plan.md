# VFX Proxy And Map Room Transition Plan

## Context

- Event map visual actions currently instantiate `VfxPrefab` directly under `MapVisualActor.vfxRoot`.
- Direct child VFX can be created successfully but may not be visible because it bypasses the existing `BattleWorldVfxRenderer` world proxy render path.
- Room merging left two related presentation ownership problems:
  - Battle-room `BattleCharacter` instances can remain alive while the map panel is opened.
  - Battle-room HUD lookups can accidentally bind to a non-battle `BattleCharacterPanelUI` when scene hierarchy or serialized references drift.

## Recommended Design

1. Route map visual action VFX through `BattleWorldVfxRenderer.TrySpawnDetached` first.
   - Use `VfxLocalPosition` to calculate the world proxy position from the action VFX root.
   - Keep the render-space VFX instance at local origin to avoid double offset.
   - Preserve direct instantiate as fallback when the proxy cannot be configured.

2. Treat map-panel party characters as map-room presentation, not battle units.
   - Stop auto-discovering `BattleCharacter` instances as map-selection characters.
   - Keep `BattleMapSelectionCharacterMarker` support for event-world prefabs.
   - Clear battle units immediately when preparing to return to map.

3. Rebind battle HUD panel from the battle room first.
   - `BattleRoomLoader` should prefer a `BattleCharacterPanelUI` under its own room hierarchy.
   - Ensure the selected panel is parented under that room's `BattleHUDCanvas`.

4. Gate shared map party presentation by room state.
   - Keep `SharedRoomRoot` active for shared background/VFX ownership.
   - Turn off `SharedRoomRoot/AllyRoot` while `BattleRoom` is active to prevent map-party characters from overlapping battle-spawned characters.
   - Turn `AllyRoot` back on when the map panel is opened, then refresh `MapRoomController`.

5. Refresh the selected character panel position after binding.
   - Re-evaluate the current timeline selection on the next frame after `BattleCharacterPanelUI.Bind`.
   - This covers cases where the same character was already selected and `BattleTimelineController.SelectCharacter` does not emit a new selection-changed event.

6. Gate boss-room battle loading behind the boss reveal sequence.
   - Treat `TimedObjectRevealSequence` as an `IBattleRoomIntroSequence`.
   - For boss nodes, resolve the shared background sequence and delay `BattleRoomLoader` until that sequence completes.
   - Suppress battle HUD/menu roots while the boss reveal is pending so `BattleCharacterPanel` and `BattleSlot` do not appear early.

## Verification

- Add EditMode tests for VFX proxy spawning, map-selection character filtering, room cleanup, battle HUD panel rebinding, shared `AllyRoot` visibility, selection panel position refresh, and boss reveal load gating.
- Run focused EditMode tests through the available project test command if possible.
- Run MSBuild for runtime/editor assemblies.
- Run `git diff --check`.

## Multiplayer Boundary

This change is presentation-only. It does not alter battle commands, state mutation, result calculation, random decisions, or runtime IDs.
