# Test_VFX Workbench Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a usable VFX workbench inside `Assets/Project/Scenes/YDM/Test_VFX.unity`.

**Architecture:** Add small test-scene-only runtime helpers that reuse the production `BattleVfxEntry` and `BattleWorldVfxRenderer` path. Keep all battle state untouched; the workbench only spawns visual objects and adjusts renderer/layer data.

**Tech Stack:** Unity C#, IMGUI debug panel, UnityEditor `AssetDatabase` for editor-only asset discovery, EditMode tests under `Assets/Tests/EditMode~/`.

---

### Task 1: Lock VFX Settings Behavior With Tests

**Files:**
- Create: `Assets/Tests/EditMode~/TestVfxWorkbenchTests.cs`

- [ ] Write tests for `TestVfxSpawnSettings.ToEntry`.
- [ ] Write tests for recursive layer application.
- [ ] Write tests for direct renderer sorting order calculation.
- [ ] Run MSBuild and confirm the tests fail to compile because production types do not exist yet.

### Task 2: Add Workbench Settings And Utility

**Files:**
- Create: `Assets/Project/Scripts/Debug/TestVfxSpawnSettings.cs`
- Create: `Assets/Project/Scripts/Debug/TestVfxWorkbenchUtility.cs`

- [ ] Implement `TestVfxSpawnSettings.ToEntry(GameObject prefab)`.
- [ ] Implement `TestVfxWorkbenchUtility.SetLayerRecursively`.
- [ ] Implement `TestVfxWorkbenchUtility.ApplyDirectRendererSorting`.
- [ ] Implement `TestVfxWorkbenchUtility.RestartParticles`.
- [ ] Run MSBuild and confirm the new tests compile.

### Task 3: Add Runtime Workbench

**Files:**
- Create: `Assets/Project/Scripts/Debug/TestVfxWorkbench.cs`

- [ ] Add sample unit spawning for player and monster prefabs.
- [ ] Discover VFX prefabs from `Assets/Project/Art/VFX` in editor play mode.
- [ ] Add IMGUI controls for target, prefab, render mode, layer, sorting, transform, RT, proxy, lifetime, and flip type.
- [ ] Add Play, Clear, Repeat, and unit action buttons.
- [ ] Keep all battle core state untouched.

### Task 4: Install The Workbench Into The Scene

**Files:**
- Modify: `Assets/Project/Scenes/YDM/Test_VFX.unity`

- [ ] Add a root `TestVfxWorkbench` GameObject with the runtime component.
- [ ] Keep existing camera, canvas, VFX camera, and render texture objects intact.

### Task 5: Verify

**Files:**
- Verify: `Assembly-CSharp.csproj`
- Verify: `Assembly-CSharp-Editor.csproj`

- [ ] Run approved MSBuild for runtime assembly.
- [ ] Run approved MSBuild for editor assembly if editor code is added.
- [ ] Do not run Unity batchmode tests because the project rule assumes the Unity Editor is open.

