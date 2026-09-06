# DebugBattle GridEffectTooltipUI Warning Design

## Problem

DebugBattle scene logs a warning when hovering grid effect targets because `GridEffectHoverTarget` calls `GridEffectTooltipUI.GetOrCreate()`, and that method only searches for an existing scene object.

`Battle.unity` already has a `GridEffectTooltipUI` object under the main `BattleHUDCanvas`, but `DebugBattle.unity` does not.

## Design

- Keep `GridEffectTooltipUI` scene-placed, matching the normal Battle scene pattern.
- Do not add runtime creation.
- Add the same tooltip UI object group to DebugBattle's main `BattleHUDCanvas`.
- Preserve the normal Battle scene object layout:
  - `GridEffectTooltipUI`
  - `Background`
  - `NameText`
  - `ToolTipText`
- Add an EditMode scene asset test so the DebugBattle scene keeps the required tooltip UI reference.

## Scope

- Modify only DebugBattle scene, a DebugBattle scene asset test, and this task documentation.
- No battle core state or result logic changes.
