# Lobby Resolution Dependent VFX And Preview Plan

## Problem

In the Lobby scene, relic shop purchase VFX and character preview UI under `CharacterPreviewSpawnRoot` only appear at the intended size in FullHD. Other runtime resolutions produce different visual sizes or positions.

## Investigation

- The global resolution system wraps overlay root Canvas children under `Resolution Viewport` to keep a 16:9 virtual screen.
- `LobbyRelicShopPresenter` creates the relic purchase transfer effect directly under the owner Canvas, which can place it outside the corrected viewport coordinate space.
- `CharacterPreviewCanvas` is a separate `ScreenSpaceCamera` Canvas rendered through `Main Camera` with `CanvasScaler.ConstantPixelSize`. `Char_01` and `Char_02` preview prefabs are 2275x1280 UI images, and `Char_03` is 2502x1408. Because the Canvas did not use the same 1920x1080 scale basis as the main UI Canvas, their apparent size changed when switching resolutions.
- `CharPick` instantiates preview UI prefabs directly under `CharacterPreviewSpawnRoot`, so the preview prefab uses whatever scale mode the parent preview Canvas currently has.
- `LobbyRelicShopPresenter` lives under the main overlay `Canvas`, whose `CanvasScaler` is `ScaleWithScreenSize` at 1920x1080. The purchase transfer VFX is created at runtime from `effect01.png` as a 60x60 `Image`. If it is created outside the corrected viewport/content root, it does not follow the same UI coordinate chain as the rest of the shop panel.
- Follow-up clarification: the target is not to redesign these elements to a generic UI scale. Their current FullHD-authored size is correct, and that apparent size must remain unchanged when switching to any supported runtime resolution.
- The previous inverse-scale attempt was wrong for this requirement. The correct behavior is for these visuals to remain in the same 1920x1080-authored UI scale chain as the FullHD layout, not to keep a constant physical pixel size.

## Recommended Design

1. Let `ResolutionCanvasViewportFitter` expose a resolved content root so temporary UI VFX can be parented inside the corrected viewport.
2. Route relic purchase transfer images and trail ghosts into the corrected content root when present.
3. Allow `ResolutionManager` to fit ScreenSpaceCamera and WorldSpace root canvases on target display 0, excluding only its own letterbox overlay.
4. Preserve the current 1920x1080 reference size behavior for preview prefabs and avoid changing combat/gameplay state.
5. Add EditMode tests for parent routing and camera/world Canvas fitting eligibility.

## Follow-up Design

1. Store `CharacterPreviewCanvas` as `ScaleWithScreenSize` with a 1920x1080 reference resolution, matching the main Lobby UI scale basis from the first loaded frame.
2. Keep `CharPick` defensively configuring the preview CanvasScaler at runtime in case the scene reference is changed or a prefab variant is used later.
3. Route relic purchase transfer images and their trail ghosts to the resolved `Resolution Viewport` content root, while keeping their authored 60x60 size and normal local scale.
4. Keep the change isolated to Lobby visual presentation; no gameplay or combat state changes.

## Relic Offer RenderTexture Investigation

- `RelicOffer_1`, `RelicOffer_2`, and `RelicOffer_3` each contain an instance of `Vfx_root_relic`.
- Those relic VFX instances render on layer 9 through the scene `VFXCamera`.
- `VFXCamera` renders to `Assets/VFX_RT.renderTexture`, a fixed 1920x1080 RenderTexture.
- The `RawImage(RT)` UI object displays that RenderTexture inside the Lobby Canvas.
- `BattleVfxCameraSync` was copying `Main Camera.rect`, `Main Camera.aspect`, and `Main Camera.projectionMatrix` into `VFXCamera`. When `ResolutionManager` letterboxes or updates the main camera viewport during runtime resolution changes, that reduced viewport was also applied inside the fixed RenderTexture. This made the VFX appear to change size and position inside `RawImage(RT)`.

## Relic Offer RenderTexture Fix

1. Keep copying camera transform and normal projection properties from the source camera.
2. When the target camera renders to a RenderTexture, force its viewport rect to full RT space `(0,0,1,1)`.
3. Use the RenderTexture's own width/height as the target aspect.
4. Recalculate the target projection matrix for the RenderTexture instead of copying the source camera's screen projection matrix.

## Relic Offer VFX Follow-up Investigation

- The remaining issue after the camera fix is the `Vfx_root_relic` object itself, not only the `VFXCamera`.
- `Vfx_root_relic` is a ParticleSystem hierarchy parented under each `RelicOffer_*` UI RectTransform.
- `ResolutionManager` can place the main Lobby Canvas children under `Resolution Viewport` and scale that content root to preserve the 16:9 authored area.
- UI graphics are expected to live in that scaled UI chain, but the relic VFX is rendered as a world object by `VFXCamera` into the fixed 1920x1080 `VFX_RT`.
- Because the ParticleSystem transform remains under the scaled UI hierarchy, its world position and world scale change when runtime resolution changes. The fixed RenderTexture then captures a different VFX placement even though the RawImage still displays the same texture asset.
- The FullHD-authored scene positions are:
  - `RelicOffer_1`: offer anchored X `-550` plus VFX local X about `-802.664`.
  - `RelicOffer_2`: offer anchored X `0` plus VFX local X about `-1347.72`.
  - `RelicOffer_3`: offer anchored X `550` plus VFX local X about `-1892.7`.
  - All three converge near the same RT-space X position and Y position in FullHD, so those summed coordinates must remain stable.

