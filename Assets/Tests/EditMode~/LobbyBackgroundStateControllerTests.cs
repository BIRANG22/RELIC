using System.Reflection;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class LobbyBackgroundStateControllerTests
{
    private GameObject root;

    [TearDown]
    public void TearDown()
    {
        if (root != null)
            Object.DestroyImmediate(root);
    }

    [Test]
    public void ShowBackground_TogglesOnlySelectedBackgroundAndPositionCharacterSetting()
    {
        LobbyBackgroundStateController controller = CreateController(
            out GameObject position,
            out GameObject characterSetting,
            out GameObject erosionSelect,
            out GameObject relicShop,
            out GameObject cultureTank,
            out GameObject positionCharacterSetting);

        controller.ShowBackground(LobbyBackgroundState.Position);

        AssertState(
            controller,
            LobbyBackgroundState.Position,
            position,
            characterSetting,
            erosionSelect,
            relicShop,
            cultureTank,
            positionCharacterSetting);

        controller.ShowBackground(LobbyBackgroundState.CharacterSetting);

        AssertState(
            controller,
            LobbyBackgroundState.CharacterSetting,
            position,
            characterSetting,
            erosionSelect,
            relicShop,
            cultureTank,
            positionCharacterSetting);

        controller.ShowBackground(LobbyBackgroundState.RelicShop);

        AssertState(
            controller,
            LobbyBackgroundState.RelicShop,
            position,
            characterSetting,
            erosionSelect,
            relicShop,
            cultureTank,
            positionCharacterSetting);
    }

    [Test]
    public void TransitionButton_AppliesTargetPanelBackgroundThroughStateController()
    {
        LobbyBackgroundStateController controller = CreateController(
            out GameObject position,
            out GameObject characterSetting,
            out GameObject erosionSelect,
            out GameObject relicShop,
            out GameObject cultureTank,
            out GameObject positionCharacterSetting);

        GameObject relicShopPanel = CreateChild("RelicShopPanel");
        GameObject buttonObject = CreateChild("RelicShopButton");
        LobbyPanelTransitionButton button = buttonObject.AddComponent<LobbyPanelTransitionButton>();

        SetPrivateField(button, "panelToOpen", relicShopPanel);
        SetPrivateField(
            button,
            "transitionMode",
            LobbyPanelTransitionButton.PanelTransitionMode.Custom);
        SetPrivateField(button, "changeLobbyBackground", true);

        InvokeNonPublic(button, "InvokeBeforePanelChange");

        AssertState(
            controller,
            LobbyBackgroundState.RelicShop,
            position,
            characterSetting,
            erosionSelect,
            relicShop,
            cultureTank,
            positionCharacterSetting);
    }

    [Test]
    public void TransitionButton_ReturnToCharacterLobbyRestoresPositionBackground()
    {
        LobbyBackgroundStateController controller = CreateController(
            out GameObject position,
            out GameObject characterSetting,
            out GameObject erosionSelect,
            out GameObject relicShop,
            out GameObject cultureTank,
            out GameObject positionCharacterSetting);

        controller.ShowBackground(LobbyBackgroundState.CharacterSetting);

        GameObject buttonObject = CreateChild("BackButton");
        LobbyPanelTransitionButton button = buttonObject.AddComponent<LobbyPanelTransitionButton>();

        SetPrivateField(
            button,
            "transitionMode",
            LobbyPanelTransitionButton.PanelTransitionMode.CharacterToLobby);
        SetPrivateField(button, "changeLobbyBackground", true);

        InvokeNonPublic(button, "InvokeBeforePanelChange");

        AssertState(
            controller,
            LobbyBackgroundState.Position,
            position,
            characterSetting,
            erosionSelect,
            relicShop,
            cultureTank,
            positionCharacterSetting);
    }

    [Test]
    public void TransitionButton_NullTargetPanelRestoresPositionBackground()
    {
        LobbyBackgroundStateController controller = CreateController(
            out GameObject position,
            out GameObject characterSetting,
            out GameObject erosionSelect,
            out GameObject relicShop,
            out GameObject cultureTank,
            out GameObject positionCharacterSetting);

        controller.ShowBackground(LobbyBackgroundState.ErosionSelect);

        GameObject buttonObject = CreateChild("CloseButton");
        LobbyPanelTransitionButton button = buttonObject.AddComponent<LobbyPanelTransitionButton>();

        SetPrivateField(
            button,
            "transitionMode",
            LobbyPanelTransitionButton.PanelTransitionMode.Custom);
        SetPrivateField(button, "changeLobbyBackground", true);

        InvokeNonPublic(button, "InvokeBeforePanelChange");

        AssertState(
            controller,
            LobbyBackgroundState.Position,
            position,
            characterSetting,
            erosionSelect,
            relicShop,
            cultureTank,
            positionCharacterSetting);
    }

    [Test]
    public void LobbyScene_BackgroundStateControllerOwnsBackgroundActivation()
    {
        string scene = File.ReadAllText("Assets/Project/Scenes/YDM/Lobby.unity");

        Assert.Multiple(() =>
        {
            Assert.That(scene, Does.Contain("--- !u!114 &2200000503"));
            Assert.That(scene, Does.Contain("guid: 62c5c9ef42c24402a295983732f370c4"));
            Assert.That(scene, Does.Contain("positionBackground: {fileID: 771082557}"));
            Assert.That(scene, Does.Contain("characterSettingBackground: {fileID: 78107223}"));
            Assert.That(scene, Does.Contain("erosionSelectBackground: {fileID: 1221678008}"));
            Assert.That(scene, Does.Contain("relicShopBackground: {fileID: 1044419931}"));
            Assert.That(scene, Does.Contain("cultureTankBackground: {fileID: 63353276}"));
            Assert.That(scene, Does.Contain("positionCharacterSetting: {fileID: 1827125532}"));
        });

        Assert.That(FindBackgroundWorldObjectReferences(scene), Is.Empty);
    }

    private LobbyBackgroundStateController CreateController(
        out GameObject position,
        out GameObject characterSetting,
        out GameObject erosionSelect,
        out GameObject relicShop,
        out GameObject cultureTank,
        out GameObject positionCharacterSetting)
    {
        root = new GameObject("Root");
        position = CreateChild("Position_Back");
        characterSetting = CreateChild("CharacterSetting_Back");
        erosionSelect = CreateChild("ErosionSelect_Back");
        relicShop = CreateChild("RelicShop_Back");
        cultureTank = CreateChild("CultureTank_Back");
        positionCharacterSetting = CreateChild("CharacterSetting");

        LobbyBackgroundStateController controller =
            root.AddComponent<LobbyBackgroundStateController>();
        SerializedObject serializedController = new(controller);
        SetReference(serializedController, "positionBackground", position);
        SetReference(serializedController, "characterSettingBackground", characterSetting);
        SetReference(serializedController, "erosionSelectBackground", erosionSelect);
        SetReference(serializedController, "relicShopBackground", relicShop);
        SetReference(serializedController, "cultureTankBackground", cultureTank);
        SetReference(serializedController, "positionCharacterSetting", positionCharacterSetting);
        serializedController.ApplyModifiedPropertiesWithoutUndo();

        return controller;
    }

    private GameObject CreateChild(string name)
    {
        GameObject child = new(name);
        child.transform.SetParent(root != null ? root.transform : null);
        return child;
    }

    private static void AssertState(
        LobbyBackgroundStateController controller,
        LobbyBackgroundState expectedState,
        GameObject position,
        GameObject characterSetting,
        GameObject erosionSelect,
        GameObject relicShop,
        GameObject cultureTank,
        GameObject positionCharacterSetting)
    {
        Assert.Multiple(() =>
        {
            Assert.That(controller.CurrentState, Is.EqualTo(expectedState));
            Assert.That(position.activeSelf, Is.EqualTo(expectedState == LobbyBackgroundState.Position));
            Assert.That(
                characterSetting.activeSelf,
                Is.EqualTo(expectedState == LobbyBackgroundState.CharacterSetting));
            Assert.That(
                erosionSelect.activeSelf,
                Is.EqualTo(expectedState == LobbyBackgroundState.ErosionSelect));
            Assert.That(relicShop.activeSelf, Is.EqualTo(expectedState == LobbyBackgroundState.RelicShop));
            Assert.That(cultureTank.activeSelf, Is.EqualTo(expectedState == LobbyBackgroundState.CultureTank));
            Assert.That(
                positionCharacterSetting.activeSelf,
                Is.EqualTo(expectedState == LobbyBackgroundState.Position));
        });
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(field, Is.Not.Null, $"Missing field: {fieldName}");
        field.SetValue(target, value);
    }

    private static void SetReference(SerializedObject target, string propertyName, Object value)
    {
        target.FindProperty(propertyName).objectReferenceValue = value;
    }

    private static void InvokeNonPublic(object target, string methodName)
    {
        MethodInfo method = target.GetType().GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(method, Is.Not.Null, $"Missing method: {methodName}");
        method.Invoke(target, null);
    }

    private static string[] FindBackgroundWorldObjectReferences(string scene)
    {
        string[] backgroundIds =
        {
            "771082557",
            "78107223",
            "1221678008",
            "1044419931",
            "63353276"
        };
        string[] lines = scene.Split('\n');
        System.Collections.Generic.List<string> references = new();

        for (int i = 0; i < lines.Length; i++)
        {
            if (!lines[i].Contains("worldObjectsToClose:") &&
                !lines[i].Contains("worldObjectsToOpen:"))
            {
                continue;
            }

            for (int j = i + 1; j < lines.Length && lines[j].StartsWith("  - "); j++)
            {
                for (int idIndex = 0; idIndex < backgroundIds.Length; idIndex++)
                {
                    if (lines[j].Contains($"fileID: {backgroundIds[idIndex]}"))
                        references.Add($"{i + 1}: {lines[j].Trim()}");
                }
            }
        }

        return references.ToArray();
    }
}
