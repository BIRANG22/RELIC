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
    public void CalculateCanvasViewportLayout_WideResize_ScalesTargetScreenIntoViewport()
    {
        Rect viewport = ResolutionManager.CalculateLetterboxRect(3440, 1080, 1920, 1080);
        ResolutionCanvasViewportLayout layout = ResolutionManager.CalculateCanvasViewportLayout(
            new Vector2(1920f, 603f),
            viewport,
            1920,
            1080);

        Assert.That(layout.Size, Is.EqualTo(new Vector2(1920f, 1080f)));
        Assert.That(layout.Scale, Is.EqualTo(0.558f).Within(0.001f));
        Assert.That(layout.Size.x * layout.Scale, Is.EqualTo(viewport.width * 1920f).Within(0.001f));
        Assert.That(layout.Size.y * layout.Scale, Is.LessThanOrEqualTo(viewport.height * 603f + 0.001f));
    }

    [Test]
    public void CalculateCanvasViewportLayout_NarrowResize_ScalesTargetScreenIntoViewport()
    {
        Rect viewport = ResolutionManager.CalculateLetterboxRect(1024, 768, 1920, 1080);
        ResolutionCanvasViewportLayout layout = ResolutionManager.CalculateCanvasViewportLayout(
            new Vector2(1024f, 768f),
            viewport,
            1920,
            1080);

        Assert.That(layout.Size, Is.EqualTo(new Vector2(1920f, 1080f)));
        Assert.That(layout.Scale, Is.EqualTo(0.533f).Within(0.001f));
        Assert.That(layout.Size.x * layout.Scale, Is.EqualTo(viewport.width * 1024f).Within(0.001f));
        Assert.That(layout.Size.y * layout.Scale, Is.EqualTo(viewport.height * 768f).Within(0.001f));
    }

    [Test]
    public void PlayerSettings_EnableResizableWindowForLetterboxedResize()
    {
        string projectSettings = File.ReadAllText("ProjectSettings/ProjectSettings.asset");

        Assert.That(projectSettings, Does.Contain("resizableWindow: 1"));
    }
}
