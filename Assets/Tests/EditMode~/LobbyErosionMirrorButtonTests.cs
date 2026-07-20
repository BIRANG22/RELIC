using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

public class LobbyErosionMirrorButtonTests
{
    [Test]
    public void OpenErosionSelectPanel_ShowsModalOverlayUnderPositionPanel()
    {
        GameObject canvasRoot = new("CanvasRoot", typeof(RectTransform));
        GameObject positionPanel = new("PositionPanel", typeof(RectTransform));
        GameObject lobbyMainPanel = new("LobbyMainPanel", typeof(RectTransform));
        GameObject erosionSelectPanel = new("ErosionSelectPanel", typeof(RectTransform));
        GameObject mirror = new("Mirror", typeof(SpriteRenderer));

        try
        {
            positionPanel.transform.SetParent(canvasRoot.transform, false);
            lobbyMainPanel.transform.SetParent(canvasRoot.transform, false);
            erosionSelectPanel.transform.SetParent(lobbyMainPanel.transform, false);
            lobbyMainPanel.SetActive(false);

            LobbyErosionMirrorButton button = mirror.AddComponent<LobbyErosionMirrorButton>();
            SetPrivateField(button, "positionPanel", positionPanel.GetComponent<RectTransform>());
            SetPrivateField(button, "erosionSelectPanel", erosionSelectPanel);

            button.OpenErosionSelectPanel();

            Transform overlay = erosionSelectPanel.transform.parent;
            Assert.That(erosionSelectPanel.activeSelf, Is.True);
            Assert.That(erosionSelectPanel.activeInHierarchy, Is.True);
            Assert.That(overlay.name, Is.EqualTo("ErosionSelectOverlay"));
            Assert.That(overlay.parent, Is.EqualTo(positionPanel.transform));
            Assert.That(overlay.gameObject.activeSelf, Is.True);
            Assert.That(overlay.GetComponent<Image>().raycastTarget, Is.True);
            Assert.That(LobbyPositionModalInputBlocker.IsBlocked, Is.True);
            Assert.That(lobbyMainPanel.activeSelf, Is.False);

            Button closeButton = erosionSelectPanel.transform
                .Find("CloseButton")
                .GetComponent<Button>();

            closeButton.onClick.Invoke();

            Assert.That(overlay.gameObject.activeSelf, Is.False);
            Assert.That(erosionSelectPanel.activeSelf, Is.False);
            Assert.That(LobbyPositionModalInputBlocker.IsBlocked, Is.False);
        }
        finally
        {
            LobbyPositionModalInputBlocker.Unblock(null);
            Object.DestroyImmediate(mirror);
            Object.DestroyImmediate(erosionSelectPanel);
            Object.DestroyImmediate(lobbyMainPanel);
            Object.DestroyImmediate(positionPanel);
            Object.DestroyImmediate(canvasRoot);
        }
    }

    [Test]
    public void Awake_AddsColliderForSpriteRendererClickTarget()
    {
        GameObject mirror = new("Mirror", typeof(SpriteRenderer));

        try
        {
            mirror.AddComponent<LobbyErosionMirrorButton>();

            Assert.That(mirror.GetComponent<Collider2D>(), Is.Not.Null);
        }
        finally
        {
            Object.DestroyImmediate(mirror);
        }
    }

    [Test]
    public void UIPanelButtonWorldSpriteClick_DoesNothing_WhenPositionModalBlocksInput()
    {
        GameObject worldButton = new("WorldButton", typeof(SpriteRenderer));
        GameObject panel = new("OtherPanel");
        panel.SetActive(false);

        try
        {
            UIPanelButton button = worldButton.AddComponent<UIPanelButton>();
            SetPrivateField(button, "panelToOpen", panel);

            LobbyPositionModalInputBlocker.Block(this);
            InvokeNonPublic(button, "OnMouseUpAsButton");

            Assert.That(panel.activeSelf, Is.False);
        }
        finally
        {
            LobbyPositionModalInputBlocker.Unblock(this);
            Object.DestroyImmediate(panel);
            Object.DestroyImmediate(worldButton);
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

    private static void InvokeNonPublic(object target, string methodName)
    {
        MethodInfo method = target.GetType().GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(method, Is.Not.Null, $"Missing method: {methodName}");
        method.Invoke(target, null);
    }
}
