using System.IO;
using NUnit.Framework;

public class LobbyBagScenePlacementTests
{
    private const string LobbyScenePath = "Assets/Project/Scenes/YDM/Lobby.unity";
    private const string LobbyKeyboardControllerPath = "Assets/Project/Scripts/LobbyMainPanelKeyboardInputController.cs";
    private const string BattleBagPanelUiGuid = "61fd852e679e8704eb1fad3a9f57c0a8";
    private const string InventoryRuntimeContextProviderGuid = "ee0929861ec98b34998f1bb1ec0f539e";

    [Test]
    public void LobbyScene_ContainsScenePlacedBagButtonAndPanel()
    {
        string sceneYaml = File.ReadAllText(LobbyScenePath);

        Assert.That(sceneYaml, Does.Contain("m_Name: BagButton"));
        Assert.That(sceneYaml, Does.Contain("m_Name: BagPanel"));
        Assert.That(sceneYaml, Does.Contain("m_Name: SlotRoot"));
        Assert.That(sceneYaml, Does.Contain($"guid: {BattleBagPanelUiGuid}"));
        Assert.That(sceneYaml, Does.Contain($"guid: {InventoryRuntimeContextProviderGuid}"));
        Assert.That(sceneYaml, Does.Contain("m_MethodName: Refresh"));
    }

    [Test]
    public void LobbyKeyboardController_DoesNotCreateBagUiAtRuntime()
    {
        string source = File.ReadAllText(LobbyKeyboardControllerPath);

        Assert.That(source, Does.Not.Contain("CreateLobbyBagPanel"));
        Assert.That(source, Does.Not.Contain("new GameObject(lobbyBagButtonObjectName"));
        Assert.That(source, Does.Not.Contain("createLobbyBagButton"));
    }
}
