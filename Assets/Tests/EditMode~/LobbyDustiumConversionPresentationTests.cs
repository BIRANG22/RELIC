using NUnit.Framework;
using UnityEngine;

public sealed class LobbyDustiumConversionPresentationTests
{
    [TestCase(10, 0f, 10)]
    [TestCase(10, 0.5f, 5)]
    [TestCase(10, 1f, 0)]
    public void CalculateRemainingRedDustium_CountsDownWithProgress(
        int amount,
        float progress,
        int expected)
    {
        Assert.That(
            ResearchResultPanelUI.CalculateRemainingRedDustium(amount, progress),
            Is.EqualTo(expected));
    }

    [TestCase(20, 10, 0f, 20)]
    [TestCase(20, 10, 0.5f, 25)]
    [TestCase(20, 10, 1f, 30)]
    public void CalculateDisplayedBlueDustium_CountsUpWithoutChangingRuntime(
        int blueBefore,
        int converted,
        float progress,
        int expected)
    {
        Assert.That(
            ResearchResultPanelUI.CalculateDisplayedBlueDustium(
                blueBefore,
                converted,
                progress),
            Is.EqualTo(expected));
    }

    [Test]
    public void EvaluateTransferColor_ChangesFromRedToBlue()
    {
        Color red = new Color(1f, 0.1f, 0.1f, 1f);
        Color blue = new Color(0.1f, 0.4f, 1f, 1f);

        Assert.That(ResearchResultPanelUI.EvaluateTransferColor(red, blue, 0f), Is.EqualTo(red));
        Assert.That(ResearchResultPanelUI.EvaluateTransferColor(red, blue, 1f), Is.EqualTo(blue));
    }

    [TestCase(10, 0f, true)]
    [TestCase(10, 0.9f, true)]
    [TestCase(1, 0.6f, false)]
    [TestCase(10, 1f, false)]
    public void ShouldShowFixedRedDustium_HidesOnlyWhenConversionCompletes(
        int amount,
        float progress,
        bool expected)
    {
        Assert.That(
            ResearchResultPanelUI.ShouldShowFixedRedDustium(amount, progress),
            Is.EqualTo(expected));
    }
}
