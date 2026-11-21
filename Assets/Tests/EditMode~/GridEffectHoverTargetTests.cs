using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class GridEffectHoverTargetTests
{
    private GameObject target;
    private GameObject otherTarget;

    [TearDown]
    public void TearDown()
    {
        if (target != null)
            Object.DestroyImmediate(target);

        if (otherTarget != null)
            Object.DestroyImmediate(otherTarget);
    }

    [Test]
    public void Attach_DisabledPreviewCollider_ReenablesOriginalAsHoverTrigger()
    {
        target = new GameObject("GridEffectPreview");
        BoxCollider2D original = target.AddComponent<BoxCollider2D>();
        original.size = new Vector2(5f, 3f);
        original.enabled = false;

        GridEffectHoverTarget.Attach(target, "GR_Stigma", new Vector2(1f, 1f));

        BoxCollider2D[] colliders = target.GetComponentsInChildren<BoxCollider2D>(true);
        Assert.That(colliders, Has.Length.EqualTo(1));
        Assert.That(original.enabled, Is.True);
        Assert.That(original.isTrigger, Is.True);
        Assert.That(original.size, Is.EqualTo(new Vector2(5f, 3f)));
    }

    [Test]
    public void PreferredHoveredTarget_LatestEntryExits_RestoresPreviousTarget()
    {
        target = new GameObject("Stigma");
        otherTarget = new GameObject("SpiderWeb");
        GridEffectHoverTarget stigma = target.AddComponent<GridEffectHoverTarget>();
        GridEffectHoverTarget spiderWeb = otherTarget.AddComponent<GridEffectHoverTarget>();

        MethodInfo setHovered = typeof(GridEffectHoverTarget).GetMethod(
            "SetHovered",
            BindingFlags.Instance | BindingFlags.NonPublic);
        MethodInfo getPreferred = typeof(GridEffectHoverTarget).GetMethod(
            "GetPreferredHoveredTarget",
            BindingFlags.Static | BindingFlags.NonPublic);

        Assert.That(setHovered, Is.Not.Null);
        Assert.That(getPreferred, Is.Not.Null);

        setHovered.Invoke(stigma, new object[] { true });
        setHovered.Invoke(spiderWeb, new object[] { true });
        Assert.That(getPreferred.Invoke(null, null), Is.SameAs(spiderWeb));

        setHovered.Invoke(spiderWeb, new object[] { false });
        Assert.That(getPreferred.Invoke(null, null), Is.SameAs(stigma));
    }
}
