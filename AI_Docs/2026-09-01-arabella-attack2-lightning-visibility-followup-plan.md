# Arabella Attack2 Lightning Visibility Followup Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make every Arabella `AttackAction2` target-grid lightning instance show the full lightning strike, not only the spreading shockwave.

**Architecture:** Keep target selection and grid anchoring on `MonsterReservedCommand.TargetGridIndices` and `GridCell.transform.position`. Route missile-less projectile impact target-grid VFX through the existing individual RenderTexture world proxy, because the battle main camera excludes the dedicated VFX layer. Configure only these spawned VFX Graph instances with a stable prefab-authored seed and restart them once so simultaneous captures render the same complete strike.

**Tech Stack:** Unity C#, NUnit EditMode regression tests, existing `BattleUnitAnimator`, `BattleVfxEntry`, and `BattleWorldVfxRenderer` presentation systems.

**Spec:** `AI_Docs/2026-09-01-monster-action-target-grid-vfx-design.md`

## Global Constraints

- All docs stay inside `AI_Docs`.
- Tests stay inside `Assets/Tests/EditMode~/` or `Assets/Tests/PlayMode~/`.
- Do not run Unity batchmode tests; the Unity editor is assumed open.
- Presentation VFX must only read battle result data and must not change command, damage, status, random, or state mutation logic.
- No commit, push, PR, branch creation, branch switch, or worktree operation without explicit user approval.

---

### Task 1: Restore The Visible Impact-Only Render Route

**Files:**
- Modify: `Assets/Tests/EditMode~/MonsterActionPresentationTargetGridVfxTests.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Character/BattleUnitAnimator.cs`

**Interfaces:**
- Consumes: `TrySpawnProjectileImpactOnMonsterTargetGrids(BattleProjectileVfxEntry entry, MonsterReservedCommand command)`
- Produces: `CreateTargetGridImpactVfxEntry(BattleProjectileVfxEntry source)` returning a `BattleVfxEntry` with `prefab = source.impactPrefab`, `flipType = VfxFlipType.None`, and `renderMode = BattleVfxRenderMode.IndividualWorldRenderTexture`

- [x] **Step 1: Write the failing test**

Replace the direct-world expectation with `BattleUnitAnimator_TargetGridImpactEntryUsesIndividualWorldRenderTexture` and assert:

```csharp
StringAssert.Contains(
    "prefab = source.impactPrefab",
    methodBody);
StringAssert.Contains(
    "flipType = VfxFlipType.None",
    methodBody);
StringAssert.Contains(
    "renderMode = BattleVfxRenderMode.IndividualWorldRenderTexture",
    methodBody);
StringAssert.DoesNotContain(
    "source.impactFlipType",
    methodBody);
```

The production change this catches is routing the VFX-layer instance around its visible world proxy.

- [x] **Step 2: Run test to verify it fails**

Run the non-batch source-level regression check for this method. Expected: failure because production code returns `BattleVfxRenderMode.DirectWorldRenderer`.

- [x] **Step 3: Write minimal implementation**

Change only `CreateTargetGridImpactVfxEntry`:

```csharp
return new BattleVfxEntry
{
    prefab = source.impactPrefab,
    flipType = VfxFlipType.None,
    renderMode = BattleVfxRenderMode.IndividualWorldRenderTexture
};
```

The existing `SpawnDetachedVfx(..., applyFacingFlip: false)` call then chooses `BattleWorldVfxRenderer.TrySpawnDetached`, renders the dedicated VFX layer, and presents its proxy at the target grid.

- [x] **Step 4: Run test to verify it passes**

Run the same source-level regression command. Expected: pass.

- [x] **Step 5: Run the focused regression check**

Run the same source-level regression command. Expected: pass.

### Task 2: Stabilize Target-Grid VFX Graph Playback

**Files:**
- Modify: `Assets/Tests/EditMode~/MonsterActionPresentationTargetGridVfxTests.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Character/BattleUnitAnimator.cs`

**Interfaces:**
- Consumes: the existing `SpawnDetachedVfx` configuration callback used by `BattleWorldVfxRenderer.TrySpawnDetached`
- Produces: target-grid-only Visual Effect configuration that preserves each component's authored `startSeed`, disables `resetSeedOnPlay`, and invokes `Reinit()` once

- [x] **Step 1: Write the failing test**

Add focused assertions that the impact-only target-grid call enables stable Visual Effect playback, while the regular target-grid call does not. Add a focused helper contract test requiring all child `VisualEffect` components to set `resetSeedOnPlay = false` and call `Reinit()` without overwriting `startSeed`.

- [x] **Step 2: Run the regression check and verify failure**

Expected: failure because no stable-playback option or Visual Effect restart helper exists.

- [x] **Step 3: Write minimal implementation**

Add `using UnityEngine.VFX`, an optional `stabilizeVisualEffects` parameter to the detached spawn/configuration path, and a private helper:

```csharp
private static void StabilizeVisualEffectPlayback(GameObject vfx)
{
    if (vfx == null)
        return;

    VisualEffect[] visualEffects = vfx.GetComponentsInChildren<VisualEffect>(true);

    for (int i = 0; i < visualEffects.Length; i++)
    {
        VisualEffect visualEffect = visualEffects[i];
        visualEffect.resetSeedOnPlay = false;
        visualEffect.Reinit();
    }
}
```

Enable it only from `TrySpawnProjectileImpactOnMonsterTargetGrids`. Keeping `startSeed` untouched uses the prefab-authored seed (`0` for Arabella AttackAction2) without changing the prefab asset or other presentation routes.

- [x] **Step 4: Run focused checks and verify pass**

Expected: the individual proxy and target-grid-only stabilization checks pass.

- [x] **Step 5: Verify compile and diff health**

Run runtime and editor MSBuild commands, then `git diff --check`. Expected: MSBuild exits 0 and diff check has no whitespace errors.

### Task 3: Preserve Playback And Visibility In Proxy Fallback

**Files:**
- Modify: `Assets/Tests/EditMode~/MonsterActionPresentationTargetGridVfxTests.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Character/BattleUnitAnimator.cs`

**Interfaces:**
- Consumes: `SpawnDetachedVfx` fallback paths and the caster spawn transform's visible layer
- Produces: stable Visual Effect playback in every detached fallback and a main-camera-visible layer for failed individual proxy captures

- [x] **Step 1: Write failing fallback regression tests**
- [x] **Step 2: Verify the tests fail because the option and visible layer are not propagated**
- [x] **Step 3: Pass `stabilizeVisualEffects` through direct/prefab fallbacks and move individual-proxy fallback instances to the visible layer**
- [x] **Step 4: Run all focused tests and both C# builds**

## Self-Review

- Spec coverage: target-grid indices, grid-cell anchors, duplicate suppression, disabled facing flip, visible individual world proxy routing, target-grid-only stable VFX Graph playback, and multiplayer presentation boundary are covered.
- Placeholder scan: no TBD/TODO placeholders remain.
- Type consistency: method and enum names match existing `BattleUnitAnimator`, `BattleProjectileVfxEntry`, and `BattleVfxEntry` code.
