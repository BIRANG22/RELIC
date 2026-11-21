using NUnit.Framework;
using UnityEngine;

public class GridEffectHoverTargetTests
{
    private GameObject target;

    [TearDown]
    public void TearDown()
    {
        if (target != null)
            Object.DestroyImmediate(target);
    }

    [Test]
    public void Attach_DisabledPreviewCollider_CreatesEnabledFallbackWithoutReenablingOriginal()
    {
        target = new GameObject("GridEffectPreview");
        BoxCollider2D original = target.AddComponent<BoxCollider2D>();
        original.size = new Vector2(5f, 3f);
        original.enabled = false;

        GridEffectHoverTarget.Attach(target, "GR_Stigma", new Vector2(1f, 1f));

        BoxCollider2D[] colliders = target.GetComponentsInChildren<BoxCollider2D>(true);
        Assert.That(original.enabled, Is.False);
        Assert.That(colliders, Has.Length.EqualTo(2));
        Assert.That(colliders[1].enabled, Is.True);
        Assert.That(colliders[1].isTrigger, Is.True);
        Assert.That(colliders[1].size, Is.EqualTo(new Vector2(1f, 1f)));
    }
}
