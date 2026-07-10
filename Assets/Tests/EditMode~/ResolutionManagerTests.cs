using NUnit.Framework;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

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
    public void ResolutionCanvasViewportFitter_PreservesDirectChildOrder_WhenMovingIntoViewport()
    {
        GameObject canvasObject = new("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));

        try
        {
            CreateRectChild(canvasObject.transform, "Background");
            CreateRectChild(canvasObject.transform, "Hud");
            CreateRectChild(canvasObject.transform, "Popup");

            ResolutionCanvasViewportFitter fitter = canvasObject.AddComponent<ResolutionCanvasViewportFitter>();
            fitter.Apply(new Rect(0f, 0f, 1f, 1f), 1920, 1080);

            Transform viewport = canvasObject.transform.Find("Resolution Viewport");

            Assert.That(viewport, Is.Not.Null);
            Assert.That(viewport.GetChild(0).name, Is.EqualTo("Background"));
            Assert.That(viewport.GetChild(1).name, Is.EqualTo("Hud"));
            Assert.That(viewport.GetChild(2).name, Is.EqualTo("Popup"));
        }
        finally
        {
            Object.DestroyImmediate(canvasObject);
        }
    }

    [Test]
    public void PlayerSettings_EnableResizableWindowForLetterboxedResize()
    {
        string projectSettings = File.ReadAllText("ProjectSettings/ProjectSettings.asset");

        Assert.That(projectSettings, Does.Contain("resizableWindow: 1"));
    }

    private static RectTransform CreateRectChild(Transform parent, string name)
    {
        GameObject childObject = new(name, typeof(RectTransform));
        childObject.transform.SetParent(parent, false);
        return childObject.GetComponent<RectTransform>();
    }
}
