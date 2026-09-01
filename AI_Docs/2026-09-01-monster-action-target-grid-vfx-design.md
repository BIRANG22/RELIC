# Monster Action Target Grid VFX Design

## Context

Some monster skills target multiple player grid cells. Their action presentation VFX previously spawned once at the monster/unit VFX anchor, so skills such as lightning strikes could not show one effect per attacked grid.

## Decision

- Keep monster action animation selection on `Monster Action Presentations`.
- Add `spawnVfxOnEachTargetGrid` to `BattleUnitActionPresentation`.
- Monster actions now prefer their `Monster Action Presentations` slot before falling back to the shared `Player Skill Presentations` Attack 1-3 slots.
- When the option is disabled, action VFX keeps the existing unit-anchor spawn behavior.
- When the option is enabled for a monster action, the same presentation VFX is spawned once per unique presentation grid.
- Presentation grids use `MonsterReservedCommand.TargetGridIndices` first, then fall back to `RangeGridIndices` if no resolved target exists. This matches skills that should show VFX on the grids actually being attacked, such as lightning on each player-occupied grid.
- If the regular presentation VFX is empty and the presentation uses an impact-only projectile VFX, the impact VFX is spawned once per unique presentation grid instead.
- Impact-only target-grid VFX suppresses the old single projectile impact playback so multi-grid skills do not show an extra duplicate impact.
- Regular target-grid VFX keeps the source `BattleVfxEntry` render route so authored board-proxy settings remain inspector-driven.
- Impact-only projectile target-grid VFX uses a runtime `BattleVfxEntry` with `BattleVfxRenderMode.IndividualWorldRenderTexture`. Battle VFX instances are placed on the dedicated `VFX` layer, which the battle main camera does not render, so bypassing the world proxy makes direct-world target-grid effects invisible.
- The impact path is spawned from a detached world proxy at the resolved `GridCell` position, not from the caster transform, so it does not reintroduce the old mirrored board-quadrant placement.
- Target-grid impact VFX Graph instances use the prefab-authored start seed with `resetSeedOnPlay` disabled, then restart once after configuration. This keeps simultaneous RenderTexture captures deterministic so every grid receives the same complete authored strike instead of independently randomized partial results.
- Stable VFX Graph playback is applied only to the impact-only target-grid presentation branch. The source prefab and every other use of the same VFX remain unchanged.
- If individual proxy creation fails, the detached fallback preserves the stable playback option and moves the instance from the dedicated VFX layer to the caster's main-camera-visible layer. This prevents the fallback from silently becoming invisible.
- The runtime impact entry uses `VfxFlipType.None` so the target-grid effect is independent from the caster's facing direction.
- Target-grid VFX resolves the `GridCell` from the command `GridIndex` and anchors to that cell transform's world position. This keeps presentation placement tied to the same cell object used by battle highlights and unit placement, instead of adding a second coordinate interpretation inside the animator.
- Impact positions preserve each resolved grid cell's world position, including depth, instead of reusing the projectile start depth.
- Target-grid VFX suppresses caster-facing flip. Ground-anchored effects should stay on the resolved grid cell even when the monster faces left; otherwise `RotationY180` can mirror a VFX Graph effect diagonally around its root.
- Invalid, duplicate, or unavailable grid indices are skipped.
- If no target-grid VFX can be spawned, the old single unit-anchor VFX path is used as fallback.

## Multiplayer Boundary

This option only reads the resolved monster command target grid indices for presentation. It does not change target selection, damage calculation, battle state, or command synchronization.
