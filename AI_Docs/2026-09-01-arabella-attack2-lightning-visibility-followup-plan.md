# Arabella Attack2 Lightning Visibility Followup Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make every Arabella `AttackAction2` target-grid lightning instance show the full lightning strike, not only the spreading shockwave.

**Architecture:** Keep target selection and grid anchoring on `MonsterReservedCommand.TargetGridIndices` and `GridCell.transform.position`. Route only missile-less projectile impact target-grid VFX through `DirectWorldRenderer`, with caster-facing flip disabled, so the VFX is rendered directly at each detached grid anchor instead of being captured through a per-instance RenderTexture.

**Tech Stack:** Unity C#, NUnit EditMode regression tests, existing `BattleUnitAnimator`, `BattleVfxEntry`, and `BattleWorldVfxRenderer` presentation systems.

**Spec:** `AI_Docs/2026-09-01-monster-action-target-grid-vfx-design.md`

## Global Constraints

- All docs stay inside `AI_Docs`.
- Tests stay inside `Assets/Tests/EditMode~/` or `Assets/Tests/PlayMode~/`.
- Do not run Unity batchmode tests; the Unity editor is assumed open.
- Presentation VFX must only read battle result data and must not change command, damage, status, random, or state mutation logic.
- No commit, push, PR, branch creation, branch switch, or worktree operation without explicit user approval.

---

### Task 1: Lock The Impact-Only Render Route

**Files:**
- Modify: `Assets/Tests/EditMode~/MonsterActionPresentationTargetGridVfxTests.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Character/BattleUnitAnimator.cs`

**Interfaces:**
- Consumes: `TrySpawnProjectileImpactOnMonsterTargetGrids(BattleProjectileVfxEntry entry, MonsterReservedCommand command)`
- Produces: `CreateTargetGridImpactVfxEntry(BattleProjectileVfxEntry source)` returning a `BattleVfxEntry` with `prefab = source.impactPrefab`, `flipType = VfxFlipType.None`, and `renderMode = BattleVfxRenderMode.DirectWorldRenderer`

- [x] **Step 1: Write the failing test**

Update `BattleUnitAnimator_TargetGridImpactEntryUsesProxyRendererDefaults` into `BattleUnitAnimator_TargetGridImpactEntryUsesDirectWorldRenderer` and assert:

```csharp
StringAssert.Contains(
    "prefab = source.impactPrefab",
    methodBody);
StringAssert.Contains(
    "flipType = VfxFlipType.None",
    methodBody);
StringAssert.Contains(
    "renderMode = BattleVfxRenderMode.DirectWorldRenderer",
    methodBody);
StringAssert.DoesNotContain(
    "source.impactFlipType",
    methodBody);
```

Also update `BattleUnitAnimator_UsesImpactOnlyProjectileForTargetGridVfx` so it continues to require `SpawnDetachedVfx(` and `applyFacingFlip: false`, but no longer rejects `BattleVfxRenderMode.DirectWorldRenderer`.

- [x] **Step 2: Run test to verify it fails**

Run the available non-batch source-level regression command for this test file. Expected: failure because production code still returns `BattleVfxRenderMode.IndividualWorldRenderTexture`.

- [x] **Step 3: Write minimal implementation**

Change only `CreateTargetGridImpactVfxEntry`:

```csharp
return new BattleVfxEntry
{
    prefab = source.impactPrefab,
    flipType = VfxFlipType.None,
    renderMode = BattleVfxRenderMode.DirectWorldRenderer
};
```

The existing `SpawnDetachedVfx(..., applyFacingFlip: false)` call then chooses `TrySpawnDetachedDirectWorldVfx`, creates a detached grid anchor, configures direct renderer sorting from that anchor's y position, and destroys the anchor after `impactLifeTime`.

- [x] **Step 4: Run test to verify it passes**

Run the same source-level regression command. Expected: pass.

- [x] **Step 5: Verify compile and diff health**

Run runtime and editor MSBuild commands, then `git diff --check`. Expected: MSBuild exits 0 and diff check has no whitespace errors.

## Self-Review

- Spec coverage: target-grid indices, grid-cell anchors, duplicate suppression, disabled facing flip, direct impact-only rendering, and multiplayer presentation boundary are covered.
- Placeholder scan: no TBD/TODO placeholders remain.
- Type consistency: method and enum names match existing `BattleUnitAnimator`, `BattleProjectileVfxEntry`, and `BattleVfxEntry` code.
