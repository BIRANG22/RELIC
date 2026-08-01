using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class LobbyMainPanelKeyboardInputControllerTests
{
    [Test]
    public void EscapePriority_ClosesCultureTankPanelThroughPresenter()
    {
        GameObject keyboardObject = new("Keyboard");
        LobbyMainPanelKeyboardInputController keyboard =
            keyboardObject.AddComponent<LobbyMainPanelKeyboardInputController>();
        GameObject panel = new("CultureTankPanel");
        LobbyCultureTankPanelPresenter presenter =
            panel.AddComponent<LobbyCultureTankPanelPresenter>();
        panel.SetActive(true);

        try
        {
            SetField(keyboard, "cultureTankPanel", panel);
            SetField(keyboard, "cultureTankPanelPresenter", presenter);

            MethodInfo close = typeof(LobbyMainPanelKeyboardInputController).GetMethod(
                "TryCloseEscapePriorityPanel",
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(close, Is.Not.Null);
            Assert.That(close.Invoke(keyboard, null), Is.EqualTo(true));
            Assert.That(panel.activeSelf, Is.False);
        }
        finally
        {
            Object.DestroyImmediate(panel);
            Object.DestroyImmediate(keyboardObject);
        }
    }

    private static void SetField(object target, string name, object value)
    {
        FieldInfo field = target.GetType().GetField(
            name,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null);
        field.SetValue(target, value);
    }
}
