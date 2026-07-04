using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class GridEffectPrefabConfigurationTests
{
    private const string PoisonGridEffectPrefabPath =
        "Assets/Project/Art/VFX/Grid_object/Vfx_Gr_poisson.prefab";

    [Test]
    public void PoisonGridEffectPrefab_UsesWorldVfxPresenterForInternalVfx()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            PoisonGridEffectPrefabPath);

        Assert.That(prefab, Is.Not.Null);

        GridEffectWorldVfxPresenter presenter =
            prefab.GetComponent<GridEffectWorldVfxPresenter>();

        Assert.That(presenter, Is.Not.Null);

        GameObject bubbles = prefab.transform.Find("Bubbles")?.gameObject;

        Assert.That(bubbles, Is.Not.Null);
        Assert.That(bubbles.layer, Is.EqualTo(LayerMask.NameToLayer("VFX")));
    }
}
