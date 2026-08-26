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
    public void ResolutionRefreshStability_RequiresConsecutiveStableFrames()
    {
        ResolutionRefreshStability stability = default;

        Assert.That(stability.Observe(new Vector2Int(1280, 720), new Vector2(1280f, 720f), 2), Is.False);
        Assert.That(stability.Observe(new Vector2Int(1600, 900), new Vector2(1600f, 900f), 2), Is.False);
        Assert.That(stability.Observe(new Vector2Int(1600, 900), new Vector2(1600f, 900f), 2), Is.False);
        Assert.That(stability.Observe(new Vector2Int(1600, 900), new Vector2(1600f, 900f), 2), Is.True);
    }

    [Test]
    public void ResolutionRefreshStability_IgnoresInvalidCanvasSize()
    {
        ResolutionRefreshStability stability = default;

        Assert.That(stability.Observe(new Vector2Int(1920, 1080), Vector2.zero, 1), Is.False);
        Assert.That(stability.Observe(new Vector2Int(1920, 1080), new Vector2(1920f, 1080f), 1), Is.False);
        Assert.That(stability.Observe(new Vector2Int(1920, 1080), new Vector2(1920f, 1080f), 1), Is.True);
    }

    [Test]
    public void CalculateCanvasViewportLayout_InvalidCanvasSizePreservesTargetSize()
    {
        ResolutionCanvasViewportLayout layout = ResolutionManager.CalculateCanvasViewportLayout(
            Vector2.zero,
            new Rect(0f, 0f, 1f, 1f),
            1920,
            1080);

        Assert.That(layout.Position, Is.EqualTo(Vector2.zero));
        Assert.That(layout.Size, Is.EqualTo(new Vector2(1920f, 1080f)));
        Assert.That(layout.Scale, Is.EqualTo(1f));
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
    public void ResolutionCanvasViewportFitter_ExposesViewportContentRoot()
    {
        GameObject canvasObject = new("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));

        try
        {
            ResolutionCanvasViewportFitter fitter = canvasObject.AddComponent<ResolutionCanvasViewportFitter>();
            fitter.Apply(new Rect(0f, 0f, 1f, 1f), 1920, 1080);

            RectTransform contentRoot = ResolutionCanvasViewportFitter.ResolveContentRoot(canvasObject.transform);

            Assert.That(contentRoot, Is.Not.Null);
            Assert.That(contentRoot.name, Is.EqualTo("Resolution Viewport"));
            Assert.That(contentRoot.parent, Is.SameAs(canvasObject.transform));
        }
        finally
        {
            Object.DestroyImmediate(canvasObject);
        }
    }

    [Test]
    public void ResolutionCanvasViewportFitter_ConstantPixelCanvasUsesFullHdContentSize()
    {
        GameObject canvasObject = new("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));

        try
        {
            RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(1280f, 720f);

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.referenceResolution = new Vector2(800f, 600f);

            ResolutionCanvasViewportFitter fitter = canvasObject.AddComponent<ResolutionCanvasViewportFitter>();
            fitter.Apply(new Rect(0f, 0f, 1f, 1f), 1280, 720);

            Assert.That(fitter.ContentRoot.sizeDelta, Is.EqualTo(new Vector2(1920f, 1080f)));
        }
        finally
        {
            Object.DestroyImmediate(canvasObject);
        }
    }

    [Test]
    public void ShouldFitCanvas_IncludesScreenSpaceCameraCanvasOnPrimaryDisplay()
    {
        GameObject canvasObject = new("CameraCanvas", typeof(RectTransform), typeof(Canvas));

        try
        {
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.targetDisplay = 0;

            Assert.That(ResolutionManager.ShouldFitCanvasForResolution(canvas), Is.True);
        }
        finally
        {
            Object.DestroyImmediate(canvasObject);
        }
    }

    [Test]
    public void ShouldFitCanvas_IncludesWorldSpaceCanvasOnPrimaryDisplay()
    {
        GameObject canvasObject = new("WorldCanvas", typeof(RectTransform), typeof(Canvas));

        try
        {
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.targetDisplay = 0;

            Assert.That(ResolutionManager.ShouldFitCanvasForResolution(canvas), Is.True);
        }
        finally
        {
            Object.DestroyImmediate(canvasObject);
        }
    }

    [Test]
    public void ShouldFitCanvas_ExcludesCanvasWithResolutionFitOptOut()
    {
        GameObject canvasObject = new("IntroCanvas", typeof(RectTransform), typeof(Canvas));

        try
        {
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<ResolutionCanvasFitOptOut>();

            Assert.That(ResolutionManager.ShouldFitCanvasForResolution(canvas), Is.False);
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
