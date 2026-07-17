using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

public class LobbyPositionToggleButtonTests
{
    [Test]
    public void TogglePositionMode_HidesLobbyThenRestoresCapturedStates()
    {
        GameObject root = new("Root");
        GameObject buttonObject = new("TestPosition", typeof(RectTransform), typeof(Button));
        buttonObject.transform.SetParent(root.transform);

        GameObject backMain = CreateChild(root, "Back_Main", true);
        GameObject effectLobby = CreateChild(root, "Effect_Lobby", true);
        GameObject effectCharacter = CreateChild(root, "Effect_Char", false);
        GameObject lobbyMainPanel = CreateChild(root, "LobbyMainPanel", true);
        GameObject characterSettingPanel = CreateChild(root, "CharacterSettingPanel", false);
        GameObject position = CreateChild(root, "Position", false);
        GameObject positionPanel = CreateChild(root, "PositionPanel", false);
        LobbyPositionToggleButton toggle = buttonObject.AddComponent<LobbyPositionToggleButton>();

        try
        {
            toggle.TogglePositionMode();

            Assert.That(backMain.activeSelf, Is.False);
            Assert.That(effectLobby.activeSelf, Is.False);
            Assert.That(effectCharacter.activeSelf, Is.False);
            Assert.That(lobbyMainPanel.activeSelf, Is.False);
            Assert.That(characterSettingPanel.activeSelf, Is.False);
            Assert.That(position.activeSelf, Is.True);
            Assert.That(positionPanel.activeSelf, Is.True);

            toggle.TogglePositionMode();

            Assert.That(backMain.activeSelf, Is.True);
            Assert.That(effectLobby.activeSelf, Is.True);
            Assert.That(effectCharacter.activeSelf, Is.False);
            Assert.That(lobbyMainPanel.activeSelf, Is.True);
            Assert.That(characterSettingPanel.activeSelf, Is.False);
            Assert.That(position.activeSelf, Is.False);
            Assert.That(positionPanel.activeSelf, Is.False);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    private static GameObject CreateChild(GameObject parent, string name, bool active)
    {
        GameObject child = new(name);
        child.transform.SetParent(parent.transform);
        child.SetActive(active);
        return child;
    }
}
