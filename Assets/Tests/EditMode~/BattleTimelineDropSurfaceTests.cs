using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

public class BattleTimelineDropSurfaceTests
{
    [Test]
    public void Awake_WhenTimelineRootHasNoGraphic_AddsTransparentRaycastSurface()
    {
        GameObject root = new("TimelineSlot", typeof(RectTransform));

        try
        {
            root.AddComponent<BattleTimelineGroupUI>();

            Graphic surface = root.GetComponent<Graphic>();
            Assert.That(surface, Is.Not.Null);
            Assert.That(surface.raycastTarget, Is.True);
            Assert.That(surface.color.a, Is.Zero);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }
}
