# Player Skill Projectile VFX Fix

## Investigation

- `PlayerSkillReservationController` stores selection skills through `PlayerReservedCommand.SetSelectionAreaResult`, including `SelectedGridIndex`.
- `BattleUnitAnimator.PlaySkillTargetVfx` already resolves `SelectedGridIndex` to a `GridCell` world position and supports impact-only `BattleProjectileVfxEntry`.
- `BattleActionRunner` only calls `BattleUnitAnimator.PlaySkillAction` for player skill execution. That path resolves `SkillVfxDatabase.TryGetVfx` and consumes only `SkillVfxEntry.Vfx`.
- No production execution path calls `PlaySkillTargetVfx`, so configured `SkillVfxEntry.ProjectileVfx` entries such as `S_Ability_09` and `S_Ability_11` are never played.

## Recommended Design

- Keep battle result calculation unchanged.
- In player skill execution, after starting the skill action animation, run selected-grid projectile VFX when the command is a non-move selection skill and has a selected grid.
- Reuse the existing `PlaySkillTargetVfx` path so the VFX position is derived from stable command data, not from UI state.

## Verification Plan

- Add EditMode source regression coverage that requires the player damage and non-damage execution paths to call selected-grid projectile VFX.
- Compile `Assembly-CSharp.csproj` and `Assembly-CSharp-Editor.csproj` with package restore disabled.
- Do not run Unity batchmode tests because project rules say the editor is assumed open and batchmode tests should not be attempted.
