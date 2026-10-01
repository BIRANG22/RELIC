using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public sealed class ScreenSpaceTransferOrbEffectTests
{
    private const string PrefabPath = "Assets/Project/PrefabsR/ScreenSpaceTransferOrbEffect.prefab";

    [Test]
    public void TransferPrefab_RendersAboveModalCanvas()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);

        Assert.That(prefab, Is.Not.Null);
        Assert.That(prefab.GetComponent<ScreenSpaceTransferOrbEffect>(), Is.Not.Null);

        Canvas canvas = prefab.GetComponent<Canvas>();
        Assert.That(canvas, Is.Not.Null);
        Assert.That(canvas.overrideSorting, Is.True);
        Assert.That(canvas.sortingOrder, Is.GreaterThan(10001));

        RawImage orb = prefab.GetComponentInChildren<RawImage>(true);
        Assert.That(orb, Is.Not.Null);
        Assert.That(orb.rectTransform.sizeDelta, Is.EqualTo(new Vector2(120f, 120f)));
    }

    [Test]
    public void EvaluateQuadraticBezier_UsesArcControlPoint()
    {
        Vector2 start = Vector2.zero;
        Vector2 control = new(5f, 10f);
        Vector2 end = new(10f, 0f);

        Vector2 midpoint = ScreenSpaceTransferOrbEffect.EvaluateQuadraticBezier(
            start,
            control,
            end,
            0.5f);

        Assert.That(midpoint.x, Is.EqualTo(5f).Within(0.0001f));
        Assert.That(midpoint.y, Is.EqualTo(5f).Within(0.0001f));
    }

    [Test]
    public void EvaluateQuadraticBezier_UsesFixedScreenEndpoints()
    {
        Vector2 startScreen = new(230f, 410f);
        Vector2 endScreen = new(1540f, 760f);

        Vector2 start = ScreenSpaceTransferOrbEffect.EvaluateQuadraticBezier(
            startScreen,
            new(885f, 805f),
            endScreen,
            0f);
        Vector2 end = ScreenSpaceTransferOrbEffect.EvaluateQuadraticBezier(
            startScreen,
            new(885f, 805f),
            endScreen,
            1f);

        Assert.That(start, Is.EqualTo(startScreen));
        Assert.That(end, Is.EqualTo(endScreen));
    }

    [Test]
    public void ResolveUiCamera_UsesNearestWorldSpaceCanvasCamera()
    {
        GameObject root = new("RootCanvas", typeof(RectTransform), typeof(Canvas));
        GameObject nested = new("NestedCanvas", typeof(RectTransform), typeof(Canvas));
        GameObject cameraObject = new("FallbackCamera", typeof(Camera));
        try
        {
            Canvas rootCanvas = root.GetComponent<Canvas>();
            rootCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            nested.transform.SetParent(root.transform, false);
            nested.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;

            Camera resolved = ScreenSpaceTransferOrbEffect.ResolveUiCamera(
                nested.transform as RectTransform,
                cameraObject.GetComponent<Camera>());

            Assert.That(resolved, Is.SameAs(cameraObject.GetComponent<Camera>()));
        }
        finally
        {
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(cameraObject);
        }
    }
}
