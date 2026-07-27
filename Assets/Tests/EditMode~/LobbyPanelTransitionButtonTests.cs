using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class LobbyPanelTransitionButtonTests
{
    private const string TransitionButtonPath = "Assets/Project/Scripts/UI/LobbyPanelTransitionButton.cs";
    private const string LobbyScenePath = "Assets/Project/Scenes/YDM/Lobby.unity";

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

    [Test]
    public void WorldObjectClick_ExecutesOnMouseReleaseInsteadOfMousePress()
    {
        string source = File.ReadAllText(TransitionButtonPath);

        Assert.That(source, Does.Contain("private void OnMouseUpAsButton()"));
        Assert.That(source, Does.Not.Contain("private void OnMouseDown()"));
    }

    [Test]
    public void LobbyScene_StatueClickColliderCoversHoverRange()
    {
        string scene = File.ReadAllText(LobbyScenePath);
        string statueCollider = GetYamlBlock(scene, "--- !u!61 &82567349");

        Assert.That(statueCollider, Does.Contain("m_Offset: {x: 0, y: 2.6}"));
        Assert.That(statueCollider, Does.Contain("m_Size: {x: 2.4, y: 5.2}"));
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

    private static string GetYamlBlock(string yaml, string marker)
    {
        int start = yaml.IndexOf(marker, System.StringComparison.Ordinal);
        Assert.That(start, Is.GreaterThanOrEqualTo(0), $"Missing marker: {marker}");

        int next = yaml.IndexOf("\n--- ", start + marker.Length, System.StringComparison.Ordinal);
        return next >= 0 ? yaml.Substring(start, next - start) : yaml.Substring(start);
    }
}
