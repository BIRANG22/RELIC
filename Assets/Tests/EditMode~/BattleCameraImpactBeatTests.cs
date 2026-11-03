using NUnit.Framework;
using UnityEngine;

public sealed class BattleCameraImpactBeatTests
{
    [TestCase(-8, -1)]
    [TestCase(-1, -1)]
    [TestCase(0, 1)]
    [TestCase(1, 1)]
    [TestCase(9, 1)]
    public void NormalizeImpactDirection_ReturnsStableHorizontalSign(int direction, int expected)
    {
        Assert.That(BattleCameraController.NormalizeImpactDirection(direction), Is.EqualTo(expected));
    }

    [Test]
    public void CalculateImpactBeatOffset_UsesDirectionAndClampsNegativeStrength()
    {
        Assert.That(
            BattleCameraController.CalculateImpactBeatOffset(1, 0.2f),
            Is.EqualTo(new Vector3(0.2f, 0f, 0f)));
        Assert.That(
            BattleCameraController.CalculateImpactBeatOffset(-1, 0.2f),
            Is.EqualTo(new Vector3(-0.2f, 0f, 0f)));
        Assert.That(
            BattleCameraController.CalculateImpactBeatOffset(-1, -0.2f),
            Is.EqualTo(Vector3.zero));
    }

    [Test]
    public void CalculateDirectionalImpactBeats_CompressesWithHitAndRecoilsAgainstIt()
    {
        Vector3 compression = BattleCameraController.CalculateImpactBeatOffset(-1, 0.08f);
        Vector3 recoil = BattleCameraController.CalculateImpactBeatOffset(-1, 0.16f, true);

        Assert.That(compression.x, Is.LessThan(0f));
        Assert.That(recoil.x, Is.GreaterThan(0f));
    }

    [Test]
    public void CalculateImpactDepthBeat_ClampsDistanceAndSupportsOppositeRecoil()
    {
        Assert.That(BattleCameraController.CalculateImpactDepthBeat(-20f, 0.5f, false), Is.EqualTo(-19.5f));
        Assert.That(BattleCameraController.CalculateImpactDepthBeat(-20f, 0.25f, true), Is.EqualTo(-20.25f));
        Assert.That(BattleCameraController.CalculateImpactDepthBeat(-20f, -1f, false), Is.EqualTo(-20f));
    }
}
