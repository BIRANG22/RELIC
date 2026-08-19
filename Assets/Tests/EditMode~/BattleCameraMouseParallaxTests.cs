using NUnit.Framework;
using UnityEngine;

public class BattleCameraMouseParallaxTests
{
    [Test]
    public void NormalizeMousePositionForParallax_ReturnsZeroAtScreenCenter()
    {
        Vector2 normalized = BattleCameraController.NormalizeMousePositionForParallax(
            new Vector2(960f, 540f),
            new Vector2(1920f, 1080f));

        Assert.That(normalized.x, Is.EqualTo(0f).Within(0.0001f));
        Assert.That(normalized.y, Is.EqualTo(0f).Within(0.0001f));
    }

    [Test]
    public void NormalizeMousePositionForParallax_ClampsScreenEdgesToUnitRange()
    {
        Vector2 normalized = BattleCameraController.NormalizeMousePositionForParallax(
            new Vector2(2400f, -200f),
            new Vector2(1920f, 1080f));

        Assert.That(normalized.x, Is.EqualTo(1f).Within(0.0001f));
        Assert.That(normalized.y, Is.EqualTo(-1f).Within(0.0001f));
    }

    [Test]
    public void CalculateMouseParallaxPositionOffset_UsesConfiguredAxisAmounts()
    {
        Vector3 offset = BattleCameraController.CalculateMouseParallaxPositionOffset(
            new Vector2(0.5f, -0.25f),
            new Vector2(0.08f, 0.05f),
            0.5f);

        Assert.That(offset.x, Is.EqualTo(0.02f).Within(0.0001f));
        Assert.That(offset.y, Is.EqualTo(-0.00625f).Within(0.0001f));
        Assert.That(offset.z, Is.EqualTo(0f).Within(0.0001f));
    }

    [Test]
    public void CalculateMouseParallaxEulerOffset_TiltsTowardMouseDirection()
    {
        Vector3 eulerOffset = BattleCameraController.CalculateMouseParallaxEulerOffset(
            new Vector2(0.5f, -0.25f),
            new Vector2(1.2f, 0.8f),
            0.5f);

        Assert.That(eulerOffset.x, Is.EqualTo(0.15f).Within(0.0001f));
        Assert.That(eulerOffset.y, Is.EqualTo(0.2f).Within(0.0001f));
        Assert.That(eulerOffset.z, Is.EqualTo(0f).Within(0.0001f));
    }
}
