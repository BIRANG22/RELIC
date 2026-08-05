using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class TestVfxWorkbenchTests
{
    [Test]
    public void SpawnSettings_ToEntryClampsNumericValuesAndCopiesVfxFields()
    {
        GameObject prefab = new("WorkbenchVfxPrefab");

        try
        {
            TestVfxSpawnSettings settings = new()
            {
                FlipType = VfxFlipType.ParticleRendererFlipY,
                RenderMode = BattleVfxRenderMode.DirectWorldRenderer,
                ProxyBlendMode = BattleVfxProxyBlendMode.Alpha,
                PlaySfx = true,
                SfxId = "Hit_01",
                SfxDelay = -0.5f,
                SfxVolumeMultiplier = -1f,
                RouteEmbeddedAudioSourcesThroughAudioManager = false,
                RemoveEmbeddedAudioSources = false,
                RenderTextureWidth = 0,
                RenderTextureHeight = -8,
                RenderCameraOrthographicSize = 0f,
                ProxyWorldHeight = -1f,
                ProxyWorldOffset = new Vector3(1f, 2f, 3f),
                ScaleDirectWorldRendererToProxyHeight = true,
                SortingLayerName = "Unit",
                SortingOrderOffset = 12,
                SortingWorldYOffset = 0.25f,
                YMultiplier = 0f
            };

            BattleVfxEntry entry = settings.ToEntry(prefab);

            Assert.That(entry.prefab, Is.SameAs(prefab));
            Assert.That(entry.flipType, Is.EqualTo(VfxFlipType.ParticleRendererFlipY));
            Assert.That(entry.renderMode, Is.EqualTo(BattleVfxRenderMode.DirectWorldRenderer));
            Assert.That(entry.proxyBlendMode, Is.EqualTo(BattleVfxProxyBlendMode.Alpha));
            Assert.That(entry.sfx.playSfx, Is.True);
            Assert.That(entry.sfx.sfxId, Is.EqualTo("Hit_01"));
            Assert.That(entry.sfx.delay, Is.EqualTo(0f));
            Assert.That(entry.sfx.volumeMultiplier, Is.EqualTo(0f));
            Assert.That(entry.sfx.routeEmbeddedAudioSourcesThroughAudioManager, Is.False);
            Assert.That(entry.sfx.removeEmbeddedAudioSources, Is.False);
            Assert.That(entry.renderTextureWidth, Is.EqualTo(1));
            Assert.That(entry.renderTextureHeight, Is.EqualTo(1));
            Assert.That(entry.renderCameraOrthographicSize, Is.EqualTo(0.01f));
            Assert.That(entry.proxyWorldHeight, Is.EqualTo(0.01f));
            Assert.That(entry.proxyWorldOffset, Is.EqualTo(new Vector3(1f, 2f, 3f)));
            Assert.That(entry.scaleDirectWorldRendererToProxyHeight, Is.True);
            Assert.That(entry.proxySortingLayerName, Is.EqualTo("Unit"));
            Assert.That(entry.proxySortingOrderOffset, Is.EqualTo(12));
            Assert.That(entry.proxySortingWorldYOffset, Is.EqualTo(0.25f));
            Assert.That(entry.proxyYMultiplier, Is.EqualTo(0.01f));
        }
        finally
        {
            DestroyObject(prefab);
        }
    }

    [Test]
    public void SetLayerRecursively_AppliesLayerToChildren()
    {
        GameObject root = new("Root");
        GameObject child = new("Child");
        child.transform.SetParent(root.transform);
        int vfxLayer = LayerMask.NameToLayer("VFX");

        try
        {
            Assert.That(vfxLayer, Is.GreaterThanOrEqualTo(0));

            TestVfxWorkbenchUtility.SetLayerRecursively(root, vfxLayer);

            Assert.That(root.layer, Is.EqualTo(vfxLayer));
            Assert.That(child.layer, Is.EqualTo(vfxLayer));
        }
        finally
        {
            DestroyObject(root);
        }
    }

    [Test]
    public void ApplyDirectRendererSorting_KeepsPrefabSortingOrderAsOffset()
    {
        GameObject root = new("Root");
        GameObject child = new("Child");
        child.transform.SetParent(root.transform);
        MeshRenderer rootRenderer = root.AddComponent<MeshRenderer>();
        MeshRenderer childRenderer = child.AddComponent<MeshRenderer>();
        rootRenderer.sortingOrder = 3;
        childRenderer.sortingOrder = -2;

        try
        {
            TestVfxWorkbenchUtility.ApplyDirectRendererSorting(
                root,
                sortingLayerName: "Unit",
                sortingWorldY: 1.25f,
                yMultiplier: 100f,
                sortingOrderOffset: 7);

            int baseOrder = BattleWorldVfxSortUtility.CalculateSortingOrder(1.25f, 100f, 7);
            Assert.That(baseOrder, Is.EqualTo(-118));
            Assert.That(rootRenderer.sortingLayerName, Is.EqualTo("Unit"));
            Assert.That(childRenderer.sortingLayerName, Is.EqualTo("Unit"));
            Assert.That(rootRenderer.sortingOrder, Is.EqualTo(baseOrder + 3));
            Assert.That(childRenderer.sortingOrder, Is.EqualTo(baseOrder - 2));
        }
        finally
        {
            DestroyObject(root);
        }
    }

    [Test]
    public void WrapIndex_WrapsShortcutSelectionIndexes()
    {
        Assert.That(TestVfxWorkbenchUtility.WrapIndex(-1, 3), Is.EqualTo(2));
        Assert.That(TestVfxWorkbenchUtility.WrapIndex(3, 3), Is.EqualTo(0));
        Assert.That(TestVfxWorkbenchUtility.WrapIndex(1, 3), Is.EqualTo(1));
        Assert.That(TestVfxWorkbenchUtility.WrapIndex(1, 0), Is.EqualTo(0));
    }

    [Test]
    public void NextEnumValue_CyclesRenderModeForward()
    {
        BattleVfxRenderMode next =
            TestVfxWorkbenchUtility.NextEnumValue(BattleVfxRenderMode.IndividualWorldRenderTexture);

        Assert.That(next, Is.EqualTo(BattleVfxRenderMode.SharedRenderTextureOverlay));
        Assert.That(
            TestVfxWorkbenchUtility.NextEnumValue(BattleVfxRenderMode.DirectWorldRenderer),
            Is.EqualTo(BattleVfxRenderMode.IndividualWorldRenderTexture));
    }

    [Test]
    public void PreviousEnumValue_CyclesFlipTypeBackward()
    {
        VfxFlipType previous = TestVfxWorkbenchUtility.PreviousEnumValue(VfxFlipType.RotationY180);

        Assert.That(previous, Is.EqualTo(VfxFlipType.None));
    }

    [Test]
    public void RebuildFilteredLabelIndexes_TrimsCaseInsensitiveSearchAndLimitsVisibleResults()
    {
        List<string> labels = new()
        {
            "SpriteAni/Light/Vfx_SpriteAni_flash_explosion",
            "SpriteAni/Arrow/Vfx_SpriteAni_Arrow",
            "Legacy/VFX_FLASH_Burst"
        };
        List<int> resultIndexes = new();

        int totalMatches = TestVfxWorkbenchUtility.RebuildFilteredLabelIndexes(
            labels,
            " flash ",
            resultIndexes,
            1);

        Assert.That(totalMatches, Is.EqualTo(2));
        Assert.That(resultIndexes, Is.EqualTo(new[] { 0 }));
    }

    [Test]
    public void RebuildFilteredLabelIndexes_EmptySearchKeepsSourceOrderAndReportsTotalMatches()
    {
        List<string> labels = new()
        {
            "A",
            "B",
            "C"
        };
        List<int> resultIndexes = new();

        int totalMatches = TestVfxWorkbenchUtility.RebuildFilteredLabelIndexes(
            labels,
            "",
            resultIndexes,
            2);

        Assert.That(totalMatches, Is.EqualTo(3));
        Assert.That(resultIndexes, Is.EqualTo(new[] { 0, 1 }));
    }

    private static void DestroyObject(Object target)
    {
        if (target == null)
            return;

        if (Application.isPlaying)
            Object.Destroy(target);
        else
            Object.DestroyImmediate(target);
    }
}
