using NUnit.Framework;
using System;
using System.IO;

public sealed class MonsterActionPresentationTargetGridVfxTests
{
    [Test]
    public void BattleUnitActionPresentation_ExposesTargetGridVfxToggle()
    {
        BattleUnitActionPresentation presentation = new();

        presentation.spawnVfxOnEachTargetGrid = true;

        Assert.That(presentation.spawnVfxOnEachTargetGrid, Is.True);
    }

    [Test]
    public void BattleUnitAnimator_ChecksMonsterPresentationBeforePlayerAttackFallback()
    {
        const string animatorPath =
            "Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Character/BattleUnitAnimator.cs";
        string source = File.ReadAllText(animatorPath);
        int methodStart = source.IndexOf(
            "public void PlayMonsterSkillAction(MonsterReservedCommand command)",
            StringComparison.Ordinal);
        Assert.That(methodStart, Is.GreaterThanOrEqualTo(0));

        int methodEnd = source.IndexOf(
            "private static bool IsActualMonsterMove",
            methodStart,
            StringComparison.Ordinal);
        Assert.That(methodEnd, Is.GreaterThan(methodStart));

        string methodBody = source.Substring(methodStart, methodEnd - methodStart);
        int monsterPresentationIndex = methodBody.IndexOf(
            "GetMonsterActionPresentation(command.ActionIndex)",
            StringComparison.Ordinal);
        int playerAttackFallbackIndex = methodBody.IndexOf(
            "playerSkillPresentations.GetAttack(command.ActionIndex)",
            StringComparison.Ordinal);

        Assert.That(monsterPresentationIndex, Is.GreaterThanOrEqualTo(0));
        Assert.That(playerAttackFallbackIndex, Is.GreaterThanOrEqualTo(0));
        Assert.That(monsterPresentationIndex, Is.LessThan(playerAttackFallbackIndex));
    }

    [Test]
    public void BattleUnitAnimator_UsesImpactOnlyProjectileForTargetGridVfx()
    {
        const string animatorPath =
            "Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Character/BattleUnitAnimator.cs";
        string source = File.ReadAllText(animatorPath);
        int methodStart = source.IndexOf(
            "private bool TrySpawnProjectileImpactOnMonsterTargetGrids(",
            StringComparison.Ordinal);
        Assert.That(methodStart, Is.GreaterThanOrEqualTo(0));

        int methodEnd = source.IndexOf(
            "private static bool TryResolveMonsterPresentationVfxAnchor(",
            methodStart,
            StringComparison.Ordinal);
        Assert.That(methodEnd, Is.GreaterThan(methodStart));

        string methodBody = source.Substring(methodStart, methodEnd - methodStart);

        StringAssert.Contains(
            "TrySpawnProjectileImpactOnMonsterTargetGrids(presentation.projectileVfx, command)",
            source);
        StringAssert.Contains(
            "GetMonsterPresentationVfxGridIndices(command)",
            source);
        StringAssert.Contains(
            "command.TargetGridIndices != null && command.TargetGridIndices.Count > 0",
            source);
        StringAssert.Contains(
            "return command.TargetGridIndices;",
            source);
        StringAssert.Contains(
            "!ShouldSpawnProjectileImpactOnMonsterTargetGrids(presentation)",
            source);
        StringAssert.Contains(
            "presentation.projectileVfx.missilePrefab == null",
            source);
        StringAssert.Contains(
            "presentation.projectileVfx.impactPrefab != null",
            source);
        StringAssert.Contains(
            "BattleVfxEntry targetGridImpactEntry = CreateTargetGridImpactVfxEntry(entry);",
            methodBody);
        StringAssert.Contains(
            "SpawnDetachedVfx(",
            methodBody);
        StringAssert.Contains(
            "targetGridImpactEntry",
            methodBody);
        StringAssert.Contains(
            "Mathf.Max(0.01f, entry.impactLifeTime)",
            methodBody);
        StringAssert.Contains(
            "applyFacingFlip: false",
            methodBody);
        StringAssert.Contains(
            "stabilizeVisualEffects: true",
            methodBody);
        StringAssert.Contains(
            "ResolveTargetGridImpactPosition(",
            source);
        StringAssert.DoesNotContain(
            "SpawnDirectWorldImpactVfx(",
            methodBody);
    }

