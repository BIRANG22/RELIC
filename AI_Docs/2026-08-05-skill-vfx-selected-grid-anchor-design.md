# Skill VFX Selected Grid Anchor Design

## Background

Skill-specific VFX is currently resolved by `SkillId` through `SkillVfxDatabase`, then played by `BattleUnitAnimator` from the caster unit's VFX spawn transform. This works for caster-side effects, but some Selection skills need the VFX to appear at the selected grid.

The selected grid already exists on `PlayerReservedCommand.SelectedGridIndex` and is included in the network command data, so the presentation layer can use that value without adding new battle result state.

## Design

1. Add a small spawn anchor option to `BattleVfxEntry`.
   - `Caster` keeps existing behavior and remains the default.
   - `SelectedGrid` means the VFX should use `PlayerReservedCommand.SelectedGridIndex` as its world anchor.

2. Keep existing `PlaySkillAction(SkillMasterData)` APIs intact.
   - Add overloads that accept `PlayerReservedCommand`.
   - `BattleActionRunner` calls the command-aware overload only where it already has the command.

3. Resolve selected-grid VFX positions inside `BattleUnitAnimator`.
   - If `spawnAnchor` is `SelectedGrid`, find `GridManager`, validate `SelectedGridIndex`, and use `GridManager.GetWorldPositionByIndex`.
   - If any required context is missing, fall back to caster-anchor behavior.

4. Preserve existing DB controls.
   - Existing offset, render texture size, proxy height, sorting layer, and sorting offset continue to live on `BattleVfxEntry`.
   - Existing entries remain compatible because the new field defaults to `Caster`.

## Multiplayer Boundary

This change does not alter skill command validation, effect resolution, damage, status, or cost logic. It only reads an already synchronized command field for presentation placement.

## Verification Plan

- Add EditMode tests under `Assets/Tests/EditMode~/`.
- Check default anchor compatibility.
- Check selected-grid anchor chooses the selected grid world position.
- Check invalid selected-grid context falls back to caster spawn.
- Run MSBuild compile and `git diff --check`; Unity batchmode tests are skipped by project rule.