## Relic Offer VFX Superseded Detachment Attempt

This detachment approach reduced part of the issue, but it still kept the shared RenderTexture composition path. It was superseded by the per-offer RenderTexture proxy fix below.

1. Do not keep `Vfx_root_relic` under the scaled UI hierarchy once it is shown.
2. At the first show/apply point, calculate the VFX world matrix by stripping the current `Resolution Viewport` scale and offset from the existing scene hierarchy.
3. Move `Vfx_root_relic` to a scene-level `Lobby Relic Offer RT VFX Root` so later UI viewport scaling cannot change its world transform.
4. Reapply the cached world position, rotation, and scale in `LateUpdate`; layer hover scale on top of the cached world scale.
5. Explicitly hide the detached VFX when the offer button or panel is disabled, because it no longer inherits the inactive state from `RelicOffer_*`.
6. Add an EditMode regression test that builds a scaled `Resolution Viewport` hierarchy with the `relic_alter` parent scale and verifies `Vfx_root_relic` is detached while keeping the neutral FullHD world matrix.

## Relic Offer RT Camera Follow-up

- The relic offer VFX object is now detached from the scaled UI hierarchy, but the shared `VFXCamera` is still a child/sync target of `Main Camera`.
- `Main Camera` can receive runtime movement from panel transitions, viewport changes, and mouse parallax logic that depends on `Screen.width`/`Screen.height`.
- Because `VFXCamera` renders into a fixed 1920x1080 RenderTexture, copying the live main camera transform/projection into that RT camera can still move or resize the RT content when the game resolution changes.
- Battle scenes also use `BattleVfxCameraSync`, and their VFX camera must continue following the battle camera so world VFX stay aligned with units.

## Relic Offer RT Camera Final Fix

1. Add an opt-in `lockRenderTextureReferenceState` mode to `BattleVfxCameraSync`.
2. Keep the default behavior unchanged for battle scenes: RT cameras still copy source camera transform/projection while using the full RT viewport.
3. When the opt-in is enabled and the target camera renders to a RenderTexture, capture the target camera's authored reference state and restore it instead of copying the live source camera.
4. Run the same restore in `OnPreCull` so the RT camera is corrected after all `LateUpdate` camera movement and immediately before it renders.
5. Enable the opt-in only on the Lobby scene `VFXCamera`, preserving the FullHD-authored relic shop RT composition across runtime resolution changes.

## Render Output Canvas Investigation

- The shared `RawImage(RT)` object contains both `DissolveImage` and the relic VFX `VFXImage`.
- `DissolveImage` has `UIDissolveReveal`.
- During `Awake`, `UIDissolveReveal` calls `HideImmediate` when `hideOnAwake` is enabled.
- `HideImmediate` called `ResetRenderOutputCanvasSorting`, and that method used `ResolveRenderOutputCanvas`.
- `ResolveRenderOutputCanvas` creates a `Canvas` component on the `RawImage(RT)` render root when none exists.
- This means the render output root could become a separate runtime canvas while simply being hidden, before any dissolve reveal is shown.
- Because the relic VFX `VFXImage` is a sibling under the same render output root, this unnecessary runtime canvas can put the VFX output under a different canvas/sorting path than the main relic shop UI during resolution changes.

## Render Output Canvas Fix

1. Keep `ApplyFrontSorting` behavior unchanged: when a dissolve reveal is actually shown, it may still create/use a render output `Canvas` to bring the effect forward.
2. Change hide/reset behavior so it only resets an existing render output `Canvas`.
3. Do not create a render output `Canvas` from `HideImmediate`/startup hiding.
4. Add an EditMode regression test that active startup hide does not create a `Canvas` on `RawImage(RT)`.

## Relic Offer Canvas Scale Investigation

- The previous `Vfx_root_relic` detachment removed the `Resolution Viewport` scale and offset, but it still used the runtime `Canvas.transform.localToWorldMatrix` as the base matrix.
- The Lobby root Canvas is authored with a 1920x1080 `CanvasScaler` and `referencePixelsPerUnit` of 100.
- Screen-space Canvas transforms can be driven differently per runtime resolution so that UI graphics render at the correct screen scale.
- `Vfx_root_relic` is not drawn by the Canvas renderer. It is a world ParticleSystem captured by `VFXCamera` into the fixed 1920x1080 `VFX_RT`.
- Therefore, if the VFX world matrix keeps the current runtime Canvas scale, the RenderTexture content moves and resizes before the RawImage displays it.
- The FullHD-authored RT composition expects Canvas UI units to be converted into VFX world units at the reference 100 pixels per unit, not at the current resolution's Canvas world scale.

## Relic Offer Canvas Scale Fix

