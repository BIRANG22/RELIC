# WebGL VFX Compatibility Design

## Goal

Keep the authored VFX Graph presentation in Windows players while giving WebGL a compatible Shuriken Particle System fallback. Keep WebGL-compatible shader effects visible without enabling unsupported Shader Model 4.5 paths.

## Confirmed causes

- `VFX_title_smoke` uses `SH_Vefects_VFX_Dissolve_Newest.shader`, which declares `#pragma target 4.5`.
- `St1_00/fire` uses a Visual Effect Graph (`vfxgraph_StylizedSmoke_v1_Fire.prefab`). VFX Graph requires compute shader and SSBO support, which WebGL cannot provide.
- Other shipping uses of VFX Graph are the restroom smoke and N_02/N_03 attack and skill effects.

## Design

Add a presentation-only `WebGlVfxFallback` component. It owns explicit references to the authored VFX Graph root and a preplaced Shuriken fallback root. At initialization it enables the fallback only for `RuntimePlatform.WebGLPlayer`; Windows and other supported platforms keep the original root. The component does not affect battle command, state, or result logic.

For the title smoke and the identified Shader Model 4.5 material users, use WebGL-compatible shader variants only after the shader source compiles at target 3.5. Windows continues to render the same material and authored parameters.

## Asset routing

- `St1_00/fire`: authored Visual Effect Graph -> existing `VFX_GroundFire` Shuriken presentation.
- Restroom and monster VFX Graph roots: authored graph -> an explicit preplaced Shuriken fallback selected per effect.
- Title smoke: retain the authored prefab, but compile its dissolve shader for WebGL-compatible target 3.5.

## Verification

- EditMode policy tests verify WebGL selects the fallback and Windows selects the authored graph.
- Shader compatibility tests reject target 4.5 for every shader reachable from a shipping VFX material.
- Compile Assembly-CSharp and run the relevant EditMode tests in the Unity Test Runner when the editor is available.
