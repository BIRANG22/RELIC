using NUnit.Framework;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public class LobbyViewStateControllerTests
{
    [Test]
    public void ShowMethods_ApplyExpectedObjectsForEveryState()
    {
        using TestContext context = new();

        context.Controller.ShowLobby();
        context.AssertState(LobbyViewState.Lobby, true, true, false, true, false, false, false);

        context.Controller.ShowCharacterSelection();
        context.AssertState(LobbyViewState.CharacterSelection, true, false, true, false, true, true, false);

        context.Controller.ShowPosition();
        context.AssertState(LobbyViewState.Position, false, false, false, false, false, false, true);
    }

    [Test]
    public void TogglePosition_TransitionsLobbyToPositionAndBackToLobby()
    {
        using TestContext context = new();

        context.Controller.ShowLobby();
        context.Controller.TogglePosition();
        Assert.That(context.Controller.CurrentState, Is.EqualTo(LobbyViewState.Position));

        context.Controller.TogglePosition();
        Assert.That(context.Controller.CurrentState, Is.EqualTo(LobbyViewState.Lobby));
    }

    [Test]
    public void DisableCameraDrag_ClearsDraggingAndSnapVelocity()
    {
        GameObject cameraObject = new("Camera", typeof(Camera), typeof(HorizontalHubCameraDrag));
        HorizontalHubCameraDrag cameraDrag = cameraObject.GetComponent<HorizontalHubCameraDrag>();

        try
        {
            SetPrivateField(cameraDrag, "isDragging", true);
            SetPrivateField(cameraDrag, "snapVelocity", 3f);

            cameraDrag.enabled = false;

            Assert.That(GetPrivateField<bool>(cameraDrag, "isDragging"), Is.False);
            Assert.That(GetPrivateField<float>(cameraDrag, "snapVelocity"), Is.Zero);
        }
        finally
        {
            Object.DestroyImmediate(cameraObject);
        }
    }

    [TestCase(0, LobbyViewState.CharacterSelection)]
    [TestCase(1, LobbyViewState.Lobby)]
    public void PanelTransitionButton_UpdatesLobbyViewState(int transitionMode, LobbyViewState expectedState)
    {
        using TestContext context = new();
        GameObject buttonObject = new("TransitionButton");
        LobbyPanelTransitionButton transitionButton = buttonObject.AddComponent<LobbyPanelTransitionButton>();
        SerializedObject serializedButton = new(transitionButton);
        serializedButton.FindProperty("transitionMode").enumValueIndex = transitionMode;
        serializedButton.ApplyModifiedPropertiesWithoutUndo();

        try
        {
            transitionButton.Execute();
            Assert.That(context.Controller.CurrentState, Is.EqualTo(expectedState));
        }
        finally
        {
            Object.DestroyImmediate(buttonObject);
        }
    }

    private static void SetPrivateField<T>(object target, string fieldName, T value)
    {
        target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(target, value);
    }

    private static T GetPrivateField<T>(object target, string fieldName)
    {
        return (T)target.GetType()
            .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(target);
    }

    private sealed class TestContext : System.IDisposable
    {
        private readonly GameObject root = new("Root");
        private readonly GameObject backMain;
        private readonly GameObject effectLobby;
        private readonly GameObject effectCharacter;
        private readonly GameObject lobbyMainPanel;
        private readonly GameObject characterSettingPanel;
        private readonly GameObject characterPreviewSpawnRoot;
        private readonly GameObject position;
        private readonly GameObject positionPanel;
        private readonly GameObject lobbyDirectionalLight;
        private readonly GameObject positionDirectionalLight;
        private readonly HorizontalHubCameraDrag cameraDrag;

        public LobbyViewStateController Controller { get; }

        public TestContext()
        {
            backMain = CreateChild("Back_Main");
            effectLobby = CreateChild("Effect_Lobby");
            effectCharacter = CreateChild("Effect_Char");
            lobbyMainPanel = CreateChild("LobbyMainPanel");
            characterSettingPanel = CreateChild("CharacterSettingPanel");
            characterPreviewSpawnRoot = CreateChild("CharacterPreviewSpawnRoot");
            position = CreateChild("Position");
            positionPanel = CreateChild("PositionPanel");
            lobbyDirectionalLight = CreateChild("Directional Light");
            positionDirectionalLight = CreateChild("Position Directional Light");

            GameObject cameraObject = CreateChild("Main Camera");
            cameraObject.AddComponent<Camera>();
            cameraDrag = cameraObject.AddComponent<HorizontalHubCameraDrag>();

            Controller = root.AddComponent<LobbyViewStateController>();
            SerializedObject serializedController = new(Controller);
            SetReference(serializedController, "backMain", backMain);
            SetReference(serializedController, "effectLobby", effectLobby);
            SetReference(serializedController, "effectCharacter", effectCharacter);
            SetReference(serializedController, "lobbyMainPanel", lobbyMainPanel);
            SetReference(serializedController, "characterSettingPanel", characterSettingPanel);
            SetReference(serializedController, "characterPreviewSpawnRoot", characterPreviewSpawnRoot);
            SetReference(serializedController, "position", position);
            SetReference(serializedController, "positionPanel", positionPanel);
            SetReference(serializedController, "lobbyDirectionalLight", lobbyDirectionalLight);
            SetReference(serializedController, "positionDirectionalLight", positionDirectionalLight);
            SetReference(serializedController, "hubCameraDrag", cameraDrag);
            serializedController.ApplyModifiedPropertiesWithoutUndo();
        }

        public void AssertState(
            LobbyViewState expectedState,
            bool expectedBackMain,
            bool expectedEffectLobby,
            bool expectedEffectCharacter,
            bool expectedLobbyPanel,
            bool expectedCharacterPanel,
            bool expectedCharacterPreview,
            bool expectedPosition)
        {
            Assert.Multiple(() =>
            {
                Assert.That(Controller.CurrentState, Is.EqualTo(expectedState));
                Assert.That(backMain.activeSelf, Is.EqualTo(expectedBackMain));
                Assert.That(effectLobby.activeSelf, Is.EqualTo(expectedEffectLobby));
                Assert.That(effectCharacter.activeSelf, Is.EqualTo(expectedEffectCharacter));
                Assert.That(lobbyMainPanel.activeSelf, Is.EqualTo(expectedLobbyPanel));
                Assert.That(characterSettingPanel.activeSelf, Is.EqualTo(expectedCharacterPanel));
                Assert.That(characterPreviewSpawnRoot.activeSelf, Is.EqualTo(expectedCharacterPreview));
                Assert.That(position.activeSelf, Is.EqualTo(expectedPosition));
                Assert.That(positionPanel.activeSelf, Is.EqualTo(expectedPosition));
                Assert.That(lobbyDirectionalLight.activeSelf, Is.EqualTo(!expectedPosition));
                Assert.That(positionDirectionalLight.activeSelf, Is.EqualTo(expectedPosition));
                Assert.That(cameraDrag.enabled, Is.EqualTo(expectedPosition));
            });
        }

        public void Dispose()
        {
            Object.DestroyImmediate(root);
        }

        private GameObject CreateChild(string name)
        {
            GameObject child = new(name);
            child.transform.SetParent(root.transform);
            return child;
        }

        private static void SetReference(SerializedObject target, string propertyName, Object value)
        {
            target.FindProperty(propertyName).objectReferenceValue = value;
        }
    }
}
