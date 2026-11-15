using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class BattleTimelineReservedActionGrindTests
{
    private const BindingFlags StaticPrivate = BindingFlags.Static | BindingFlags.NonPublic;

    [Test]
    public void CalculateGrindPosition_AtCompletion_MovesLeftWithoutVerticalOffset()
    {
        MethodInfo method = typeof(BattleTimelineSpriteAnimationController).GetMethod(
            "CalculateGrindPosition",
            StaticPrivate);

        Assert.That(method, Is.Not.Null);

        Vector2 result = (Vector2)method.Invoke(null, new object[]
        {
            new Vector2(120f, 35f),
            70f,
            1f
        });

        Assert.That(result.x, Is.EqualTo(50f).Within(0.001f));
        Assert.That(result.y, Is.EqualTo(35f).Within(0.001f));
    }

    [Test]
    public void CalculateGrindScale_AtCompletion_CompressesOnlyWidth()
    {
        MethodInfo method = typeof(BattleTimelineSpriteAnimationController).GetMethod(
            "CalculateGrindScale",
            StaticPrivate);

        Assert.That(method, Is.Not.Null);

        Vector3 result = (Vector3)method.Invoke(null, new object[]
        {
            new Vector3(2f, 3f, 1f),
            0.1f,
            1f
        });

        Assert.That(result.x, Is.EqualTo(0.2f).Within(0.001f));
        Assert.That(result.y, Is.EqualTo(3f).Within(0.001f));
        Assert.That(result.z, Is.EqualTo(1f).Within(0.001f));
    }

    [Test]
    public void GrindCalculations_ClampProgressToValidRange()
    {
        MethodInfo positionMethod = typeof(BattleTimelineSpriteAnimationController).GetMethod(
            "CalculateGrindPosition",
            StaticPrivate);
        MethodInfo scaleMethod = typeof(BattleTimelineSpriteAnimationController).GetMethod(
            "CalculateGrindScale",
            StaticPrivate);

        Assert.That(positionMethod, Is.Not.Null);
        Assert.That(scaleMethod, Is.Not.Null);

        Vector2 position = (Vector2)positionMethod.Invoke(null, new object[]
        {
            Vector2.zero,
            40f,
            2f
        });
        Vector3 scale = (Vector3)scaleMethod.Invoke(null, new object[]
        {
            Vector3.one,
            0.25f,
            -1f
        });

        Assert.That(position, Is.EqualTo(Vector2.left * 40f));
        Assert.That(scale, Is.EqualTo(Vector3.one));
    }
}
