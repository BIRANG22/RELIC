# SpriteAni Direct VFX Render Design

## Background

Sprite animation VFX prefabs use `SpriteRenderer` and `Animator`, but the test skill VFX entry is using `IndividualWorldRenderTexture`. That mode renders a prefab through an offscreen camera into a RenderTexture, then displays it on a world quad. It works for many particle VFX, but it can crop large 2D sprite frames and makes root transform offsets look like world placement errors.

`Vfx_SpriteAni_flash_explosion.prefab` also has a root local position of `(-4, 1.2, 0)`, while the source sprite frames are centered inside the 1280x720 canvas. This root offset causes the effect to appear away from the selected grid.

## Design

1. Use `DirectWorldRenderer` for SpriteAni skill VFX.
   - Sprite animation prefabs should render through their real `SpriteRenderer` components instead of the RenderTexture proxy quad.
   - Existing particle VFX can continue to use `IndividualWorldRenderTexture`.

2. Let Direct world VFX opt into DB-driven sizing.
   - Add a `scaleDirectWorldRendererToProxyHeight` option on `BattleVfxEntry`.
   - When enabled, direct-rendered VFX scales its instantiated renderer bounds to `proxyWorldHeight`.
   - This keeps existing entries stable because the new option defaults to false.

3. Apply DB offset to Direct world VFX.
   - Caster-anchored direct VFX uses `proxyWorldOffset` as local offset from the caster spawn point.
   - Selected-grid direct VFX uses `proxyWorldOffset` as world offset from the selected grid anchor.

4. Normalize the flash explosion SpriteAni prefab.
   - Reset the prefab root local position to `(0, 0, 0)`.
   - Control placement and size from `SkillVfxDatabase.asset`.

## Verification Plan

- Add EditMode tests for direct world VFX offset and opt-in height scaling.
- Update the flash explosion skill VFX DB entry to direct rendering, selected-grid anchoring, and DB-driven height.
- Run MSBuild and `git diff --check`.
- Unity batchmode tests are skipped by project rule.