1. Keep calculating the VFX position relative to the `Resolution Viewport` so the authored UI offsets remain intact.
2. Replace the runtime Canvas `localToWorldMatrix` base with a RenderTexture reference matrix built from `CanvasScaler.referencePixelsPerUnit`.
3. For the Lobby Canvas, this locks the UI-to-world conversion to `1 / 100`, which is the FullHD-authored RT scale.
4. Preserve the existing scene-level detachment and LateUpdate reapply path so the VFX no longer follows later resolution refreshes.
5. Update the regression test to emulate a lower-resolution Canvas world scale and assert that the resulting VFX transform still uses the FullHD reference scale.

## Game View Resolution Follow-up Investigation

- The latest reproduction changes the Unity Game view resolution dropdown while testing, not only the in-game resolution option.
- In this path, Unity can drive the ScreenSpace Canvas transform and the `Resolution Viewport` RectTransform differently while the scene is running.
- The previous follow-up still calculated the relic VFX reference matrix from `ringTransform.localToWorldMatrix` and `viewportRoot.worldToLocalMatrix`.
- Those matrices already include the Game view's current Canvas/Viewport driven scale before the code attempts to remove it.
- Since `Vfx_root_relic` is rendered by `VFXCamera` into a fixed 1920x1080 RenderTexture, its reference matrix must be derived from the authored FullHD UI local chain, not from runtime world matrices.

## Game View Resolution Superseded Matrix Fix

This matrix reconstruction approach was also superseded by the per-offer RenderTexture proxy fix below because it still relied on keeping a world ParticleSystem aligned with a UI-authored composition.

1. Reconstruct the `Vfx_root_relic` reference matrix by walking the local hierarchy from the VFX to the `Resolution Viewport`.
2. Use `RectTransform.anchoredPosition3D` for UI parents and `Transform.localPosition` for the particle root so stretch-driven RectTransform positions do not enter the calculation.
3. Multiply that authored local chain by a neutral `Resolution Viewport` rotation and a Canvas reference matrix based on `referencePixelsPerUnit`.
4. Keep detaching the VFX to the scene-level RT VFX root after the first successful calculation.
5. Strengthen the EditMode regression test so the Canvas runtime scale can be changed independently while the expected world transform still matches the FullHD-authored UI chain.

## Battle Scene Comparison

- Battle VFX do not depend on the active screen resolution because `BattleWorldVfxRenderer` renders each effect in a separate off-screen render space.
- Each battle effect owns its own RenderTexture and orthographic camera, then shows only the rendered texture through a proxy.
- Lobby relic offer VFX still used the older mixed path: `Vfx_root_relic` was a child of `RelicOffer_*` UI RectTransforms, but it was rendered as a world ParticleSystem by the shared `VFXCamera` into the shared `VFX_RT`.
- The scene-authored `VFXImage` RawImage is a corrected full-screen composition surface, while the relic VFX objects are driven by Canvas/RectTransform world transforms. Runtime Game view resolution changes can therefore alter the particle world matrix even when the RawImage texture size stays fixed.
- This confirms the remaining bug is a Canvas-coordinate versus world-coordinate render path mismatch in the Lobby scene, not the same class of Battle VFX problem.

## Relic Offer Per-Offer RenderTexture Fix

1. Stop showing the serialized `Vfx_root_relic` objects directly under `RelicOffer_1` to `RelicOffer_3`; keep them as templates only.
2. For each bound relic offer button, create a child `RarityRingVfxProxy` RawImage under that same offer RectTransform.
3. Clone the template VFX into a scene-level isolated render root, render it with its own orthographic camera into an individual RenderTexture, and assign that texture to the button-local RawImage.
4. Keep the RawImage anchored at the offer center with a 250x250 authored UI size, so its position and size are resolved by the same Canvas chain as the relic icon.
5. Copy the existing shared `VFXImage` material when available so the visual blend remains consistent, but avoid using the shared `VFX_RT` content for relic offer rings.
6. Drive rarity colors, restart, fade, and cleanup through the runtime clone and release the individual RenderTexture/material when the offer UI is destroyed.
7. Replace the previous world-matrix detachment regression test with tests that assert the source VFX remains inactive and the visible output is the per-offer UI proxy.

## Scope

- Update `Assets/Project/Scripts/Core/Managers/ResolutionManager.cs`.
- Update `Assets/Project/Scripts/Gameplay/Scene/Lobby/RelicShop/LobbyRelicShopPresenter.cs`.
- Update `Assets/Project/Scripts/Gameplay/Scene/Lobby/RelicShop/LobbyRelicOfferButtonUI.cs`.
- Update `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/BattleVfxCameraSync.cs`.
- Update `Assets/Project/Scripts/UI/Battle/Canvas/UIDissolveReveal.cs`.
- Update `Assets/Project/Scenes/YDM/Lobby.unity`.
- Update `Assets/Tests/EditMode~/ResolutionManagerTests.cs`.
- Update `Assets/Tests/EditMode~/AnimationVfxLoadoutCleanupTests.cs`.
- Update `Assets/Tests/EditMode~/LobbyRelicOfferButtonUITests.cs`.
- Update `Assets/Tests/EditMode~/UIDissolveRevealLayoutTests.cs`.
