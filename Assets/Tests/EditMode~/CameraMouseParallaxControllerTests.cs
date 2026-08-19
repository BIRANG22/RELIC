using NUnit.Framework;
using UnityEngine;

public sealed class CameraMouseParallaxControllerTests
{
    [Test]
    public void NormalizeMousePositionForParallax_ReturnsZeroAtCenter()
    {
        Vector2 normalized = CameraMouseParallaxController.NormalizeMousePositionForParallax(
            new Vector2(960f, 540f),
            new Vector2(1920f, 1080f));

        Assert.That(normalized.x, Is.EqualTo(0f).Within(0.0001f));
        Assert.That(normalized.y, Is.EqualTo(0f).Within(0.0001f));
    }

    [Test]
    public void NormalizeMousePositionForParallax_ClampsOutsideScreen()
    {
        Vector2 normalized = CameraMouseParallaxController.NormalizeMousePositionForParallax(
            new Vector2(-100f, 1600f),
            new Vector2(1920f, 1080f));

        Assert.That(normalized.x, Is.EqualTo(-1f).Within(0.0001f));
        Assert.That(normalized.y, Is.EqualTo(1f).Within(0.0001f));
    }

    [Test]
    public void CalculateMouseParallaxPositionOffset_UsesBattleSceneTunedAmounts()
    {
        Vector3 offset = CameraMouseParallaxController.CalculateMouseParallaxPositionOffset(
            new Vector2(0.5f, -0.25f),
            new Vector2(0.08f, 0.03f),
            1f);

        Assert.That(offset.x, Is.EqualTo(0.04f).Within(0.0001f));
        Assert.That(offset.y, Is.EqualTo(-0.0075f).Within(0.0001f));
        Assert.That(offset.z, Is.EqualTo(0f).Within(0.0001f));
    }

    [Test]
    public void CalculateMouseParallaxEulerOffset_UsesBattleSceneTunedAmounts()
    {
        Vector3 eulerOffset = CameraMouseParallaxController.CalculateMouseParallaxEulerOffset(
            new Vector2(0.5f, -0.25f),
            new Vector2(3f, 1.5f),
            1f);

        Assert.That(eulerOffset.x, Is.EqualTo(0.75f).Within(0.0001f));
        Assert.That(eulerOffset.y, Is.EqualTo(0.75f).Within(0.0001f));
        Assert.That(eulerOffset.z, Is.EqualTo(0f).Within(0.0001f));
    }

    [Test]
    public void GetMouseParallaxIntensityMultiplier_ReducesWhenCameraMoved()
    {
        float multiplier = CameraMouseParallaxController.GetMouseParallaxIntensityMultiplier(
            true,
            0.35f);

        Assert.That(multiplier, Is.EqualTo(0.35f).Within(0.0001f));
    }
}
