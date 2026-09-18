# WebGL VFX Compatibility Implementation Plan

## Goal

Prevent WebGL-only VFX loss while preserving Windows VFX Graph presentation.

## Steps

1. Add a failing EditMode test for explicit WebGL/Windows fallback selection.
2. Add the presentation-only fallback component and make the test pass.
3. Attach explicit authored and fallback roots to the affected scene/prefab VFX instances.
4. Add regression coverage for shipping Shader Model 4.5 VFX shader sources and lower only the compatible sources to target 3.5.
5. Compile scripts and run editor tests; validate a WebGL build in the browser when the Unity editor is available.
