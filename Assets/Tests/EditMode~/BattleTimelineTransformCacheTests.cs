using NUnit.Framework;
using Relic.Gameplay.Battle;
using TMPro;
using UnityEngine;

public sealed class BattleTimelineTransformCacheTests
{
    private GameObject root;

    [TearDown]
    public void TearDown()
    {
        if (root != null)
            Object.DestroyImmediate(root);
    }

    [Test]
    public void GetSizeRoot_AfterFirstLookup_ReturnsCachedTransformWithoutNameSearch()
    {
        root = new GameObject("Timeline");
        Transform order = AddChild(root.transform, "Order01");
        Transform sizeRoot = AddChild(order, "Use_skill2");
        BattleTimelineTransformCache cache = new(root.transform);

        Assert.That(cache.GetSizeRoot(0, 2), Is.SameAs(sizeRoot));
        sizeRoot.name = "Renamed";

        Assert.That(cache.GetSizeRoot(0, 2), Is.SameAs(sizeRoot));
    }

    [Test]
    public void GetValueText_AfterCachedTextDestroyed_FindsReplacement()
    {
        root = new GameObject("Timeline");
        Transform sizeRoot = AddChild(root.transform, "Use_skill1");
        GameObject originalObject = new("Value");
        originalObject.transform.SetParent(sizeRoot, false);
        TMP_Text original = originalObject.AddComponent<TextMeshProUGUI>();
        BattleTimelineTransformCache cache = new(root.transform);

        Assert.That(cache.GetValueText(sizeRoot), Is.SameAs(original));
        Object.DestroyImmediate(originalObject);
        GameObject replacementObject = new("Value");
        replacementObject.transform.SetParent(sizeRoot, false);
        TMP_Text replacement = replacementObject.AddComponent<TextMeshProUGUI>();

        Assert.That(cache.GetValueText(sizeRoot), Is.SameAs(replacement));
    }

    private static Transform AddChild(Transform parent, string childName)
    {
        GameObject child = new(childName);
        child.transform.SetParent(parent, false);
        return child.transform;
    }
}
