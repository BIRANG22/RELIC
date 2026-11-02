using NUnit.Framework;
using UnityEngine;

public sealed class BattleRenderPrefabAuditTests
{
    [TestCase("Assets/Project/PrefabsR/Character/A/A_BattlePrefab.prefab")]
    [TestCase("Assets/Project/PrefabsR/Character/B/B_BattlePrefab.prefab")]
    [TestCase("Assets/Project/PrefabsR/Character/C/C_BattlePrefab.prefab")]
    [TestCase("Assets/Project/PrefabsR/Character/D/D_BattlePrefab.prefab")]
    [TestCase("Assets/Project/PrefabsR/Monster/Normal/01.Muck/Muck.prefab")]
    [TestCase("Assets/Project/PrefabsR/Monster/Normal/02.Blob/Blob.prefab")]
    [TestCase("Assets/Project/PrefabsR/Monster/Normal/03.Rancor/Rancor.prefab")]
    [TestCase("Assets/Project/PrefabsR/Monster/Normal/04.Blight/Blight.prefab")]
    [TestCase("Assets/Project/PrefabsR/Monster/Normal/05.Vespa/Vespa.prefab")]
    [TestCase("Assets/Project/PrefabsR/Monster/Normal/06.Sinder/Sinder.prefab")]
    [TestCase("Assets/Project/PrefabsR/Monster/Normal/07.Draugr/Draugr.prefab")]
    [TestCase("Assets/Project/PrefabsR/Monster/Normal/08.Barrow/Barrow.prefab")]
    [TestCase("Assets/Project/PrefabsR/Monster/Elite/01.Nocturne/Nocturne.prefab")]
    [TestCase("Assets/Project/PrefabsR/Monster/Elite/02.Ruke/Ruke.prefab")]
    [TestCase("Assets/Project/PrefabsR/Monster/Elite/03.Mort/Mort.prefab")]
    [TestCase("Assets/Project/PrefabsR/Monster/Boss/01.Arabella/Arabella.prefab")]
    public void BattlePrefab_ShadowUsesUnitSortingLayer(string assetPath)
    {
        GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        Assert.That(prefab, Is.Not.Null, assetPath);

        GameObject instance = Object.Instantiate(prefab);
        try
        {
            YSortSprite sorter = FindSpriteRootSorter(instance.transform);
            Assert.That(sorter, Is.Not.Null, assetPath);
            sorter.ApplySortingNow();

            Transform[] shadows = FindShadows(instance.transform);
            Assert.That(shadows, Is.Not.Empty, assetPath);
            foreach (Transform shadow in shadows)
            {
                SpriteRenderer renderer = shadow.GetComponent<SpriteRenderer>();
                Assert.That(renderer, Is.Not.Null, assetPath + ":" + shadow.name);
                Assert.That(renderer.sortingLayerName, Is.EqualTo("Unit"), assetPath + ":" + shadow.name);
                Assert.That(renderer.sortingOrder, Is.EqualTo(sorter.CalculateCurrentSortingOrder() - 1), assetPath + ":" + shadow.name);
            }
        }
        finally
        {
            Object.DestroyImmediate(instance);
        }
    }

    private static Transform[] FindShadows(Transform root)
    {
        var result = new System.Collections.Generic.List<Transform>();
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            if (child.name.StartsWith("Shadow", System.StringComparison.OrdinalIgnoreCase)) result.Add(child);
        return result.ToArray();
    }

    private static YSortSprite FindSpriteRootSorter(Transform root)
    {
        foreach (YSortSprite sorter in root.GetComponentsInChildren<YSortSprite>(true))
            if (sorter.name == "SpriteRoot") return sorter;
        return null;
    }
}
