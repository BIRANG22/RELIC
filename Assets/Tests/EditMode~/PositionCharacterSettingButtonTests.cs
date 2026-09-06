using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class PositionCharacterSettingButtonTests
{
    private GameObject controllerObject;
    private GameObject buttonObject;
    private GameObject selectionStateObject;

    [TearDown]
    public void TearDown()
    {
        if (buttonObject != null)
            Object.DestroyImmediate(buttonObject);

        if (controllerObject != null)
            Object.DestroyImmediate(controllerObject);

        if (selectionStateObject != null)
            Object.DestroyImmediate(selectionStateObject);
    }

    [Test]
    public void Execute_OpensCharacterSelection_WithoutChangingPartySlotIndex()
    {
        selectionStateObject = new GameObject("CharacterSelectionState");
        CharacterSelectionState selectionState =
            selectionStateObject.AddComponent<CharacterSelectionState>();
        InvokeNonPublic(selectionState, "Awake");
        selectionState.SelectPartySlot(2);

        controllerObject = new GameObject("LobbyViewStateController");
        LobbyViewStateController viewStateController =
            controllerObject.AddComponent<LobbyViewStateController>();
        viewStateController.ShowPosition();

        buttonObject = new GameObject("Cha1");
        PositionCharacterSettingButton button =
            buttonObject.AddComponent<PositionCharacterSettingButton>();

        button.Execute();

        Assert.That(
            viewStateController.CurrentState,
            Is.EqualTo(LobbyViewState.CharacterSelection));
        Assert.That(selectionState.CurrentPartySlotIndex, Is.EqualTo(2));
    }

    [Test]
    public void WorldSpriteClick_AddsColliderAndOpensCharacterSelection()
    {
        controllerObject = new GameObject("LobbyViewStateController");
        LobbyViewStateController viewStateController =
            controllerObject.AddComponent<LobbyViewStateController>();
        viewStateController.ShowPosition();

        buttonObject = new GameObject("Cha1");
        buttonObject.AddComponent<SpriteRenderer>();
        PositionCharacterSettingButton button =
            buttonObject.AddComponent<PositionCharacterSettingButton>();

        InvokeNonPublic(button, "Awake");
        InvokeNonPublic(button, "OnMouseUpAsButton");

        Assert.That(buttonObject.GetComponent<Collider2D>(), Is.Not.Null);
        Assert.That(
            viewStateController.CurrentState,
            Is.EqualTo(LobbyViewState.CharacterSelection));
    }

    [Test]
    public void WorldSpriteClick_DoesNotOpenCharacterSelection_WhenPositionModalBlocksInput()
    {
        controllerObject = new GameObject("LobbyViewStateController");
        LobbyViewStateController viewStateController =
            controllerObject.AddComponent<LobbyViewStateController>();
        viewStateController.ShowPosition();

        buttonObject = new GameObject("Cha1");
        buttonObject.AddComponent<SpriteRenderer>();
        PositionCharacterSettingButton button =
            buttonObject.AddComponent<PositionCharacterSettingButton>();

        try
        {
            LobbyPositionModalInputBlocker.Block(button);

            InvokeNonPublic(button, "Awake");
            InvokeNonPublic(button, "OnMouseUpAsButton");

            Assert.That(
                viewStateController.CurrentState,
                Is.EqualTo(LobbyViewState.Position));
        }
        finally
        {
            LobbyPositionModalInputBlocker.Unblock(button);
        }
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
