using NUnit.Framework;
using UnityEngine;

public class MapLineViewTests
{
    [Test]
    public void CalculateLayout_UsesDistanceInsetAndConnectionAngle()
    {
        MapLineLayout layout = MapLineView.CalculateLayout(
            Vector2.zero, new Vector2(100f, 40f), 13f, 20f);

        Assert.That(layout.AnchoredPosition, Is.EqualTo(new Vector2(50f, 20f)));
        Assert.That(layout.Size.x, Is.EqualTo(67.7033f).Within(0.001f));
        Assert.That(layout.Size.y, Is.EqualTo(13f));
        Assert.That(layout.RotationDegrees, Is.EqualTo(21.8014f).Within(0.001f));
    }

    [Test]
    public void CalculateLayout_HorizontalConnectionHasZeroRotation()
    {
        MapLineLayout layout = MapLineView.CalculateLayout(
            Vector2.zero, new Vector2(100f, 0f), 13f, 20f);

        Assert.That(layout.AnchoredPosition, Is.EqualTo(new Vector2(50f, 0f)));
        Assert.That(layout.Size, Is.EqualTo(new Vector2(60f, 13f)));
        Assert.That(layout.RotationDegrees, Is.Zero);
    }
}