    [Test]
    public void BattleUnitAnimator_AnchorsTargetGridVfxToGridCellTransform()
    {
        const string animatorPath =
            "Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Character/BattleUnitAnimator.cs";
        string source = File.ReadAllText(animatorPath);
        int methodStart = source.IndexOf(
            "private bool TrySpawnVfxOnMonsterTargetGrids(",
            StringComparison.Ordinal);
        Assert.That(methodStart, Is.GreaterThanOrEqualTo(0));

        int methodEnd = source.IndexOf(
            "private static Vector3 ResolveTargetGridImpactPosition(",
            methodStart,
            StringComparison.Ordinal);
        Assert.That(methodEnd, Is.GreaterThan(methodStart));

        string targetGridVfxSection = source.Substring(methodStart, methodEnd - methodStart);

        StringAssert.Contains(
            "TryResolveMonsterPresentationVfxAnchor(manager, gridIndex, out Vector3 anchorPosition)",
            targetGridVfxSection);
        StringAssert.Contains(
            "GridCell cell = manager.GetCellByIndex(gridIndex);",
            targetGridVfxSection);
        StringAssert.Contains(
            "anchorPosition = cell.transform.position;",
            targetGridVfxSection);
        StringAssert.DoesNotContain(
            "manager.GetWorldPositionByIndex(gridIndex)",
            targetGridVfxSection);
    }

    [Test]
    public void BattleUnitAnimator_DoesNotApplyCasterFacingFlipToTargetGridVfx()
    {
        const string animatorPath =
            "Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Character/BattleUnitAnimator.cs";
        string source = File.ReadAllText(animatorPath);
        int methodStart = source.IndexOf(
            "private bool TrySpawnVfxOnMonsterTargetGrids(",
            StringComparison.Ordinal);
        Assert.That(methodStart, Is.GreaterThanOrEqualTo(0));

        int methodEnd = source.IndexOf(
            "private bool TrySpawnProjectileImpactOnMonsterTargetGrids(",
            methodStart,
            StringComparison.Ordinal);
        Assert.That(methodEnd, Is.GreaterThan(methodStart));

        string targetGridVfxSection = source.Substring(methodStart, methodEnd - methodStart);

        StringAssert.Contains(
            "BattleVfxEntry targetGridEntry = CreateTargetGridVfxEntry(entry);",
            targetGridVfxSection);
        StringAssert.Contains(
            "SpawnDetachedVfx(",
            targetGridVfxSection);
        StringAssert.Contains(
            "applyFacingFlip: false",
            targetGridVfxSection);
        StringAssert.DoesNotContain(
            "stabilizeVisualEffects: true",
            targetGridVfxSection);
        StringAssert.DoesNotContain(
            "entry.impactFlipType",
            targetGridVfxSection);
    }

    [Test]
    public void BattleUnitAnimator_TargetGridImpactEntryUsesIndividualWorldRenderTexture()
    {
        const string animatorPath =
            "Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Character/BattleUnitAnimator.cs";
        string source = File.ReadAllText(animatorPath);
        int methodStart = source.IndexOf(
            "private static BattleVfxEntry CreateTargetGridImpactVfxEntry(",
            StringComparison.Ordinal);
        Assert.That(methodStart, Is.GreaterThanOrEqualTo(0));

        int methodEnd = source.IndexOf(
            "private void ApplyUnitVfxSortingTarget(",
            methodStart,
            StringComparison.Ordinal);
        Assert.That(methodEnd, Is.GreaterThan(methodStart));

        string methodBody = source.Substring(methodStart, methodEnd - methodStart);

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
    }

    [Test]
    public void BattleUnitAnimator_StabilizesAllChildVisualEffectsWithoutOverwritingAuthoredSeed()
    {
        const string animatorPath =
            "Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Character/BattleUnitAnimator.cs";
        string source = File.ReadAllText(animatorPath);
        int methodStart = source.IndexOf(
            "private static void StabilizeVisualEffectPlayback(GameObject vfx)",
            StringComparison.Ordinal);
        Assert.That(methodStart, Is.GreaterThanOrEqualTo(0));

        int methodEnd = source.IndexOf(
            "private static void EnsureVfxPauseController(",
            methodStart,
            StringComparison.Ordinal);
        Assert.That(methodEnd, Is.GreaterThan(methodStart));

        string methodBody = source.Substring(methodStart, methodEnd - methodStart);

        StringAssert.Contains(
            "GetComponentsInChildren<VisualEffect>(true)",
            methodBody);
        StringAssert.Contains(
            "visualEffect.resetSeedOnPlay = false;",
            methodBody);
        StringAssert.Contains(
            "visualEffect.Reinit();",
            methodBody);
        StringAssert.DoesNotContain(
            "visualEffect.startSeed =",
            methodBody);
    }

