using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using System.Collections.Generic;

public class IntroSequenceControllerCanvasTests
{
    [Test]
    public void ConfigureIntroCanvas_UsesScreenSpaceCameraAndEnablesPostProcessing()
    {
        GameObject canvasObject = new("IntroCanvas");
        GameObject cameraObject = new("IntroCamera");

        try
        {
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            Camera camera = cameraObject.AddComponent<Camera>();
            UniversalAdditionalCameraData cameraData =
                cameraObject.AddComponent<UniversalAdditionalCameraData>();
            cameraData.renderPostProcessing = false;

            IntroSequenceController.ConfigureIntroCanvasForTest(
                canvas,
                camera,
                useScreenSpaceCamera: true,
                enablePostProcessing: true,
                planeDistance: 25f,
                sortingOrder: 31000);

            Assert.That(canvas.renderMode, Is.EqualTo(RenderMode.ScreenSpaceCamera));
            Assert.That(canvas.worldCamera, Is.SameAs(camera));
            Assert.That(canvas.planeDistance, Is.EqualTo(25f).Within(0.001f));
            Assert.That(canvas.overrideSorting, Is.True);
            Assert.That(canvas.sortingOrder, Is.EqualTo(31000));
            Assert.That(cameraData.renderPostProcessing, Is.True);
        }
        finally
        {
            Object.DestroyImmediate(canvasObject);
            Object.DestroyImmediate(cameraObject);
        }
    }

    [Test]
    public void ConfigureIntroCanvas_CanKeepOverlayMode()
    {
        GameObject canvasObject = new("IntroCanvas");
        GameObject cameraObject = new("IntroCamera");

        try
        {
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            Camera camera = cameraObject.AddComponent<Camera>();

            IntroSequenceController.ConfigureIntroCanvasForTest(
                canvas,
                camera,
                useScreenSpaceCamera: false,
                enablePostProcessing: true,
                planeDistance: 25f,
                sortingOrder: 31000);

            Assert.That(canvas.renderMode, Is.EqualTo(RenderMode.ScreenSpaceOverlay));
            Assert.That(canvas.worldCamera, Is.Null);
            Assert.That(canvas.overrideSorting, Is.True);
            Assert.That(canvas.sortingOrder, Is.EqualTo(31000));
        }
        finally
        {
            Object.DestroyImmediate(canvasObject);
            Object.DestroyImmediate(cameraObject);
        }
    }

    [Test]
    public void SetCanvasesHiddenForIntro_RestoresOriginalEnabledStates()
    {
        GameObject activeObject = new("ActiveOverlay");
        GameObject disabledObject = new("DisabledOverlay");

        try
        {
            Canvas activeCanvas = activeObject.AddComponent<Canvas>();
            GraphicRaycaster activeRaycaster = activeObject.AddComponent<GraphicRaycaster>();
            Canvas disabledCanvas = disabledObject.AddComponent<Canvas>();
            GraphicRaycaster disabledRaycaster = disabledObject.AddComponent<GraphicRaycaster>();
            disabledCanvas.enabled = false;
            disabledRaycaster.enabled = false;

            Dictionary<Canvas, bool> enabledStates = new();
            Dictionary<GraphicRaycaster, bool> raycasterStates = new();
            GameObject[] targets = { activeObject, disabledObject };

            IntroSequenceController.SetCanvasesHiddenForIntroForTest(
                targets,
                enabledStates,
                raycasterStates,
                hidden: true);

            Assert.IsTrue(activeObject.activeSelf);
            Assert.IsTrue(disabledObject.activeSelf);
            Assert.IsFalse(activeCanvas.enabled);
            Assert.IsFalse(disabledCanvas.enabled);
            Assert.IsFalse(activeRaycaster.enabled);
            Assert.IsFalse(disabledRaycaster.enabled);

            IntroSequenceController.SetCanvasesHiddenForIntroForTest(
                targets,
                enabledStates,
                raycasterStates,
                hidden: false);

            Assert.IsTrue(activeCanvas.enabled);
            Assert.IsFalse(disabledCanvas.enabled);
            Assert.IsTrue(activeRaycaster.enabled);
            Assert.IsFalse(disabledRaycaster.enabled);
        }
        finally
        {
            Object.DestroyImmediate(activeObject);
            Object.DestroyImmediate(disabledObject);
        }
    }
}
