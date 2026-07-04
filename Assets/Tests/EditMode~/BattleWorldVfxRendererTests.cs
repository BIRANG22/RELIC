using System;
using NUnit.Framework;
using UnityEngine;

public class BattleWorldVfxRendererTests
{
    private const string RendererRootName = "__BattleWorldVfxRenderer";

    [TearDown]
    public void TearDown()
    {
        GameObject rendererRoot = GameObject.Find(RendererRootName);
        if (rendererRoot != null)
            DestroyObject(rendererRoot);
    }

    [Test]
    public void BattleVfxEntry_DefaultsToIndividualWorldRenderTexture()
    {
        BattleVfxEntry entry = new();

        Assert.That(entry.renderMode, Is.EqualTo(BattleVfxRenderMode.IndividualWorldRenderTexture));
        Assert.That(entry.renderTextureWidth, Is.GreaterThan(0));
        Assert.That(entry.renderTextureHeight, Is.GreaterThan(0));
        Assert.That(entry.proxySortingLayerName, Is.EqualTo("Unit"));
        Assert.That(entry.proxySortingWorldYOffset, Is.EqualTo(0f));
    }

    [Test]
    public void SortUtility_UsesSameNegativeYConventionAsUnitYSort()
    {
        int order = BattleWorldVfxSortUtility.CalculateSortingOrder(
            y: 1.25f,
            yMultiplier: 100f,
            offset: 7);

        Assert.That(order, Is.EqualTo(-118));
    }

    [Test]
    public void TrySpawnDetached_ReturnsFalseWhenVfxSetupThrows()
    {
        GameObject prefab = new("ThrowingVfxPrefab");

        try
        {
            BattleVfxEntry entry = new()
            {
                prefab = prefab
            };

            bool spawned = false;
            BattleWorldVfxHandle handle = null;

            Assert.DoesNotThrow(() =>
            {
                spawned = BattleWorldVfxRenderer.TrySpawnDetached(
                    entry,
                    Vector3.zero,
                    renderLayer: 0,
                    visibleLayer: 0,
                    lifeTime: 0.1f,
                    _ => throw new InvalidCastException("simulated broken VFX prefab"),
                    out handle);
            });

            Assert.That(spawned, Is.False);
            Assert.That(handle, Is.Null);
        }
        finally
        {
            DestroyObject(prefab);
        }
    }

    private static void DestroyObject(UnityEngine.Object target)
    {
        if (target == null)
            return;

        if (Application.isPlaying)
            UnityEngine.Object.Destroy(target);
        else
            UnityEngine.Object.DestroyImmediate(target);
    }
}