    [Test]
    public void BattleUnitAnimator_StablePlaybackFlagFlowsThroughDetachedSpawnFallbacks()
    {
        const string animatorPath =
            "Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Character/BattleUnitAnimator.cs";
        string source = File.ReadAllText(animatorPath);
        int firstOverloadStart = source.IndexOf(
            "private void SpawnDetachedVfx(",
            StringComparison.Ordinal);
        Assert.That(firstOverloadStart, Is.GreaterThanOrEqualTo(0));
        int methodStart = source.IndexOf(
            "private void SpawnDetachedVfx(",
            firstOverloadStart + 1,
            StringComparison.Ordinal);
        Assert.That(methodStart, Is.GreaterThanOrEqualTo(0));

        int methodEnd = source.IndexOf(
            "private static GameObject CreateDetachedVfxAnchor(",
            methodStart,
            StringComparison.Ordinal);
        Assert.That(methodEnd, Is.GreaterThan(methodStart));

        string methodBody = source.Substring(methodStart, methodEnd - methodStart);

        StringAssert.Contains(
            "bool stabilizeVisualEffects = false",
            methodBody);
        StringAssert.Contains(
            "stabilizeVisualEffects: stabilizeVisualEffects",
            methodBody);
        StringAssert.Contains(
            "TrySpawnDetachedDirectWorldVfx(entry, anchorWorldPosition, lifeTime, applyFacingFlip, stabilizeVisualEffects)",
            methodBody);
        StringAssert.Contains(
            "SpawnDetachedPrefabVfx(entry, anchorWorldPosition, lifeTime, applyFacingFlip, stabilizeVisualEffects);",
            methodBody);
    }

    [Test]
    public void BattleUnitAnimator_IndividualProxyFallbackUsesMainCameraVisibleLayer()
    {
        const string animatorPath =
            "Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Character/BattleUnitAnimator.cs";
        string source = File.ReadAllText(animatorPath);
        int methodStart = source.IndexOf(
            "private void SpawnDetachedPrefabVfx(",
            StringComparison.Ordinal);
        Assert.That(methodStart, Is.GreaterThanOrEqualTo(0));

        int methodEnd = source.IndexOf(
            "private static GameObject CreateDetachedVfxAnchor(",
            methodStart,
            StringComparison.Ordinal);
        Assert.That(methodEnd, Is.GreaterThan(methodStart));

        string methodBody = source.Substring(methodStart, methodEnd - methodStart);

        StringAssert.Contains(
            "if (stabilizeVisualEffects)",
            methodBody);
        StringAssert.Contains(
            "StabilizeVisualEffectPlayback(vfx);",
            methodBody);
        StringAssert.Contains(
            "if (entry.renderMode == BattleVfxRenderMode.IndividualWorldRenderTexture)",
            methodBody);
        StringAssert.Contains(
            "SetLayerRecursively(vfx, visibleLayer);",
            methodBody);
    }

    [Test]
    public void BattleUnitAnimator_ConfigureVfxCanSkipFacingFlip()
    {
        const string animatorPath =
            "Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Character/BattleUnitAnimator.cs";
        string source = File.ReadAllText(animatorPath);
        int methodStart = source.IndexOf(
            "private void ConfigureVfxInstance(GameObject vfx, BattleVfxEntry entry, bool applyFacingFlip)",
            StringComparison.Ordinal);
        Assert.That(methodStart, Is.GreaterThanOrEqualTo(0));

        int methodEnd = source.IndexOf(
            "private static void EnsureVfxPauseController(",
            methodStart,
            StringComparison.Ordinal);
        Assert.That(methodEnd, Is.GreaterThan(methodStart));

        string methodBody = source.Substring(methodStart, methodEnd - methodStart);

        StringAssert.Contains(
            "if (applyFacingFlip)",
            methodBody);
        StringAssert.Contains(
            "ApplyVfxFlip(vfx, entry.flipType);",
            methodBody);
    }
}
