using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class MapNodeViewCheckImageTests
{
    [Test]
    public void NodePrefab_CreatesCheckAnimationImageAtFortyPercentSize()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Project/PrefabsR/Map/NodePrefab.prefab");
        Assert.That(prefab, Is.Not.Null);

        GameObject instance = Object.Instantiate(prefab);

        try
        {
            MapNodeView nodeView = instance.GetComponent<MapNodeView>();
            MethodInfo ensure = typeof(MapNodeView).GetMethod(
                "EnsureCheckAnimationImage",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(ensure, Is.Not.Null);
            ensure.Invoke(nodeView, null);

            RectTransform checkImage = instance.transform
                .Find("CheckAnimationImage") as RectTransform;
            Assert.That(checkImage, Is.Not.Null);
            Assert.That(checkImage.sizeDelta.x, Is.EqualTo(38.4f).Within(0.001f));
            Assert.That(checkImage.sizeDelta.y, Is.EqualTo(38.4f).Within(0.001f));
        }
        finally
        {
            Object.DestroyImmediate(instance);
        }
    }
}
