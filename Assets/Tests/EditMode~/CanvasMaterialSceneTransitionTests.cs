using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

public class CanvasMaterialSceneTransitionTests
{
    [Test]
    public void SetClosedImmediate_ConfiguresReferenceResolutionScalerForRuntimeCanvas()
    {
        GameObject transitionObject = new("SceneTransitionManager", typeof(RectTransform));

        try
        {
            CanvasMaterialSceneTransition transition = transitionObject.AddComponent<CanvasMaterialSceneTransition>();

            transition.SetClosedImmediate();

            Canvas canvas = transitionObject.GetComponent<Canvas>();
            CanvasScaler scaler = transitionObject.GetComponent<CanvasScaler>();

            Assert.That(canvas, Is.Not.Null);
            Assert.That(canvas.renderMode, Is.EqualTo(RenderMode.ScreenSpaceOverlay));
            Assert.That(scaler, Is.Not.Null);
            Assert.That(scaler.uiScaleMode, Is.EqualTo(CanvasScaler.ScaleMode.ScaleWithScreenSize));
            Assert.That(scaler.screenMatchMode, Is.EqualTo(CanvasScaler.ScreenMatchMode.MatchWidthOrHeight));
            Assert.That(scaler.referenceResolution, Is.EqualTo(new Vector2(1920f, 1080f)));
            Assert.That(scaler.matchWidthOrHeight, Is.EqualTo(0f));
        }
        finally
        {
            Object.DestroyImmediate(transitionObject);
        }
    }
}
