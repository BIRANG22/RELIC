using NUnit.Framework;
using System.IO;
using UnityEngine;

public class ResolutionManagerTests
{
    [Test]
    public void SupportedResolutionLabels_MatchConfiguredOptions()
    {
        Assert.That(
            ResolutionManager.GetSupportedResolutionLabels(),
            Is.EqualTo(new[]
            {
                "1280 \u00D7 720",
                "1366 \u00D7 768",
                "1600 \u00D7 900",
                "1920 \u00D7 1080",
                "2560 \u00D7 1440",
                "3840 \u00D7 2160"
            }));
    }

    [Test]
    public void CalculateLetterboxRect_WideScreen_AddsSideBars()
    {
        Rect rect = ResolutionManager.CalculateLetterboxRect(3440, 1440, 1920, 1080);

        Assert.That(rect.y, Is.EqualTo(0f).Within(0.001f));
        Assert.That(rect.height, Is.EqualTo(1f).Within(0.001f));
        Assert.That(rect.width, Is.EqualTo(0.744f).Within(0.001f));
        Assert.That(rect.x, Is.EqualTo(0.128f).Within(0.001f));
    }

    [Test]
    public void CalculateLetterboxRect_NarrowScreen_AddsTopAndBottomBars()
    {
        Rect rect = ResolutionManager.CalculateLetterboxRect(1024, 768, 1920, 1080);

        Assert.That(rect.x, Is.EqualTo(0f).Within(0.001f));
        Assert.That(rect.width, Is.EqualTo(1f).Within(0.001f));
        Assert.That(rect.height, Is.EqualTo(0.75f).Within(0.001f));
        Assert.That(rect.y, Is.EqualTo(0.125f).Within(0.001f));
    }

    [Test]
    public void PlayerSettings_EnableResizableWindowForLetterboxedResize()
    {
        string projectSettings = File.ReadAllText("ProjectSettings/ProjectSettings.asset");

        Assert.That(projectSettings, Does.Contain("resizableWindow: 1"));
    }
}
