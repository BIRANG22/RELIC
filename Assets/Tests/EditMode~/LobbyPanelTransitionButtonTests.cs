using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class LobbyPanelTransitionButtonTests
{
    private GameObject controllerObject;
    private GameObject buttonObject;

    [TearDown]
    public void TearDown()
    {
        if (buttonObject != null)
            Object.DestroyImmediate(buttonObject);

        if (controllerObject != null)
            Object.DestroyImmediate(controllerObject);
    }

    [Test]
    public void CharacterReturn_OpensPositionState()
    {
        controllerObject = new GameObject("LobbyViewStateController");
        LobbyViewStateController viewStateController =
            controllerObject.AddComponent<LobbyViewStateController>();
        viewStateController.ShowCharacterSelection();

        buttonObject = new GameObject("CharacterReturnButton");
        LobbyPanelTransitionButton button =
            buttonObject.AddComponent<LobbyPanelTransitionButton>();

        SetPrivateField(
            button,
            "transitionMode",
            LobbyPanelTransitionButton.PanelTransitionMode.CharacterToLobby);
        InvokeNonPublic(button, "InvokeAfterPanelChange");

        Assert.That(
            viewStateController.CurrentState,
            Is.EqualTo(LobbyViewState.Position));
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
