# Arabella Attack2 Target Grid VFX Fix Plan

## Investigation

- Arabella `AttackAction2` has `spawnVfxOnEachTargetGrid` enabled, no regular action VFX prefab, and an impact-only `projectileVfx.impactPrefab` pointing to `Vfx_Mon_B_01_attack_02.prefab`.
- `S_Monster_22` collects alive players' `CurrentGridIndex` values, and `BattleMonsterTurnPlanner` copies those exact grid indices into `MonsterReservedCommand.TargetGridIndices`.
- `GridManager` resolves grid indices through its serialized `GridCell[]` order, and the Battle scene maps `Grid_00` through `Grid_34` in order.
- The original mirrored-placement problem came from the target-grid impact path resolving the right command grid, then playing through a caster-oriented direct impact helper.
- Follow-up testing showed that routing Arabella's lightning through per-instance board-proxy RenderTextures can make only one full lightning strike visible while the other instances show mainly the shockwave.

## Design

- Keep command, target selection, damage, and range calculation unchanged.
- For impact-only target-grid VFX, build a runtime `BattleVfxEntry` from the impact prefab with `flipType = VfxFlipType.None` and `renderMode = BattleVfxRenderMode.DirectWorldRenderer`.
- Spawn that entry through `SpawnDetachedVfx` at the resolved grid-cell world anchor plus `impactOffset`.
- This keeps placement on the detached grid anchor while avoiding the RenderTexture capture path that can hide parts of the lightning graph when several instances play together.
- Continue deduplicating grid indices and skipping invalid or missing cells.

## Steps

1. Update the existing EditMode regression test to require the detached direct-world impact entry.
2. Change `BattleUnitAnimator` so target-grid impact-only projectile VFX keeps detached grid anchoring and uses `DirectWorldRenderer`.
3. Keep the old single-target projectile impact helper unchanged for non-target-grid projectile playback.
4. Verify with source checks, MSBuild compile, and `git diff --check`. Unity batchmode tests are skipped by project rule.

## Multiplayer Boundary

This is presentation-only. It reads existing synchronized `TargetGridIndices` and does not change battle commands, state mutation, damage, status effects, random rolls, or network DTOs.
