using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

public class PositionStageSelectControllerTests
{
    private GameObject canvasObject;
    private GameObject playObject;
    private GameObject panelObject;

    [TearDown]
    public void TearDown()
    {
        if (playObject != null)
            Object.DestroyImmediate(playObject);

        if (panelObject != null)
            Object.DestroyImmediate(panelObject);

        if (canvasObject != null)
            Object.DestroyImmediate(canvasObject);
    }

    [Test]
    public void OpenAndClose_ControlsOverlay_AndWorldSpriteHasCollider()
    {
        canvasObject = new GameObject(
            "PositionPanel",
            typeof(RectTransform),
            typeof(Canvas));
        panelObject = new GameObject("StageSelectPanel", typeof(RectTransform));
        playObject = new GameObject("Play", typeof(SpriteRenderer));
        PositionStageSelectController controller =
            playObject.AddComponent<PositionStageSelectController>();
        SetPrivateField(controller, "positionPanel", canvasObject.GetComponent<RectTransform>());
        SetPrivateField(controller, "stageSelectPanel", panelObject);

        controller.OpenStageSelect();

        Assert.That(playObject.GetComponent<Collider2D>(), Is.Not.Null);
        Assert.That(panelObject.activeSelf, Is.True);
        Assert.That(panelObject.transform.parent, Is.Not.EqualTo(null));
        Image blocker = panelObject.transform.parent.GetComponentInChildren<Image>(true);
        Assert.That(blocker, Is.Not.Null);
        Assert.That(blocker.raycastTarget, Is.True);

        controller.CloseStageSelect();
        Assert.That(panelObject.transform.parent.gameObject.activeSelf, Is.False);
    }

    [Test]
    public void OpenStageSelect_DoesNotOpen_WhenPositionModalBlocksInput()
    {
        canvasObject = new GameObject(
            "PositionPanel",
            typeof(RectTransform),
            typeof(Canvas));
        panelObject = new GameObject("StageSelectPanel", typeof(RectTransform));
        panelObject.SetActive(false);
        playObject = new GameObject("Play", typeof(SpriteRenderer));
        PositionStageSelectController controller =
            playObject.AddComponent<PositionStageSelectController>();
        SetPrivateField(controller, "positionPanel", canvasObject.GetComponent<RectTransform>());
        SetPrivateField(controller, "stageSelectPanel", panelObject);

        try
        {
            LobbyPositionModalInputBlocker.Block(controller);

            controller.OpenStageSelect();

            Assert.That(panelObject.activeSelf, Is.False);
            Assert.That(panelObject.transform.parent, Is.Null);
        }
        finally
        {
            LobbyPositionModalInputBlocker.Unblock(controller);
        }
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, $"Missing field: {fieldName}");
        field.SetValue(target, value);
    }
}
