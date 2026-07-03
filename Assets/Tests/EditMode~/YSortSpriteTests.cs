using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class YSortSpriteTests
{
    private GameObject rootObject;

    [TearDown]
    public void TearDown()
    {
        if (rootObject != null)
            Object.DestroyImmediate(rootObject);
    }

    [Test]
    public void IdleBackYSort_UsesSpriteRootOrderMinusOne()
    {
        rootObject = new GameObject("Character");
        rootObject.transform.position = Vector3.zero;

        GameObject spriteRootObject = new("SpriteRoot");
        spriteRootObject.transform.SetParent(rootObject.transform);
        spriteRootObject.transform.localPosition = Vector3.zero;
        SpriteRenderer spriteRootRenderer = spriteRootObject.AddComponent<SpriteRenderer>();
        YSortSprite spriteRootSorter = spriteRootObject.AddComponent<YSortSprite>();

        GameObject highlightObject = new("HighlightSprite");
        highlightObject.transform.SetParent(rootObject.transform);
        highlightObject.transform.localPosition = new Vector3(0f, -0.4f, 0f);

        GameObject idleBackObject = new("Idle_Back");
        idleBackObject.transform.SetParent(highlightObject.transform);
        idleBackObject.transform.localPosition = new Vector3(0f, 0.1f, 0f);
        SpriteRenderer idleBackRenderer = idleBackObject.AddComponent<SpriteRenderer>();
        YSortSprite idleBackSorter = idleBackObject.AddComponent<YSortSprite>();

        InvokeUnityMessage(spriteRootSorter, "Awake");
        InvokeUnityMessage(idleBackSorter, "Awake");
        InvokeUnityMessage(spriteRootSorter, "LateUpdate");
        InvokeUnityMessage(idleBackSorter, "LateUpdate");

        Assert.That(spriteRootRenderer.sortingOrder, Is.EqualTo(0));
        Assert.That(idleBackRenderer.sortingOrder, Is.EqualTo(spriteRootRenderer.sortingOrder - 1));
    }

    [Test]
    public void ParticleSystemRendererYSort_UsesCalculatedSortingOrder()
    {
        rootObject = new GameObject("GridEffectVfx");
        rootObject.transform.position = new Vector3(0f, 1.25f, 0f);

        rootObject.AddComponent<ParticleSystem>();
        ParticleSystemRenderer particleRenderer = rootObject.GetComponent<ParticleSystemRenderer>();
        particleRenderer.sortingOrder = 42;

        YSortSprite sorter = rootObject.AddComponent<YSortSprite>();
        sorter.sortingOrderOffset = 7;

        InvokeUnityMessage(sorter, "Awake");
        InvokeUnityMessage(sorter, "LateUpdate");

        Assert.That(particleRenderer.sortingOrder, Is.EqualTo(-118));
    }

    private static void InvokeUnityMessage(object target, string methodName)
    {
        MethodInfo method = target.GetType().GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(method, Is.Not.Null, $"Missing Unity message: {methodName}");
        method.Invoke(target, null);
    }
}
